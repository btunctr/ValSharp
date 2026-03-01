using System;
using System.Collections.Generic;
using System.Text;

namespace ValSharp.Content;

public class CompetitiveTier
{
    public string Uuid { get; set; }
    public string AssetObjectName { get; set; }
    public Tier[] Tiers { get; set; }
    public string AssetPath { get; set; }

    public class Tier
    {
        public int tier { get; set; }
        public string TierName { get; set; }
        public string Division { get; set; }
        public string DivisionName { get; set; }
        public string Color { get; set; }
        public string BackgroundColor { get; set; }
        public string SmallIcon { get; set; }
        public string LargeIcon { get; set; }
        public string RankTriangleDownIcon { get; set; }
        public string RankTriangleUpIcon { get; set; }
    }
}