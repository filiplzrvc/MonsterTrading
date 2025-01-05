using System.Net;
using System.Net.Sockets;
using System.Text;
using MTCG.Services.HTTP;

namespace MTCG.Services
{
    public class HttpServer
    {
        private readonly RequestHandler _requestHandler;
        private readonly int _port;

        public HttpServer(RequestHandler requestHandler, int port = 10001)
        {
            _requestHandler = requestHandler;
            _port = port;
        }

        public void Start()
        {
            Console.WriteLine($"HttpServer started: use http://localhost:{_port}/");

            var server = new TcpListener(IPAddress.Any, _port);
            server.Start();

            while (true)
            {
                var client = server.AcceptTcpClient();

                // Erstelle einen neuen Thread für die Client-Verbindung
                var clientThread = new Thread(HandleClient);
                clientThread.Start(client);
            }
        }

        private void HandleClient(object? clientObject)
        {
            if (clientObject is not TcpClient client)
            {
                return;
            }

            using var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
            using var reader = new StreamReader(client.GetStream());

            try
            {
                // 1. Erste Zeile des HTTP-Requests einlesen
                string? line = reader.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    return; // Keine gültige Anfrage, Verbindung schließen
                }

                var httpParts = line.Split(' ');

                if (httpParts.Length < 3)
                {
                    Console.WriteLine("Invalid HTTP request line.");
                    writer.WriteLine("HTTP/1.0 400 Bad Request");
                    writer.WriteLine("Content-Type: text/plain");
                    writer.WriteLine();
                    writer.WriteLine("Error: Invalid request");
                    return;
                }

                var method = httpParts[0];  // z.B. "POST"
                var path = httpParts[1];    // z.B. "/users"
                var version = httpParts[2]; // z.B. "HTTP/1.1"

                Console.WriteLine($"Method: {method}, Path: {path}, Version: {version}");

                // 2. Header des HTTP-Requests lesen
                var headers = new Dictionary<string, string>();
                int contentLength = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0)
                        break; // Leere Zeile markiert das Ende der HTTP-Header

                    var headerParts = line.Split(':', 2);
                    if (headerParts.Length == 2)
                    {
                        var headerName = headerParts[0].Trim();
                        var headerValue = headerParts[1].Trim();
                        headers[headerName] = headerValue;

                        if (headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(headerValue, out var length))
                            {
                                contentLength = length;
                            }
                            else
                            {
                                Console.WriteLine("Invalid Content-Length header.");
                                writer.WriteLine("HTTP/1.0 400 Bad Request");
                                writer.WriteLine("Content-Type: text/plain");
                                writer.WriteLine();
                                writer.WriteLine("Error: Invalid Content-Length");
                                return;
                            }
                        }
                    }
                }

                // 3. Body des HTTP-Requests einlesen (falls vorhanden)
                StringBuilder requestBody = new StringBuilder();
                if (contentLength > 0)
                {
                    char[] buffer = new char[contentLength];
                    reader.ReadBlock(buffer, 0, contentLength);
                    requestBody.Append(buffer);
                }

                // 4. Übergibt die Anfrage an den RequestHandler
                _requestHandler.HandleRequest(method, path, requestBody.ToString(), writer, headers);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client request: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }
    }
}
