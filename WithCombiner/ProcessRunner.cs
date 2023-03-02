using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WithCombiner
{
    internal class ProcessRunner
    {
        private const string RTSP_ADDRESS  = "rtsp://admin:HelloWorld4@192.168.1.64:554/ISAPI/Streaming/Channels/101";
        private const int VideoPort = 8100; //8084 works
        private const string SdpFileName = "rtpsession.sdp";
        private FFmpegCommandBuilder _ffCombiner = new FFmpegCommandBuilder();
        private Process _ffmpegProcess;
        private FFMpegAppOptions _ffmpegOptions = new FFMpegAppOptions
        {
            BinaryFolder = "C:\\Program Files\\ffmpeg\\bin",
            TempFolder = "A:\\Work\\PIMU\\ffmpeg_temp",
            SdpFolder = "A:\\Work\\PIMU\\ffmpeg_sdp"
        };

        public string SdpFilePath 
        {
            get
            {
                return Path.Combine(_ffmpegOptions.SdpFolder, SdpFileName);
            }
        }

        public uint SSRC { get; private set; } = 38106908;
        public int RtpPort
        {
            get
            {
                return VideoPort;
            }
        }

        public void Run() 
        {
            ConfigureFfMpegCombiner();
            ConfigureAndRunProcess();
        } 

        private void ConfigureAndRunProcess()
        {
            _ffmpegProcess = new Process();
            _ffmpegProcess.StartInfo.FileName = _ffCombiner.EXE_PATH;
            _ffmpegProcess.StartInfo.CreateNoWindow = true;
            _ffmpegProcess.StartInfo.UseShellExecute = false;
            _ffmpegProcess.StartInfo.RedirectStandardError = true;
            _ffmpegProcess.StartInfo.RedirectStandardInput = true;
            _ffmpegProcess.StartInfo.RedirectStandardOutput = true;
            
            _ffmpegProcess.OutputDataReceived += FFMpegOutputLog;
            _ffmpegProcess.ErrorDataReceived += FFMpegOutputError;
            
            _ffmpegProcess.StartInfo.Arguments = _ffCombiner.GetCommandString(FFmpegCommandBuilder.FfmpegStreamTypeEnum.rtspTortp_codecWithSsrc);
            _ffmpegProcess.Start();

            _ffmpegProcess.BeginOutputReadLine();
            _ffmpegProcess.BeginErrorReadLine();
        }

       

        private void ConfigureFfMpegCombiner()
        {
            // TODO: FFmpegBuilderConfig. Class with configuration that puts inside the Combiner.
            _ffCombiner.SSRC = SSRC;
            _ffCombiner.EXE_PATH = Path.Combine(_ffmpegOptions.BinaryFolder, "ffmpeg");
            _ffCombiner.RTSP = RTSP_ADDRESS;
            _ffCombiner.VideoCodec = CodecEnum.h264.ToString("g");
            // server - default Ip.loopback
            // ssrc - default 
            _ffCombiner.FfmpegAppOptions = _ffmpegOptions;
            _ffCombiner.RTP_PORT = VideoPort;
            _ffCombiner.SdpFullPath = Path.Combine(_ffmpegOptions.SdpFolder,SdpFileName);
        }

        public string GetCmdCommand()
        {
            return $"ffmpeg {_ffCombiner.GetCommandString()}";
        }

        private void FFMpegOutputError(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private void FFMpegOutputLog(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }
    }
}
