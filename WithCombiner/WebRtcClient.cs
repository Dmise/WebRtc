using SIPSorcery.Net;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;

namespace WithCombiner
{
    internal class WebRtcClient : WebSocketBehavior
    {
        private RTCPeerConnection _pc;

        public event Func<WebSocketContext, Task<RTCPeerConnection>> SocketOpened;
        public event Func<WebSocketContext, RTCPeerConnection, string, Task> MessageReceived;

        public WebRtcClient()
        {

        }

        protected override void OnMessage(MessageEventArgs e)
        {
            var handler = MessageReceived;
            if (handler == null) return;
            MessageReceived(Context, _pc, e.Data);
        }

        protected override async void OnOpen()
        {
            var handler = SocketOpened;
            if (handler == null) return;
            _pc = await SocketOpened(Context);
        }
    }
}
