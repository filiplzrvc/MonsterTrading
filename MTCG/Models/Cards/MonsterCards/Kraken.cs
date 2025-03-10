﻿using MTCG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models.Cards.MonsterCards
{
    public class Kraken : MonsterCard
    {
        public bool IsImmuneToSpells { get; set; }

        public Kraken() : base("Kraken", 80, "Water")
        {
            IsImmuneToSpells = true; // Kraken ist immun gegen Zauberkarten
        }
    }
}
