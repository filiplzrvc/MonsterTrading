﻿using MTCG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models.Cards.MonsterCards
{
    public class Wizzard : MonsterCard
    {
        public bool CanControlOrks { get; set; }

        public Wizzard() : base("Wizzard", 75, "Normal")
        {
            CanControlOrks = true; // Zauberer können Orks kontrollieren
        }
    }
}
