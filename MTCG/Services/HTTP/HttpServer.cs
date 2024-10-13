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
                using var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
                using var reader = new StreamReader(client.GetStream());

                // 1. Erste Zeile des HTTP-Requests einlesen
                string? line = reader.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    continue;  // Wenn die Zeile leer ist, warte auf die nächste Verbindung
                }

                var httpParts = line.Split(' ');

                // Füge die Überprüfung der Länge hinzu
                if (httpParts.Length < 3)
                {
                    Console.WriteLine("Invalid HTTP request line.");
                    writer.WriteLine("HTTP/1.0 400 Bad Request");
                    writer.WriteLine("Content-Type: text/plain");
                    writer.WriteLine();
                    writer.WriteLine("Error: Invalid request");
                    continue;  // gehe zur nächsten Verbindung
                }

                var method = httpParts[0];  // z.B. "POST"
                var path = httpParts[1];    // z.B. "/users"
                var version = httpParts[2]; // z.B. "HTTP/1.1"

                Console.WriteLine($"Method: {method}, Path: {path}, Version: {version}");

                // 2. Header des HTTP-Requests lesen
                int contentLength = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0)
                        break;  // Leere Zeile markiert das Ende der HTTP-Header

                    var headerParts = line.Split(':');
                    if (headerParts.Length > 1)
                    {
                        var headerName = headerParts[0];
                        var headerValue = headerParts[1].Trim();
                        Console.WriteLine($"Header: {headerName} = {headerValue}");
                        if (headerName == "Content-Length")
                        {
                            if (!int.TryParse(headerValue, out contentLength))
                            {
                                Console.WriteLine("Invalid Content-Length header.");
                                writer.WriteLine("HTTP/1.0 400 Bad Request");
                                writer.WriteLine("Content-Type: text/plain");
                                writer.WriteLine();
                                writer.WriteLine("Error: Invalid Content-Length");
                                continue;
                            }
                        }
                    }
                }

                // 3. Body des HTTP-Requests einlesen (falls vorhanden)
                StringBuilder requestBody = new StringBuilder();
                if (contentLength > 0)
                {
                    char[] buffer = new char[contentLength];
                    int totalBytesRead = 0;
                    while (totalBytesRead < contentLength)
                    {
                        var bytesRead = reader.Read(buffer, totalBytesRead, contentLength - totalBytesRead);
                        if (bytesRead == 0)
                        {
                            break;  // Keine weiteren Daten vorhanden
                        }
                        totalBytesRead += bytesRead;
                    }
                    requestBody.Append(buffer);
                    Console.WriteLine($"Request Body: {requestBody.ToString()}");
                }

                // 4. Übergibt die Anfrage an den RequestHandler
                _requestHandler.HandleRequest(method, path, requestBody.ToString(), writer);
            }
        }
    }
}
