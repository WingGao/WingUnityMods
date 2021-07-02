using System.Threading;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace NEON.UI
{
    public class WingWS : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            var msg = $"Echo: {e.Data}";
            Send(msg);
        }

        static void Run()
        {
            var wssv = new WebSocketServer("ws://0.0.0.0:10031");
            wssv.AddWebSocketService<WingWS>("/wing");
            wssv.Start();
        }

        public static void Start()
        {
            Thread thread = new Thread(Run);
            thread.Start();
        }
    }
}