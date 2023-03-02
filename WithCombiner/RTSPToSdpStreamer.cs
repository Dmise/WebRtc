using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WithCombiner.FFmpegCommandBuilder;

namespace WithCombiner
{
    internal class RTSPToSdpStreamer : IDisposable
    {
        // const
        private uint _ssrc = 38106908;

        //injected
        private ILogger<RTSPToSdpStreamer>? _logger;
        private string _rtspAddress;
        private FFMpegAppOptions _ffmpegOptions;
        private int _videoPort;

        //other private
        private FFmpegCommandBuilder _ffCombiner = new FFmpegCommandBuilder();
        private Process _ffmpegProcess;

        //public fields

        public string SdpFilePath
        {
            get
            {
                return Path.Combine(_ffmpegOptions.SdpFolder, $"{_videoPort}.sdp");
            }
        }

        public uint SSRC { get { return _ssrc; } }
        public int RtpPort
        {
            get
            {
                return _videoPort;
            }
        }

        public RTSPToSdpStreamer(string rtspaddress, FFMpegAppOptions ffmpegOptions, int videoPort, 
            ILogger<RTSPToSdpStreamer>? logger = null)  // _sp.GetService<ILogger<RTSPToSdpStreamer>>(), _rtspStreamAddress,  _ffmpegOptions
        {
            _rtspAddress = rtspaddress;
            _ffmpegOptions = ffmpegOptions;
            _videoPort = videoPort;
            _logger = logger;
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

            _ffmpegProcess.StartInfo.Arguments = _ffCombiner.GetCommandString(FfmpegStreamTypeEnum.rtspTortp_codecWithSsrc);
            _ffmpegProcess.Start();

            _ffmpegProcess.BeginOutputReadLine();
            _ffmpegProcess.BeginErrorReadLine();
        }

        private void ConfigureFfMpegCombiner()
        {
            // TODO: FFmpegBuilderConfig. Class with configuration that puts inside the Combiner.
            _ffCombiner.SSRC = SSRC;
            _ffCombiner.EXE_PATH = Path.Combine(_ffmpegOptions.BinaryFolder, "ffmpeg");
            _ffCombiner.RTSP = _rtspAddress;
            _ffCombiner.VideoCodec = CodecEnum.h264.ToString("g");
            // server - default Ip.loopback
            // ssrc - default 
            _ffCombiner.FfmpegAppOptions = _ffmpegOptions;
            _ffCombiner.RTP_PORT = _videoPort;
            _ffCombiner.SdpFullPath = Path.Combine(_ffmpegOptions.SdpFolder, SdpFilePath);
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

        public void Dispose()
        {
            KillProcess();
        }
        public void KillProcess()
        {
            _ffmpegProcess.Kill();
        }
    }
}
