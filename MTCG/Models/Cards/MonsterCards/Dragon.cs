using MTCG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models.Cards.MonsterCards
{
    public class Dragon : MonsterCard
    {
        public bool CanFly { get; set; }

        public Dragon() : base("Dragon", 100, "Fire")
        {
            CanFly = true; // Drache kann fliegen, eine spezifische Eigenschaft
        }
    }
}

