using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValSharp.DTOs
{
    public class MatchmakingData
    {
        public string QueueID { get; set; }
        public string[] PreferredGamePods { get; set; }
        public int SkillDisparityRRPenalty { get; set; }
    }
}
