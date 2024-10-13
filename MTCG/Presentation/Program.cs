using System.Net;
using System.Net.Sockets;
using System.Text;
using MTCG.Services;
using MTCG.Services.HTTP;

class Program
{
    static void Main(string[] args)
    {
        var registerService = new Register();
        var loginService = new Login(registerService.GetUsers());

        // Initialisiere den RequestHandler
        var requestHandler = new RequestHandler(registerService, loginService);

        // Initialisiere und starte den HTTP-Server
        var httpServer = new HttpServer(requestHandler);
        httpServer.Start();  // Server starten
    }
}



