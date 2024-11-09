using MTCG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models.Cards.SpellCards
{
    public class WaterSpell : SpellCard
    {
        public bool IsEffectiveAgainstFire { get; set; }

        public WaterSpell() : base("Water Spell", 45, "Water")
        {
            IsEffectiveAgainstFire = true; // Wasserzauber ist besonders effektiv gegen Feuerkreaturen
        }
    }
}
