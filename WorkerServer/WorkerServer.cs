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
using System.Runtime.CompilerServices;
using System.Linq.Expressions;

class WorkerServer
{
    private readonly Config _config;
    private Socket _queueSocket;
    private Socket _dataCollectSocket;  // MainServer가 아니라 DataCollectServer로 변경
    private readonly List<Protocols.JsonFile> _batch;

    public WorkerServer()
    {
        _config = new Config();
        _queueSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _dataCollectSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _batch = new List<Protocols.JsonFile>();
    }

    public async Task NetworkConnect()
    {
        await _queueSocket.ConnectAsync(_config.QueueServerEndPoint);
        Console.WriteLine("[WorkerServer] QueueServer 연결 성공");

        await _dataCollectSocket.ConnectAsync(_config.DataCollectServerEndPoint);
        Console.WriteLine("[WorkerServer] DataCollectServer 연결 성공");
    }

    public async Task Start(CancellationToken ct = default)
    {
        await NetworkConnect();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // 1. QueueServer에 데이터 요청
                var request = new Protocols.JsonFile
                {
                    FileName = "",
                    Data = Array.Empty<byte>()
                };
                await Protocols.SendJsonFile(_queueSocket, request, Protocols.FileType.RequestJson);

                // 2. 응답 수신
                var (type, jsonFile) = await Protocols.RecvJson(_queueSocket);

                if (type != Protocols.FileType.ResponseJson)
                    continue;

                // 3. 빈 응답 → 큐가 비어있음 → 100ms 대기 후 재요청
                if (jsonFile.Data.Length == 0)
                {
                    await Task.Delay(100, ct);
                    continue;
                }

                // 4. 데이터 있음 → 배치에 누적
                _batch.Add(jsonFile);

                // 5. 50건 모이면 DataCollectServer로 전송
                if (_batch.Count >= 50)
                {
                    await FlushToDataCollect();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[WorkerServer] 연결 끊김 — {ex.Message}");
                break;  // 추후 재연결 로직 추가 위치
            }
        }

        // 종료 전 남은 데이터 전송
        if (_batch.Count > 0)
            await FlushToDataCollect();
    }

    private async Task FlushToDataCollect()
    {
        var sendFile = new Protocols.JsonFile
        {
            FileName = $"worker_{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            Data = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(_batch)
        };

        await Protocols.SendJsonFile(_dataCollectSocket, sendFile, Protocols.FileType.DataSend);
        Console.WriteLine($"[WorkerServer] DataCollectServer 전달 — {_batch.Count}건");
        _batch.Clear();
    }
}