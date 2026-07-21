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

class QueueServer
{
    private Config config;
    private Socket socket;
    Channel<Protocols.JsonFile> JsonFileChannel;
    public QueueServer()
    {
        socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        config = new Config();
        JsonFileChannel = Channel.CreateBounded<Protocols.JsonFile>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            }
        );
    }
    public async Task Start(CancellationToken ct = default)
    {
        try
        {
            socket.Bind(config.QueueServerEndPoint);
            socket.Listen(10);

            while (!ct.IsCancellationRequested)
            {
            Socket client = await socket.AcceptAsync(ct);
            _ =  handle_queue(client,ct);
            }
        }
        catch
        {
            throw;
        }
        finally
        {
            socket.Close();
        }
    }
    public async Task handle_queue(Socket client,CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                (Protocols.FileType type,Protocols.JsonFile json_file) = await Protocols.RecvJson(client);
                switch (type)
                {
                    case Protocols.FileType.DataSend:
                        await JsonFileChannel.Writer.WriteAsync(json_file);
                    break;

                    case Protocols.FileType.RequestJson:
                        if (JsonFileChannel.Reader.TryRead(out Protocols.JsonFile? item))
                        {
                            await Protocols.SendJsonFile(client, item, Protocols.FileType.ResponseJson);
                        }
                        else
                        {
                            // 큐가 비어있으면 빈 응답 전송 → Worker 데드락 방지
                            var empty = new Protocols.JsonFile
                            {
                                FileName = "",
                                Data = Array.Empty<byte>()
                            };
                            await Protocols.SendJsonFile(client, empty, Protocols.FileType.ResponseJson);
                        }
                        break;
                    

                    default:
                        Console.WriteLine($"[QueueServer] 알 수 없는 타입 — {type}");
                    break;
                }
            }
        }
        catch
        {
            throw;
        }
        finally
        {
        client.Close();
        }
    }
}