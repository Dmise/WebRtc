
using System.Net;
using System.Runtime.ExceptionServices;
using WithCombiner;

int wsPort = 5330;
int videoPort = 8098;
string _rtspAddress = "rtsp://admin:HelloWorld4@192.168.1.64:554/ISAPI/Streaming/Channels/101";
FFMpegAppOptions _ffmpegOptions = new FFMpegAppOptions
{
    BinaryFolder = "C:\\Program Files\\ffmpeg\\bin",
    TempFolder = "A:\\Work\\PIMU\\ffmpeg_temp",
    SdpFolder = "A:\\Work\\PIMU\\ffmpeg_sdp"
};
Task[] tasks= new Task[3];


var process = new RTSPToSdpStreamer(_rtspAddress, _ffmpegOptions, videoPort);
tasks[0] = new Task(() => process.Run());
tasks[0].Start();

var ffmpegListener = new FFmpegListener(process.SdpFilePath, process.RtpPort, process.SSRC);
tasks[1] = new Task(() => ffmpegListener.Start());
tasks[1].Start();

var wsserver = new WSSignalingServer(IPAddress.Loopback, wsPort, process.SdpFilePath, ffmpegListener);
wsserver.Run();
tasks[2] = new Task(() => wsserver.Run());
tasks[2].Start();

while (true)
{
    var key = Console.ReadKey();
    if (key.KeyChar == 'q')
    {
        process.Dispose();
        System.Environment.Exit(0);
    }
}
