﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using MTCG.Services;
using MTCG.Services.HTTP;
using MTCG.Database;
class Program
{
    static void Main(string[] args)
    {
        var db = new Datalayer();
        db.InitializeDatabase();

        var registerService = new RegisterService(db);
        var loginService = new LoginService(db);
        var cardService = new CardService(db);
        var packageService = new PackageService(db, registerService);
        var tradingService = new TradingService(db);
        var battleService = new BattleService(cardService, registerService);

        // Initialisiere den RequestHandler
        var requestHandler = new RequestHandler(registerService, loginService, cardService, packageService, tradingService, battleService);

        // Initialisiere und starte den HTTP-Server
        var httpServer = new HttpServer(requestHandler);
        httpServer.Start();  // Server starten
    }  
}



