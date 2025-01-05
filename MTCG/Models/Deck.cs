using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models
{
    public class Deck
    {
        public List<Card> Cards { get; private set; }

        public Deck()
        {
            Cards = new List<Card>();
        }

        public bool AddCard(Card card)
        {
            if (Cards.Count >= 4)
            {
                return false; // Deck kann nicht mehr als 4 Karten enthalten
            }

            Cards.Add(card);
            return true;
        }

        public bool RemoveCard(Card card)
        {
            return Cards.Remove(card);
        }

        public bool IsFull()
        {
            return Cards.Count == 4;
        }
    }
}
