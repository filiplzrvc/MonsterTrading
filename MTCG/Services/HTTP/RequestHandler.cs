using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace MTCG.Services.HTTP
{
    public class RequestHandler
    {
        private readonly Register registerService;
        private readonly Login loginService;

        public RequestHandler(Register registerService, Login loginService)
        {
            this.registerService = registerService;
            this.loginService = loginService;
        }

        public void HandleRequest(string method, string path, string body, StreamWriter writer)
        {
            switch (method)
            {
                case "POST" when path == "/users":
                    HandleRegister(body, writer);
                    break;
                case "POST" when path == "/sessions":
                    HandleLogin(body, writer);
                    break;
                case "GET" when path == "/":
                    HandleRoot(writer);
                    break;
                default:
                    SendResponse(writer, "HTTP/1.0 404 Not Found", "Error: Path not found", "text/plain");
                    break;
            }
        }

        private void HandleRegister(string body, StreamWriter writer)
        {
            var userData = JsonConvert.DeserializeObject<UserData>(body);

            if (userData == null || string.IsNullOrEmpty(userData.Username) || string.IsNullOrEmpty(userData.Password))
            {
                SendResponse(writer, "HTTP/1.0 400 Bad Request", "{\"error\": \"Invalid user data\"}", "application/json");
                return;
            }

            string responseMessage = registerService.RegisterUser(userData.Username, userData.Password);

            string statusCode = responseMessage.Contains("User registered successfully")
                ? "HTTP/1.0 201 Created"
                : "HTTP/1.0 409 Conflict";

            SendResponse(writer, statusCode, responseMessage, "application/json");
        }

        private void HandleLogin(string body, StreamWriter writer)
        {
            var userData = JsonConvert.DeserializeObject<UserData>(body);

            if (userData == null || string.IsNullOrEmpty(userData.Username) || string.IsNullOrEmpty(userData.Password))
            {
                SendResponse(writer, "HTTP/1.0 400 Bad Request", "{\"error\": \"Invalid user data\"}", "application/json");
                return;
            }

            string token = loginService.LoginUser(userData.Username, userData.Password);
            string statusCode = token.Contains("token")
                ? "HTTP/1.0 200 OK"
                : "HTTP/1.0 401 Unauthorized";

            SendResponse(writer, statusCode, token, "application/json");
        }

        private void HandleRoot(StreamWriter writer)
        {
            string responseBody = "<html><body><h1>Willkommen zum MTCG-Server</h1><p>Der Server funktioniert.</p></body></html>";
            SendResponse(writer, "HTTP/1.0 200 OK", responseBody, "text/html");
        }


        private void SendResponse(StreamWriter writer, string statusCode, string responseBody, string contentType)
        {
            writer.WriteLine(statusCode);
            writer.WriteLine($"Content-Type: {contentType}");
            writer.WriteLine($"Content-Length: {responseBody.Length}");
            writer.WriteLine();
            writer.WriteLine(responseBody);
        }
    }

    public class UserData
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
