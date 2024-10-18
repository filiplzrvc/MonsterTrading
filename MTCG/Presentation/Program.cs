using System.Net;
using System.Net.Sockets;
using System.Text;
using MTCG.Services;
using MTCG.Services.HTTP;
using MTCG.Services.Database;

class Program
{
    static void Main(string[] args)
    {
        var db = new Datalayer();
        db.InitializeDatabase();

        var registerService = new Register(db);
        var loginService = new Login(db);
        var cardService = new CardService(db);

        // Initialisiere den RequestHandler
        var requestHandler = new RequestHandler(registerService, loginService, cardService);

        // Initialisiere und starte den HTTP-Server
        var httpServer = new HttpServer(requestHandler);
        httpServer.Start();  // Server starten
    }
}



