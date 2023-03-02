using SIPSorcery.Net;
using System.Net;
using WebSocketSharp.Server;
using WebSocketSharp.Net.WebSockets;
using System.Diagnostics;
using ToolzLib;

namespace WithCombiner
{
    internal class WSSignalingServer
    {
        private WebSocketServer _webSocketServer;
        private IPAddress _wsAddress;
        private int _wsPort;
        private string _sdpFile;
        private RTCPeerConnection _pc;
        private FFmpegListener _ffmpegListener;
        private Toolz _toolz;

        public WSSignalingServer(IPAddress address, int port, string sdpFilePath, FFmpegListener listener)
        {
            _wsAddress= address;
            _wsPort= port;
            _sdpFile= sdpFilePath;
            _ffmpegListener = listener;
        }
        public void Run()
        {
            var _process = true;
            _webSocketServer = new WebSocketServer(_wsAddress, _wsPort);
            _webSocketServer.AddWebSocketService<WebRtcClient>("/", (client) =>
            {
                client.SocketOpened += SendOffer;
                client.MessageReceived += WebSocketMessageReceived;
            });

            _webSocketServer.Start();

            Console.WriteLine();
            Console.WriteLine($"Waiting for browser web socket connection to {_webSocketServer.Address}:{_webSocketServer.Port}...");

            // Wait for a signal saying the call failed, was cancelled with ctrl-c or completed.
            

            while (_process)
            {
                var key = Console.ReadKey();
                if (key.KeyChar == 'q' )
                {
                    _process = false;
                }
            }
        }
     
        private Task OnKeyPress(CancellationToken exit)
        {
            while (!exit.WaitHandle.WaitOne(0))
            {
                var keyProps = Console.ReadKey();

                if (keyProps.KeyChar == 'q')
                {
                    // Quit application.
                    Console.WriteLine("Quitting");
                    break;
                }
            }
            return Task.CompletedTask;
        }

        private async Task<RTCPeerConnection> SendOffer(WebSocketContext context)
        {
            // var state = context.WebSocket.ReadyState;
            
            var sdp = SDP.ParseSDPDescription(File.ReadAllText(_sdpFile));
            var videoAnn = sdp.Media.Single(x => x.Media == SDPMediaTypesEnum.video);
            var videoFormatSDP = videoAnn.MediaFormats.Values.First();
            
            var pc = Createpc(videoFormatSDP);

            var offerInit = pc.createOffer(null);
            await pc.setLocalDescription(offerInit);            

            context.WebSocket.Send(offerInit.sdp);
            _pc = pc;
            return pc;
        }

        private RTCPeerConnection Createpc(SDPAudioVideoMediaFormat videoFormat)
        {
            var pc = new RTCPeerConnection(null);

            MediaStreamTrack videoTrack = new MediaStreamTrack(
                                        SDPMediaTypesEnum.video,
                                        false,
                                        new List<SDPAudioVideoMediaFormat> { videoFormat },
                                        MediaStreamStatusEnum.SendOnly);
            //videoTrack.Ssrc = SSRC_REMOTE_VIDEO;
            pc.addTrack(videoTrack);

            pc.onconnectionstatechange += (state) =>
            {             
                if (state == RTCPeerConnectionState.connected)
                {
                                                       
                    SignOnRtpPacketReceived(true);
                }
                else if (state == RTCPeerConnectionState.disconnected)
                {
                    SignOnRtpPacketReceived(false);
                    pc.close();
                    pc.Dispose();
                    pc = null;                  
                }
            };
            
            return pc;
        }

        private async Task WebSocketMessageReceived(WebSocketContext context, RTCPeerConnection pc, string message)
        {
            try
            {
                if (pc.remoteDescription == null)
                {                  
                    pc.setRemoteDescription(new RTCSessionDescriptionInit { sdp = message, type = RTCSdpType.answer });
                }
            }
            catch (Exception excp)
            {
                
            }
        }

        private void SignOnRtpPacketReceived(bool subscribe)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (subscribe)
                {                   
                   _ffmpegListener.OnRtpPacketReceivedEvnt += HandlerNewRTPPacket;                                     
                   Console.WriteLine("_ffmpegListner(RTP Session does not started.)");                   
                }
                else
                {
                    Console.WriteLine("Unsubscribe OnRtpPacketReceived");
                    _ffmpegListener.OnRtpPacketReceivedEvnt -= HandlerNewRTPPacket;
                }
            }
            catch (Exception ex)
            {
                
            }
        }

        private void HandlerNewRTPPacket(IPEndPoint ep, SDPMediaTypesEnum media, RTPPacket rtpPkt)
        {
            
            if (media == SDPMediaTypesEnum.video && _pc?.VideoDestinationEndPoint != null)
            {                
                _pc.SendRtpRaw(media, rtpPkt.Payload, rtpPkt.Header.Timestamp, rtpPkt.Header.MarkerBit, rtpPkt.Header.PayloadType);
            }
            else
            {
                Console.WriteLine($"if clause (media == SDPMediaTypesEnum.video && _pc.VideoDestinationEndPoint != null) : {media == SDPMediaTypesEnum.video && _pc.VideoDestinationEndPoint != null} ");
            }
        }
    }  
}
