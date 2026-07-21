using System.Net;

class Config
{
    // ─── 포트 상수 ────────────────────────────────────────
    // 포트 번호를 한 곳에서 관리하면 나중에 변경이 쉬워요
    public const int DataCollectServerPort = 1111;
    public const int MainServerPort        = 2222;
    public const int QueueServerPort       = 3333;
    public const int SensorServerPort      = 4444;
    public const int CameraServerPort      = 5555;

    // ─── EndPoint ─────────────────────────────────────────
    public IPEndPoint DataCollectServerEndPoint { get; }
    public IPEndPoint MainServerEndPoint        { get; }
    public IPEndPoint QueueServerEndPoint       { get; }
    public IPEndPoint SensorServerEndPoint      { get; }
    public IPEndPoint CameraServerEndPoint      { get; }

    public Config()
    {
        // 현재는 전부 localhost, 나중에 실제 IP로 교체만 하면 됨
        var dataCollectServerIP = IPAddress.Loopback;
        var mainServerIP        = IPAddress.Loopback;
        var queueServerIP       = IPAddress.Loopback;
        var sensorServerIP      = IPAddress.Loopback;  // 라즈베리 파이 IP로 교체 예정
        var cameraServerIP      = IPAddress.Loopback;

        DataCollectServerEndPoint = new IPEndPoint(dataCollectServerIP, DataCollectServerPort);
        MainServerEndPoint        = new IPEndPoint(mainServerIP,        MainServerPort);
        QueueServerEndPoint       = new IPEndPoint(queueServerIP,       QueueServerPort);
        SensorServerEndPoint      = new IPEndPoint(sensorServerIP,      SensorServerPort);
        CameraServerEndPoint      = new IPEndPoint(cameraServerIP,      CameraServerPort);
    }
}