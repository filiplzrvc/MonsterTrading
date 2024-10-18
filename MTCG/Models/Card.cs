using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models
{
    public abstract class Card
    {
        public string Name { get; set; }
        public double Damage { get; set; }
        public string ElementType { get; set; }

        public Card(string name, double damage, string elementType)
        {
            Name = name;
            Damage = damage;
            ElementType = elementType;
        }

        public abstract string GetCardType();
    }
}
