using MTCG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Cards.MonsterCards
{
    public class Knights : MonsterCard
    {
        public bool IsVulnerableToWater { get; private set; }

        public Knights() : base("Knight", 70, "Normal")
        {
            IsVulnerableToWater = true; // Ritter sind anfällig für Wasserzauber
        }
    }
}