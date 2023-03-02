
using System.Net;
using WithCombiner;

int wsPort = 5330;
Task[] tasks= new Task[3];

var process = new ProcessRunner();
tasks[0] = new Task(() => process.Run());
tasks[0].Start();

var ffmpegListener = new FFmpegListener(process.SdpFilePath, process.RtpPort, process.SSRC);
tasks[1] = new Task(() => ffmpegListener.Start());
tasks[1].Start();

var wsserver = new WSSignalingServer(IPAddress.Loopback, wsPort, process.SdpFilePath, ffmpegListener);
wsserver.Run();
tasks[2] = new Task(() => wsserver.Run());
tasks[2].Start();
