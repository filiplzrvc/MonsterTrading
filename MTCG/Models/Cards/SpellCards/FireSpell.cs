﻿using MTCG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models.Cards.SpellCards
{
    public class FireSpell : SpellCard
    {
        public bool IsEffectiveAgainstWater { get; set; }

        public FireSpell() : base("Fire Spell", 40, "Fire")
        {
            IsEffectiveAgainstWater = true; // Feuerzauber ist besonders effektiv gegen Wasserkreaturen
        }
    }
}
