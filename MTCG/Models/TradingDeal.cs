using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTCG.Models
{
    public  class TradingDeal
    {
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public Guid CardToTrade { get; set; }
        public string Type { get; set; }
        public double MinimumDamage { get; set; }
    }
}
