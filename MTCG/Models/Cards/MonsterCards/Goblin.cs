using MTCG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models.Cards.MonsterCards
{
    public class Goblin : MonsterCard
    {
        public bool IsAfraidOfDragons { get; set; }

        public Goblin() : base("Goblin", 50, "Normal")
        {
            IsAfraidOfDragons = true; // Goblins haben Angst vor Drachen
        }
    }
}
