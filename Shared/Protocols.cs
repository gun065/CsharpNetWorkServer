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

class Protocols
{
    public enum FileParseProcess
    {
        ReadBasicJsonLength,
        ReadJsonFile,
        ReadFile
    }
    public enum FileType
    {
        SomeFile,
        RequestJson,
        ResponseJson,
        DataSend
    }
    public class HeaderPacket
    {
        public string FileName {get; set;} = string.Empty;
        public long TotalSize { get; set; }
        public FileType Type {get; set;}
    }
    public class JsonFile
    {
        public string FileName {get; set;} = string.Empty;
        public byte[] Data {get; set;} = Array.Empty<byte>();
    }
    async static public Task SendAll (Socket connect, Memory<byte> data)
    {
        while ( !data.IsEmpty)
        {
            int send_data = await connect.SendAsync(data);
            data = data[send_data..];
        }
    }
        public static async Task SendJsonFile(Socket connect, JsonFile jsonFile,FileType type)
    {
        int    maxSize  = 256 + jsonFile.Data.Length + 512;
        byte[] rentBuf  = ArrayPool<byte>.Shared.Rent(maxSize);
        Memory<byte> mem = rentBuf;
        try
        {
            var header = new HeaderPacket
            {
                FileName = jsonFile.FileName,
                TotalSize = jsonFile.Data.Length,
                Type = type
            };

            byte[] headerBytes = JsonSerializer.SerializeToUtf8Bytes(header);
            byte[] bodyBytes    = JsonSerializer.SerializeToUtf8Bytes(jsonFile);

            BitConverter.GetBytes(headerBytes.Length).CopyTo(mem.Span);
            headerBytes.CopyTo(mem[4..]);
            bodyBytes.CopyTo(mem[(4+headerBytes.Length)..]);
            await SendAll(connect,mem[..4]);
            await SendAll(connect,mem[4..(4+headerBytes.Length)]);
            await SendAll(connect,mem[(4+headerBytes.Length)..(4+headerBytes.Length+bodyBytes.Length)]);
        }
        catch
        {
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentBuf);
        }
    }
    async public static Task<int> Recv (Socket connect, int size)
    {
        byte[] buffers = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            Memory<byte> buffer = buffers;
            int read_size = 0;
            while (size > read_size)
            {
                int read = await connect.ReceiveAsync(buffer.Slice(read_size,size - read_size));
                if (read == 0) break;
                read_size += read;
            }
            
            return BitConverter.ToInt32(buffer.Span);
        }
        catch
        {
            Console.WriteLine("errer");
            throw; 
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffers);
        }
    }

    async public static Task<(byte[] RentedArray, Memory<byte> Data)> RecvExact(Socket connect, int size)
    {
        byte[]rentBuf = ArrayPool<byte>.Shared.Rent(size);
        Memory<byte> mem = rentBuf.AsMemory(0, size);
        int received = 0;

        while (received < size)
        {
            int read = await connect.ReceiveAsync(mem[received..]);
            if (read == 0)
                throw new IOException("연결이 끊겼습니다.");
            received += read;
        }
        return (rentBuf, mem);
    }

    public async static Task<(FileType Type,JsonFile File)> RecvJson (Socket connect)
    {
        int headerLength = 0;
        byte[]? lengthRented = null;

        try
        {
            var (rented, mem) = await RecvExact(connect, 4);
            lengthRented = rented;
            headerLength = BitConverter.ToInt32(mem.Span);   
        }
        catch
        {
            Console.WriteLine("errer");
            throw;
        }
        finally
        {   if (lengthRented is not null)
                ArrayPool<byte>.Shared.Return(lengthRented);
        }
        HeaderPacket header;
        byte[]? headerRented = null;

        try
        {
            var (rented, mem) = await RecvExact(connect, headerLength);
            headerRented = rented;
            header = JsonSerializer.Deserialize<HeaderPacket>(mem.Span)
                     ?? throw new InvalidDataException("헤더 역직렬화 실패");
        }
        catch
        {
            throw;
        }
        finally
        {
            if (headerRented is not null)
                ArrayPool<byte>.Shared.Return(headerRented);
        }
        byte[]? bodyRented = null;

        try
        {
            var (rented, mem) = await RecvExact(connect, (int)header.TotalSize);
            bodyRented = rented;
            // Memory<byte>.Span으로 복사 없이 역직렬화
            JsonFile jsonFile = JsonSerializer.Deserialize<JsonFile>(mem.Span)
                ?? throw new InvalidDataException("본문 역직렬화 실패");
            return (header.Type, jsonFile);
        }
        catch
        {
            throw;
        }
        finally
        {
            if (bodyRented is not null)
                ArrayPool<byte>.Shared.Return(bodyRented);
        }
    }
    public async Task swtich_test()
    {

    }
}