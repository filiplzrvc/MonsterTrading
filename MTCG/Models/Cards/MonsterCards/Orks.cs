using MTCG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models.Cards.MonsterCards
{
    public class Orks : MonsterCard
    {
        public bool IsControlledByWizards { get; set; }

        public Orks() : base("Orks", 55, "Normal")
        {
            IsControlledByWizards = true; // Orks können von Zauberern kontrolliert werden
        }
    }
}
