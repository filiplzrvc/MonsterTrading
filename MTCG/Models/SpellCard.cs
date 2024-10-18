using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models
{
    public class SpellCard : Card
    {
        public SpellCard(string name, double damage, string elementType) : base(name, damage, elementType){}

        public override string GetCardType()
        {
            return "Spell";
        }
    }
}
