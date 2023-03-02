
using Org.BouncyCastle.Crypto.Tls;
using System.Net;
using System.Runtime.ExceptionServices;
using ToolzLib;
using WithCombiner;

int wsPort = 5330;
int videoPort = 8098;
string _rtspAddress = "rtsp://admin:HelloWorld4@192.168.1.64:554/ISAPI/Streaming/Channels/101";
Toolz _toolz = new Toolz();
FFmpegListener _ffmpegListener = new FFmpegListener();
FFMpegAppOptions _ffmpegOptions = new FFMpegAppOptions
{
    BinaryFolder = "C:\\Program Files\\ffmpeg\\bin",
    TempFolder = "A:\\Work\\PIMU\\ffmpeg_temp",
    SdpFolder = "A:\\Work\\PIMU\\ffmpeg_sdp"
};
Task[] tasks= new Task[3];

var _rtspStreamer = new RTSPToSdpStreamer(_rtspAddress, _ffmpegOptions, videoPort);
tasks[0] = new Task(() => _rtspStreamer.Run());
tasks[0].Start();


if (_toolz.Waiter(
    () => File.Exists(_rtspStreamer.SdpFilePath),
    TimeSpan.FromSeconds(6)
    ))
{
    _ffmpegListener = new FFmpegListener(_rtspStreamer.SdpFilePath, _rtspStreamer.RtpPort, _rtspStreamer.SSRC);
    tasks[1] = new Task(() => _ffmpegListener.Start());
    tasks[1].Start();
}
else
{
    System.Environment.Exit(0);
}


if (_toolz.Waiter(
    () => _ffmpegListener.Running,
    TimeSpan.FromSeconds(9)
    ))
{
    var wsserver = new WSSignalingServer(IPAddress.Loopback, wsPort, _rtspStreamer.SdpFilePath, _ffmpegListener);
    wsserver.Run();
    tasks[2] = new Task(() => wsserver.Run());
    tasks[2].Start();
}
else
{
    System.Environment.Exit(0);
}


while (true)
{
    var key = Console.ReadKey();
    if (key.KeyChar == 'q')
    {
        _rtspStreamer.Dispose();
        System.Environment.Exit(0);
    }
}
