﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models
{
    public class MonsterCard : Card
    {
        public MonsterCard(string name, double damage, string elementype) : base(name, damage, elementype) {}
        public override string GetCardType()
        {
            return "Monster";
        }

        // Factory-Methode
        public static MonsterCard CreateFromJson(dynamic rawCard)
        {
            return new MonsterCard(rawCard.Name, (double)rawCard.Damage, rawCard.ElementType ?? "Neutral");
        }
    }
}
