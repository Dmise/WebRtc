//-----------------------------------------------------------------------------
// Filename: Program.cs
//
// Description: Listens on an RTP socket for a feed from ffmpeg and forwards it
// to a WebRTC peer.
//
// Author(s):
// Aaron Clauson (aaron@sipsorcery.com)
// 
// History:
// 08 Jul 2020	Aaron Clauson	Created, Dublin, Ireland.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.Extensions.Logging;
using SIPSorcery.Net;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;
using System.Windows;
using System.Runtime.Intrinsics.Arm;

namespace SIPSorcery.Examples
{
    public class WebRtcClient : WebSocketBehavior
    {
        public RTCPeerConnection pc;

        public event Func<WebSocketContext, Task<RTCPeerConnection>> WebSocketOpened;
        public event Action<WebSocketContext, RTCPeerConnection, string> OnMessageReceived;

        public WebRtcClient()
        { }

        protected override void OnMessage(MessageEventArgs e)
        {
            OnMessageReceived(this.Context, pc, e.Data);
        }

        protected override async void OnOpen()
        {
            base.OnOpen();
            pc = await WebSocketOpened(this.Context);
        }
    }

    class Program
    {
        private const uint SSRC_REMOTE_VIDEO = 38106908;
        private const int WEBSOCKET_PORT = 8081;
        private static IPAddress WsAddress = IPAddress.Loopback;
        private static SDPAudioVideoMediaFormat videoFormatRTP;
        private static SDPAudioVideoMediaFormat videoFormatPC;
        // 
        //  lavfi  -> filtercomplex
        // -re (input) - Read input at native frame rate.
        // -f fmt (input/output) force input or output file format. 
        // -i url (input) input file url
        // -y (global) overwrite output files without asking
        // -r force frame rate (valid for raw formats) 'ffmpeg -r 1 -i input.m2v'
        // c[:stream]  codec (input/output,perstream) ;  codec[:stream_specifier]
        //private const string FFMPEG_DEFAULT_COMMAND = "ffmpeg -re -f lavfi -i testsrc=size=640x480:rate=10 -vcodec {0} -pix_fmt yuv420p -strict experimental -g 1 -ssrc {2} -f rtp rtp://127.0.0.1:{1} -sdp_file {3}";
        // ffmpegArgs = $"-loglevel verbose -rtsp_transport tcp -protocol_whitelist rtp,udp,tcp " +
        //            $"-i {_rtspStreamAddress} -vcodec {VideoCodec} -acodec aac -movflags +faststart {fileName}";
        // -reorder_queue_size 4000 
        // -max_delay 10000000
        private const string FFMPEG_PREVEW = "-re -f lavfi -i testsrc=size=640x480:rate=10";
        private const string RTSP_CAM = "rtsp://admin:HelloWorld4@192.168.1.64:554/ISAPI/Streaming/Channels/101";  // -c:v h264
        private const string FFMPEG_DEFAULT_COMMAND = "ffmpeg {0} -vcodec {1} -f rtp rtp://127.0.0.1:{2} -sdp_file {3}";
        // ffmpeg -i rtsp://admin:HelloWorld4@192.168.1.64:554/ISAPI/Streaming/Channels/101 -vcodec vp8 -f rtp rtp://127.0.0.1:8081 -sdp_file ffmpeg.sdp
        // cc
        private const string FFMPEG_SDP_FILE = "ffmpeg.sdp";
        private const int FFMPEG_DEFAULT_RTP_PORT = 5020;

        /// <summary>
        /// The codec to pass to ffmpeg via the command line. WebRTC supported options are:
        /// - vp8
        /// - vp9
        /// - h264
        /// Note if you change this option you will need to delete the ffmpeg.sdp file.
        /// </summary>
        private const string FFMPEG_VP8_CODEC = "vp8";
        private const string FFMPEG_VP9_CODEC = "vp9";
        private const string FFMPEG_H264_CODEC = "h264";
        private const string FFMPEG_DEFAULT_CODEC = FFMPEG_H264_CODEC;

        private static Microsoft.Extensions.Logging.ILogger logger = NullLogger.Instance;

        private static WebSocketServer _webSocketServer;
        private static SDPAudioVideoMediaFormat _ffmpegVideoFormat;
        private static RTPSession _ffmpegListener;
        private bool _runningSession = false;
        private static Action<RTCPeerConnection,Action<IPEndPoint, SDPMediaTypesEnum, RTPPacket>> SubUnscriber;
        private static CancellationTokenSource exitCts = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            string videoCodec = FFMPEG_DEFAULT_CODEC;

            if (args?.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    case FFMPEG_VP8_CODEC:
                    case FFMPEG_VP9_CODEC:
                    case FFMPEG_H264_CODEC:
                        videoCodec = args[0].ToLower();
                        break;

                    default:                        
                        Console.WriteLine($"Video codec option not recognised. Valid values are {FFMPEG_VP8_CODEC}, {FFMPEG_VP9_CODEC} and {FFMPEG_H264_CODEC}. Using {videoCodec}.");
                        break;
                }
            }
        
            logger = AddConsoleLogger();
            
            string ffmpegCommand = String.Format(FFMPEG_DEFAULT_COMMAND, FFMPEG_PREVEW, videoCodec, FFMPEG_DEFAULT_RTP_PORT, FFMPEG_SDP_FILE);

            // Start web socket.
            Console.WriteLine("Starting web socket server...");
            Console.WriteLine($"WebSocket Address: {WsAddress.MapToIPv4().ToString()}:{WEBSOCKET_PORT}");
            _webSocketServer = new WebSocketServer(WsAddress, WEBSOCKET_PORT);            
            _webSocketServer.AddWebSocketService<WebRtcClient>("/", (client) =>
            {
                client.WebSocketOpened += SendOffer;
                client.OnMessageReceived += WebSocketMessageReceived;                
            });
            

            // SDP Check
            if (File.Exists(FFMPEG_SDP_FILE))
            {
                var sdp = SDP.ParseSDPDescription(File.ReadAllText(FFMPEG_SDP_FILE));
                var videoAnn = sdp.Media.Single(x => x.Media == SDPMediaTypesEnum.video);
                if (videoAnn.MediaFormats.Values.First().Name().ToLower() != videoCodec)
                {
                    logger.LogWarning($"Removing existing ffmpeg SDP file {FFMPEG_SDP_FILE} due to codec mismatch.");
                    File.Delete(FFMPEG_SDP_FILE);
                }
            }

            Console.WriteLine("Start ffmpeg using the command below and then initiate a WebRTC connection from the browser");
            
            Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);            
            var exePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var sdpFilePath = String.Concat("\"", exePath, "/ffmpeg.sdp", "\"").Replace("\\", "/");
            //Console.WriteLine($"ffmpeg -re -an -i rtsp://admin:HelloWorld4@192.168.1.64:554/ISAPI/Streaming/Channels/101 -vcodec h264 -muxdelay 0.1 -use_wallclock_as_timestamps 1 -f rtp rtp://127.0.0.1:{FFMPEG_DEFAULT_RTP_PORT} -sdp_file {sdpFilePath}");
            Console.WriteLine($"ffmpeg -re -an -i {RTSP_CAM} -vcodec {FFMPEG_H264_CODEC} -muxdelay 0.1 -use_wallclock_as_timestamps 1 -ssrc {SSRC_REMOTE_VIDEO} -f rtp rtp://{WsAddress.MapToIPv4().ToString()}:{FFMPEG_DEFAULT_RTP_PORT} -sdp_file {sdpFilePath}");

            if (!File.Exists(FFMPEG_SDP_FILE))
            {                           
                Console.WriteLine($"Waiting for {FFMPEG_SDP_FILE} to appear...");
            }

            await Task.Run(() => StartFfmpegListener(FFMPEG_SDP_FILE, exitCts.Token));

            Console.WriteLine($"ffmpeg listener successfully created on port {FFMPEG_DEFAULT_RTP_PORT} with video format {_ffmpegVideoFormat.Name()}.");

            _webSocketServer.Start();

            Console.WriteLine();
            Console.WriteLine($"Waiting for browser web socket connection to {_webSocketServer.Address}:{_webSocketServer.Port}...");

            // Wait for a signal saying the call failed, was cancelled with ctrl-c or completed.
            await Task.Run(() => OnKeyPress(exitCts.Token));

            _webSocketServer.Stop();
        }

        private static async Task StartFfmpegListener(string sdpPath, CancellationToken cancel)
        {
            while (!File.Exists(FFMPEG_SDP_FILE) && !cancel.IsCancellationRequested)
            {
                await Task.Delay(500);
            }

            if (!cancel.IsCancellationRequested)
            {
                var sdp = SDP.ParseSDPDescription(File.ReadAllText(FFMPEG_SDP_FILE));
               
                _ffmpegListener = new RTPSession(false, false, false, IPAddress.Loopback, FFMPEG_DEFAULT_RTP_PORT);
                _ffmpegListener.AcceptRtpFromAny = true;

                // The SDP is only expected to contain a single video media announcement.
                // Add Track to RTPSession
                var videoAnn = sdp.Media.Single(x => x.Media == SDPMediaTypesEnum.video);
                videoFormatRTP = videoAnn.MediaFormats.Values.First();
                _ffmpegVideoFormat = videoFormatRTP;
                MediaStreamTrack videoTrack = new MediaStreamTrack(SDPMediaTypesEnum.video, false, new List<SDPAudioVideoMediaFormat> { videoFormatRTP }, MediaStreamStatusEnum.RecvOnly);
                //videoTrack.Ssrc = SSRC_REMOTE_VIDEO; //   /!\ Need to set the correct SSRC in order to accept RTP stream
                var point1 = _ffmpegListener.VideoLocalTrack;
                _ffmpegListener.addTrack(videoTrack);
                _ffmpegListener.SetRemoteDescription(SIP.App.SdpType.answer, sdp);

                var point2  = _ffmpegListener.VideoLocalTrack;
                // Set a dummy destination end point or the RTP session will end up sending RTCP reports
                // to itself. port = 0 
                var dummyIPEndPoint = new IPEndPoint(IPAddress.Loopback, FFMPEG_DEFAULT_RTP_PORT);
                _ffmpegListener.SetDestination(SDPMediaTypesEnum.video, dummyIPEndPoint, dummyIPEndPoint);

                await _ffmpegListener.Start();
            }
        }

        private static Task OnKeyPress(CancellationToken exit)
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

        private static async Task<RTCPeerConnection> SendOffer(WebSocketContext context)
        {          
            // var state = context.WebSocket.ReadyState;
            logger.LogDebug($"Web socket client connection from {context.UserEndPoint}, sending offer.");

            var sdp = SDP.ParseSDPDescription(File.ReadAllText(FFMPEG_SDP_FILE));
            var videoAnn = sdp.Media.Single(x => x.Media == SDPMediaTypesEnum.video);
            videoFormatPC = videoAnn.MediaFormats.Values.First();
            var areMatch = SDPAudioVideoMediaFormat.AreMatch(videoFormatPC, videoFormatRTP);

            var pc = Createpc(context, videoFormatPC);

            var offerInit = pc.createOffer(null);
            await pc.setLocalDescription(offerInit);

            logger.LogDebug($"Sending SDP offer to client {context.UserEndPoint}.");

            context.WebSocket.Send(offerInit.sdp);
            
            return pc;
        }

        private static RTCPeerConnection Createpc(WebSocketContext context, SDPAudioVideoMediaFormat videoFormat)
        {
            var pc = new RTCPeerConnection(null);

            MediaStreamTrack videoTrack = new MediaStreamTrack(SDPMediaTypesEnum.video, false, new List<SDPAudioVideoMediaFormat> { videoFormat }, MediaStreamStatusEnum.SendOnly);
            //videoTrack.Ssrc = SSRC_REMOTE_VIDEO;
            pc.addTrack(videoTrack);

            pc.onconnectionstatechange += (state) =>
            {
                logger.LogDebug($"Peer connection state changed to {state}.");

                if (state == RTCPeerConnectionState.connected)
                {
                    logger.LogDebug("Creating RTP session to receive ffmpeg stream.");
                    if (_ffmpegListener == null)
                        StartFfmpegListener(FFMPEG_SDP_FILE, exitCts.Token);                    

                    SignOnRtpPacketReceived(pc, true);
                }
                else if (state == RTCPeerConnectionState.disconnected) 
                {
                    SignOnRtpPacketReceived(pc, false);
                    pc.close();
                    pc.Dispose();
                    pc = null;
                    _ffmpegListener.Close("RTCPeerConnectionState.disconnected");
                    _ffmpegListener.Dispose();
                    _ffmpegListener = null;
                }
            };
            return pc;
        }

        private static void WebSocketMessageReceived(WebSocketContext context, RTCPeerConnection pc, string message)
        {
            try
            {
                if (pc.remoteDescription == null)
                {
                    logger.LogDebug("Answer SDP: " + message);
                    pc.setRemoteDescription(new RTCSessionDescriptionInit { sdp = message, type = RTCSdpType.answer });
                }              
            }
            catch (Exception excp)
            {
                logger.LogError("Exception WebSocketMessageReceived. " + excp.Message);
            }
        }

        /// <summary>
        ///  Adds a console logger. Can be omitted if internal SIPSorcery debug and warning messages are not required.
        /// </summary>
        private static Microsoft.Extensions.Logging.ILogger AddConsoleLogger()
        {
            var serilogLogger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug)
                .WriteTo.Console()
                .CreateLogger();
            var factory = new SerilogLoggerFactory(serilogLogger);
            SIPSorcery.LogFactory.Set(factory);
            return factory.CreateLogger<Program>();
        } 
        
        private static void SignOnRtpPacketReceived(RTCPeerConnection pc, bool subscribe)
        {
            if(subscribe)
                _ffmpegListener.OnRtpPacketReceived += Handle;
            else
                _ffmpegListener.OnRtpPacketReceived -= Handle; // TODO почему-то не отписывает

            void Handle(IPEndPoint ep, SDPMediaTypesEnum media, RTPPacket rtpPkt)
            {
                if (media == SDPMediaTypesEnum.video && pc.VideoDestinationEndPoint != null)
                {
                    //logger.LogDebug($"Forwarding {media} RTP packet to webrtc peer timestamp {rtpPkt.Header.Timestamp}.");
                    pc.SendRtpRaw(media, rtpPkt.Payload, rtpPkt.Header.Timestamp, rtpPkt.Header.MarkerBit, rtpPkt.Header.PayloadType);
                }
            }
        }      
    }
}
