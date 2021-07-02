using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestGameMain.Mod.mm
{
    public class WingHttpServer
    {
        private static bool runServer = true;
        public static HttpListener listener;
        public static string url = "http://localhost:10030/";
        public static int pageViews = 0;
        public static int requestCount = 0;

        public static string pageData =
            "<!DOCTYPE>" +
            "<html>" +
            "  <head>" +
            "    <title>HttpListener Example</title>" +
            "  </head>" +
            "  <body>" +
            "    <p>Page Views: {0}</p>" +
            "    <form method=\"post\" action=\"shutdown\">" +
            "      <input type=\"submit\" value=\"Shutdown\" {1}>" +
            "    </form>" +
            "  </body>" +
            "</html>";


        private static void HandleIncomingConnections()
        {
            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = listener.GetContext();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Print out some info about the request
                Console.WriteLine("Request #: {0}", ++requestCount);
                Console.WriteLine(req.Url.ToString());
                // Console.WriteLine(req.HttpMethod);
                // Console.WriteLine(req.UserHostName);
                // Console.WriteLine(req.UserAgent);
                // Console.WriteLine();

                // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/shutdown"))
                {
                    Console.WriteLine("Shutdown requested");
                    runServer = false;
                }

                byte[] data = Encoding.UTF8.GetBytes(String.Format(pageData, pageViews, 0));
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                // Write out to the response stream (asynchronously), then close it
                resp.OutputStream.Write(data, 0, data.Length);
                resp.Close();
            }
        }

        public static void Start()
        {
            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            Thread thread1 = new Thread(HandleIncomingConnections);
            thread1.Start();
        }

        public static void Stop()
        {
            runServer = false;
        }
    }
}