using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using static System.Net.WebRequestMethods;
using System.Net.Http.Headers;

namespace WithCombiner
{
    public enum CodecEnum
    {
        vp8, vp9, h264
    }
    public class FFMpegAppOptions
    {

        public string BinaryFolder { get; set; }

        public string TempFolder { get; set; }

        public string SdpFolder { get; set; }

        public static FFMpegAppOptions Default => new()
        {
            BinaryFolder = "ffmpeg/bin/",
            TempFolder = "ffmpeg/tmp/",
            SdpFolder = "sdp/"
        };
    }

    internal class FFmpegCommandBuilder
    {
        private string ServerIpString { get { return Server.MapToIPv4().ToString(); } }
        #region Options
        /// <summary>
        /// ffmpeg.exe full filename path
        /// </summary>
        public string EXE_PATH { get; set; }
        public string RTSP { get; set; }
        public string VideoCodec { get; set; }
        public IPAddress Server { get; set; } = IPAddress.Loopback;

        //may be add some logic here. But seems give random ssrc is no good idea. Need to study.
        //if (Ssrc == 0) Ssrc = (uint) new Random().Next(10000000, 100000000);
        public uint SSRC { get; set; } = 38106908;
        public FFMpegAppOptions FfmpegAppOptions { get; set; }
        public int RTP_PORT { get; set; }
        public string SdpFullPath { get; set; }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="rtpPort">rtp stream port</param>
        /// <param name="sdpFilePath"></param>
        public FFmpegCommandBuilder(FFMpegAppOptions options, IPAddress serverIp, int rtpPort, string sdpFilePath)
        {
            Server = serverIp;
            RTP_PORT = rtpPort;
            SdpFullPath = sdpFilePath;
            FfmpegAppOptions = options;
        }
        public FFmpegCommandBuilder(IPAddress serverIp, int rtpPort, string sdpFilePath)
        {
            this.FfmpegAppOptions = new FFMpegAppOptions();
            RTP_PORT = rtpPort;
            SdpFullPath = sdpFilePath;
        }

        public enum FfmpegStreamTypeEnum
        {
            /// <summary>
            /// <para>Template command is: ffmpeg -re -an -i {0} -c copy -f rtp -muxdelay 0.1 -use_wallclock_as_timestamps 1 {1} -sdp_file {2} </para>
            /// 0 - rtsp from camera <br/>
            /// 1 - rtp where ffmpeg restream<br/>
            /// 2 - path to sdp file
            /// </summary>         
            rtspTortp_copy,
            rtspToRtpCodecNoSsrc,
            /// <summary>
            ///  <para>-re -loglevel verbose -rtsp_transport tcp -protocol_whitelist rtp,udp,tcp/ <br />
            /// -vcodec {VideoCodec} -an -i {_rtspStreamAddress} -vcodec {VideoCodec}/ <br/>
            /// -ssrc {Ssrc} -f rtp rtp://127.0.0.1:{VideoPort}/ -sdp_file {SdpFullPath}" </para>
            /// 0 - rtsp from camera <br/>
            /// 1 - rtp where ffmpeg restream<br/>
            /// 2 - path to sdp file
            /// </summary>
            rtspVideoCodec2times,
            rtspTortp_codecWithSsrc
        }

        public FFmpegCommandBuilder() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns>arguments string to set in cmd</returns>
        public string GetCommandString(FfmpegStreamTypeEnum type = FfmpegStreamTypeEnum.rtspTortp_copy)
        {
            switch (type)
            {
                case FfmpegStreamTypeEnum.rtspTortp_copy:
                    return String.Format(argumentDic[type], RTSP, ServerIpString, RTP_PORT, SdpFullPath);
                case FfmpegStreamTypeEnum.rtspToRtpCodecNoSsrc:
                    return String.Format(argumentDic[type], RTSP, ServerIpString, RTP_PORT, SdpFullPath, VideoCodec);
                case FfmpegStreamTypeEnum.rtspVideoCodec2times:
                    return String.Format(argumentDic[type], VideoCodec, RTSP, SSRC, RTP_PORT, SdpFullPath);
                case FfmpegStreamTypeEnum.rtspTortp_codecWithSsrc:
                    return String.Format(argumentDic[type], RTSP, VideoCodec, SSRC, ServerIpString, RTP_PORT, SdpFullPath);
                default:
                    return String.Empty;
            }
        }
        private static Dictionary<FfmpegStreamTypeEnum, string> argumentDic = new Dictionary<FfmpegStreamTypeEnum, string>()
        {
            {
                // 0 - rtsp from camera
                // 1 - rtp where ffmpeg restream
                // 2 - path to sdp file 
                FfmpegStreamTypeEnum.rtspTortp_copy,
                "-re -an -i {0} -c copy -f rtp -muxdelay 0.1 -use_wallclock_as_timestamps 1 -f rtp rtp://{1}:{2} -sdp_file {3}"
            },

            {
                // 0 - rtsp from camera
                // 1 - codec
                // 2 - rtp where ffmpeg restream
                // 3 - path to sdp file 
                FfmpegStreamTypeEnum.rtspToRtpCodecNoSsrc,
                "-re -an -i {0} -vcodec {4} -muxdelay 0.1 -use_wallclock_as_timestamps 1 -f rtp rtp://{1}:{2} -sdp_file {3}"
            },

            {
                // 0 - rtsp from camera
                // 1 - codec
                // 2 - rtp where ffmpeg restream
                // 3 - path to sdp file 
                FfmpegStreamTypeEnum.rtspTortp_codecWithSsrc,
                "-re -an -i {0} -vcodec {1} -muxdelay 0.1 -use_wallclock_as_timestamps 1 -ssrc {2} -f rtp rtp://{3}:{4} -sdp_file {5}"
            },

            {
                // не работает. legacy code
                FfmpegStreamTypeEnum.rtspVideoCodec2times,
                "-re -loglevel verbose -rtsp_transport tcp -protocol_whitelist rtp,udp,tcp " +
                    "-vcodec {1} -an -i {2} " +
                    "-vcodec {1} -ssrc {3} -f rtp rtp://127.0.0.1:{4} -sdp_file {5}"
            }
        };

        // enumeration-format-strings MSDN used instead
        public void CodecToString(CodecEnum codec)
        {
            switch (codec)
            {
                case CodecEnum.vp8:
                    this.VideoCodec = "vp8";
                    break;
                case CodecEnum.vp9:
                    this.VideoCodec = "vp9";
                    break;
                case CodecEnum.h264:
                    this.VideoCodec = "h264";
                    break;
                default:
                    this.VideoCodec = "h264";
                    break;
            }
        }     
    }
}
