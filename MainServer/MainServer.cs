using System.Net.Sockets;
using System.Text.Json;

class MainServer
{
    private readonly Config _config;
    private readonly Socket _listenSocket;

    public MainServer()
    {
        _config       = new Config();
        _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    public async Task Start(CancellationToken ct = default)
    {
        try
        {
            _listenSocket.Bind(_config.MainServerEndPoint);
            _listenSocket.Listen(10);
            Console.WriteLine($"[MainServer] 시작 — {_config.MainServerEndPoint}");

            while (!ct.IsCancellationRequested)
            {
                Socket client = await _listenSocket.AcceptAsync(ct);
                Console.WriteLine($"[MainServer] 연결 — {client.RemoteEndPoint}");

                _ = HandleClient(client, ct);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[MainServer] 종료");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[MainServer] 소켓 오류 — {ex.SocketErrorCode}");
        }
        finally
        {
            _listenSocket.Close();
        }
    }

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
                    Console.WriteLine($"[MainServer] 예상치 못한 타입 — {type}");
                    continue;
                }

                // 묶음 역직렬화
                List<Protocols.JsonFile>? batch =
                    JsonSerializer.Deserialize<List<Protocols.JsonFile>>(jsonFile.Data);

                if (batch is null || batch.Count == 0) continue;

                // DB 저장 (가공 로직 확정 후 구현)
                await SaveToDb(batch);

                // 현재 상태 출력
                Print(batch);
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[MainServer] 연결 끊김 — {ex.Message}");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[MainServer] 소켓 오류 — {ex.SocketErrorCode}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[MainServer] 클라이언트 처리 종료");
        }
        finally
        {
            client.Close();
        }
    }

    // ─── DB 저장 (미완성 — 데이터 구조 확정 후 구현) ─────
    private Task SaveToDb(List<Protocols.JsonFile> batch)
    {
        // 추후 SQLite, Redis 등 선택 후 구현
        Console.WriteLine($"[MainServer] DB 저장 — {batch.Count}개");
        return Task.CompletedTask;
    }

    // ─── 현재 상태 출력 ──────────────────────────────────
    private void Print(List<Protocols.JsonFile> batch)
    {
        Console.WriteLine($"[MainServer] 수신 {batch.Count}개 — {DateTime.UtcNow:HH:mm:ss}");
        foreach (var file in batch)
        {
            Console.WriteLine($"  └─ {file.FileName}");
        }
    }
}