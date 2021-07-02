using WebSocketSharp;
using WebSocketSharp.Server;

namespace WingGao.Mod
{
    public class WingWS:WebSocketBehavior
    {
        
        protected override void OnMessage (MessageEventArgs e)
        {
            var msg = $"Echo: {e.Data}";
            Send (msg);
        }
        
        public static void Start()
        {
            var wssv = new WebSocketServer ("ws://0.0.0.0:10031");
            wssv.AddWebSocketService<WingWS> ("/wing");
            wssv.Start();
        }
    }
}