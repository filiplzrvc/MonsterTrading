using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models
{
    public class MonsterCard : Card
    {

        public string ElementType { get; set; }
        public MonsterCard(string name, double damage, string elementype) : base(name, damage) 
        {
            ElementType = elementype;
        }
        public override string GetCardType()
        {
            return "Monster";
        }
    }
}
