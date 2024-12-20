﻿using MTCG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models.Cards.SpellCards
{
    public class NormalSpell : SpellCard
    {
        public bool HasNoElementalEffect { get; set; }

        public NormalSpell() : base("Normal Spell", 30, "Normal")
        {
            HasNoElementalEffect = true; // Normaler Zauber hat keine speziellen Effekte
        }
    }
}
