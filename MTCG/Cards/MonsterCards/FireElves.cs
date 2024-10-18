using MTCG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Cards.MonsterCards
{
    public class FireElves : MonsterCard
    {
        public bool CanEvadeDragons { get; set; }

        public FireElves() : base("Fire Elves", 60, "Fire")
        {
            CanEvadeDragons = true; // Feuerelfen können Drachen ausweichen
        }
    }
}
