using SIPSorcery.Net;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;

namespace WithCombiner
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
            var handler = OnMessageReceived;
            if (handler == null) return;
            OnMessageReceived(this.Context, pc, e.Data);
        }

        protected override async void OnOpen()
        {
            var handler = WebSocketOpened;
            if (handler == null) return;
            pc = await WebSocketOpened(this.Context);
        }
    }
}
