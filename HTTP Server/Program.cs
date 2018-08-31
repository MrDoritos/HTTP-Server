using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HTTP_Server.HTTP;
using System.Net.Sockets;
using System.IO;

namespace HTTP_Server
{
    class Program
    {
        static public string someHTML = 
            "<html><title>Test</title><body><p>Hello World</p><img src=\"/cool.png\"></img><img src=\"/teapot.png\"></img></body></html>";

        static public HTTP.Server server;
        static void Main(string[] args)
        {
            server = new HTTP.Server();
            server.StartServer(new System.Net.IPEndPoint(new System.Net.IPAddress(new byte[] { 127, 0, 0, 1 }), 8080));
            server.requestRecieved += RequestHandler;
            Console.WriteLine($"HTTP Server listening on {server.Hostname}:{server.Port}");
            Task.Delay(-1).GetAwaiter().GetResult();
        }

        static async Task RequestHandler(HttpRequest request, TcpClient client)
        {
            HttpResponse httpResponse = new HttpResponse();
            var header = new Header();
            switch (request.Header.RequestURI.ToLower())
            {
                case "/":
                    if (File.Exists("index.html"))
                    {
                        httpResponse.Content = new Content() { Buffer = File.ReadAllBytes("index.html") };
                    }
                    else
                    {
                        httpResponse.Content = new Content() { Buffer = Encoding.UTF8.GetBytes(someHTML) };
                    }
                    break;
                case "/teapot.png":
                    header.StatusCode = Header.StatusCodes.I_AM_A_TEAPOT;
                    header.SetContentType = Header.ContentTypes.IMG;
                    break;
                default:
                    string req = request.Header.RequestURI.ToLower();
                    var files = Directory.GetFiles(Environment.CurrentDirectory).Select(n => n.Split('\\').Last());
                    if (files.Any(n => req.Contains(n.ToLower())))
                    {                        
                        var file = (files.First(n => req.Contains(n.ToLower())));
                        switch (file.Split('.').Last())
                        {
                            case "png":
                                header.SetContentType = Header.ContentTypes.IMG;
                                break;
                            case "xml":
                                header.SetContentType = Header.ContentTypes.XML;
                                break;
                            case "css":
                                header.SetContentType = Header.ContentTypes.CSS;
                                break;
                        }
                        var bytes = File.ReadAllBytes(Environment.CurrentDirectory.TrimEnd('\\') + "\\" + file);
                        httpResponse.Content = new Content() { Buffer = bytes };                        
                    }
                    else
                    {
                        header.StatusCode = Header.StatusCodes.NOT_FOUND;
                        header.SetContentType = Header.ContentTypes.HTML;
                    }
                    break;
            }            
            httpResponse.Header = header;
            Console.WriteLine($"{(int)httpResponse.Header.StatusCode} {httpResponse.Header.StatusCode} {httpResponse.Header.ContentType} {request.Header.RequestURI}");
            client.Client.Send(httpResponse.Serialize());            
        }
    }
}
