using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace WithCombiner
{
    internal class FFmpegListener
    {
        private RTPSession _ffmpegListener;
        private string _ffmpegSdpFile;
        private int _rtpPort;
        private uint _ssrc;


        public SDPAudioVideoMediaFormat videoFormatRTP;
        public event Action<IPEndPoint, SDPMediaTypesEnum, RTPPacket> OnRtpPacketReceivedEvnt;
        public FFmpegListener()
        {

        }
        public FFmpegListener(string sdpFilePath, int rtpPort, uint ssrc)
        {
            _ffmpegSdpFile = sdpFilePath;
            _rtpPort = rtpPort;
            _ssrc = ssrc;
        }

        public bool Running
        {
            get
            {              
                return (_ffmpegListener?.IsStarted ?? false) && (!_ffmpegListener?.IsClosed ?? false);
            }
        }
        public void Start()
        {
            while (!File.Exists(_ffmpegSdpFile))
            {
                Task.Delay(500);
            }

            var sdp = SDP.ParseSDPDescription(File.ReadAllText(_ffmpegSdpFile));
            _ffmpegListener = new RTPSession(false, false, false, IPAddress.Loopback, _rtpPort);
            _ffmpegListener.AcceptRtpFromAny = true;

            // The SDP is only expected to contain a single video media announcement.
            // Add Track to RTPSession
            var videoAnn = sdp.Media.Single(x => x.Media == SDPMediaTypesEnum.video);
            videoFormatRTP = videoAnn.MediaFormats.Values.First();

            MediaStreamTrack videoTrack = new MediaStreamTrack(
                                            SDPMediaTypesEnum.video,
                                            false,
                                            new List<SDPAudioVideoMediaFormat> { videoFormatRTP },
                                            MediaStreamStatusEnum.RecvOnly);
            videoTrack.Ssrc = _ssrc; //   /!\ Need to set the correct SSRC in order to accept RTP stream

            _ffmpegListener.addTrack(videoTrack);
            _ffmpegListener.SetRemoteDescription(SIPSorcery.SIP.App.SdpType.answer, sdp);


            // Set a dummy destination end point or the RTP session will end up sending RTCP reports
            // to itself. port = 0 
            var dummyIPEndPoint = new IPEndPoint(IPAddress.Loopback, _rtpPort);
            _ffmpegListener.SetDestination(SDPMediaTypesEnum.video, dummyIPEndPoint, dummyIPEndPoint);
            _ffmpegListener.OnRtpPacketReceived += OnRtpPacketReceived;
            _ffmpegListener.Start();

        }

        private void OnRtpPacketReceived(IPEndPoint arg1, SDPMediaTypesEnum arg2, RTPPacket arg3)
        {
            var handler = OnRtpPacketReceivedEvnt;
            if (OnRtpPacketReceivedEvnt == null) return;
            OnRtpPacketReceivedEvnt.Invoke(arg1, arg2, arg3);
        }
    }
}