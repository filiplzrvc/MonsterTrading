using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models
{
    internal class BattleRoundResult
    {
        public Card Player1Card { get; set; }
        public Card Player2Card { get; set; }
        public string RoundResult { get; set; }
        public int Winner { get; set; } 
    }
}
