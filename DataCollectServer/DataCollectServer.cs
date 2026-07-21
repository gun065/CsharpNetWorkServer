using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Buffers;

class DataCollectServer
{
    private readonly Config _config;

    // WorkerServer 수신용 (Listen)
    private readonly Socket _listenSocket;

    // MainServer 송신용 (Connect)
    private readonly Socket _mainSocket;

    // 1초 동안 누적할 버퍼 — Channel로 스레드 안전 보장
    private readonly Channel<Protocols.JsonFile> _buffer;

    public DataCollectServer()
    {
        _config      = new Config();
        _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _mainSocket   = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        _buffer = Channel.CreateBounded<Protocols.JsonFile>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
    }

    public async Task Start(CancellationToken ct = default)
    {
        try
        {
            // MainServer 연결
            await _mainSocket.ConnectAsync(_config.MainServerEndPoint, ct);
            Console.WriteLine("[DataCollectServer] MainServer 연결 성공");

            // WorkerServer 수신 준비
            _listenSocket.Bind(_config.DataCollectServerEndPoint);
            _listenSocket.Listen(10);
            Console.WriteLine($"[DataCollectServer] 시작 — {_config.DataCollectServerEndPoint}");

            // 수신 루프 + 1초 주기 송신 루프 동시 실행
            await Task.WhenAll(
                AcceptLoop(ct),   // WorkerServer 연결 수락
                SendLoop(ct)      // 1초마다 MainServer로 전달
            );
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[DataCollectServer] 종료");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[DataCollectServer] 소켓 오류 — {ex.SocketErrorCode}");
        }
        finally
        {
            _buffer.Writer.TryComplete();
            _listenSocket.Close();
            _mainSocket.Close();
        }
    }

    // ─── WorkerServer 연결 수락 루프 ─────────────────────
    private async Task AcceptLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                Socket client = await _listenSocket.AcceptAsync(ct);
                Console.WriteLine($"[DataCollectServer] WorkerServer 연결 — {client.RemoteEndPoint}");

                // HandleClient 안에서 예외 처리하므로 discard 안전
                _ = HandleClient(client, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[DataCollectServer] Accept 오류 — {ex.SocketErrorCode}");
            }
        }
    }

    // ─── WorkerServer로부터 데이터 수신 → 버퍼에 적재 ───
    private async Task HandleClient(Socket client, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                (Protocols.FileType type, Protocols.JsonFile jsonFile) =
                    await Protocols.RecvJson(client);

                if (type != Protocols.FileType.DataSend)
                {
                    Console.WriteLine($"[DataCollectServer] 예상치 못한 타입 — {type}");
                    continue;
                }

                // Channel에 적재 — 스레드 안전
                await _buffer.Writer.WriteAsync(jsonFile, ct);
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[DataCollectServer] WorkerServer 연결 끊김 — {ex.Message}");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[DataCollectServer] 소켓 오류 — {ex.SocketErrorCode}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[DataCollectServer] 클라이언트 처리 종료");
        }
        finally
        {
            client.Close();
        }
    }

    // ─── 1초마다 누적 데이터 → MainServer 전달 ───────────
    private async Task SendLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // 1초 대기
                await Task.Delay(1000, ct);

                // 버퍼에 쌓인 데이터 전부 꺼내기
                var batch = new List<Protocols.JsonFile>();
                while (_buffer.Reader.TryRead(out Protocols.JsonFile? item))
                {
                    batch.Add(item);
                }

                if (batch.Count == 0)
                {
                    Console.WriteLine("[DataCollectServer] 전송할 데이터 없음");
                    continue;
                }

                // 묶어서 MainServer로 전달
                var sendFile = new Protocols.JsonFile
                {
                    FileName = $"collect_{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    Data     = JsonSerializer.SerializeToUtf8Bytes(batch)
                };

                await Protocols.SendJsonFile(_mainSocket, sendFile, Protocols.FileType.DataSend);
                Console.WriteLine($"[DataCollectServer] MainServer 전달 — {batch.Count}개");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[DataCollectServer] MainServer 연결 끊김 — {ex.Message}");
                break;
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[DataCollectServer] 소켓 오류 — {ex.SocketErrorCode}");
                break;
            }
        }
    }
}