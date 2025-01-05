using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;   
        public int Coins { get; set; } = 20;
        public int Elo { get; set; } = 100;
        public int GamesPlayed { get; set; } = 0;
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public string? AuthToken { get; set; }
        public Stack UserStack { get; set; } = new Stack();
        public List<Card> Deck { get; set; } = new List<Card>();
        public string OriginalUsername { get; set; }

        public User() { }
        public User(string Username, string Password)
        {
            this.Username = Username;
            this.Password = Password;
            UserStack = new Stack();
            Deck = new List<Card>();
        }

        

        public void AddCardToDeck(Card card)
        {
            if (Deck.Count < 4)
            {
                Deck.Add(card);
            }
        }

        public void UpdateElo (bool hasWon)
        {
            if (hasWon)
            {
                Elo += 3;
                Wins += 3;
            }
            else 
            {
                Elo -= 5;
                Losses++;
            }
        }

        public void BuyCardPackage(List<Card> package)
        {
            if(Coins >= 5)
            {
                Coins -= 5;
                foreach(var card in package)
                {
                    UserStack.AddCard(card);
                }
            }
            else
            {
                Console.WriteLine("Nicht genug Coins, um ein Kartenpaket zu kaufen.");
            }
        }
    }
}
