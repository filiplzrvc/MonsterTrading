using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models
{
    public class Stack
    {
        public List<Card> Cards { get; set; }

        public Stack() 
        {
            Cards = new List<Card>();
        }

        public void AddCard(Card card) 
        {
            Cards.Add(card);
        }

        public void RemoveCard(Card card)
        {
            Cards.Remove(card);
        }
    }
}
