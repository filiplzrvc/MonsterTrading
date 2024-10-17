using System.Net;
using System.Net.Sockets;
using System.Text;
using MTCG.Services;
using MTCG.Services.HTTP;

class Program
{
    static void Main(string[] args)
    {
        var db = new Database();
        db.InitializeDatabase();

        var registerService = new Register(db);
        var loginService = new Login(db);

        // Initialisiere den RequestHandler
        var requestHandler = new RequestHandler(registerService, loginService);

        // Initialisiere und starte den HTTP-Server
        var httpServer = new HttpServer(requestHandler);
        httpServer.Start();  // Server starten
    }
}



