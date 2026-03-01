using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValSharp.DTOs
{
    public class MMR
    {
        public string Version { get; set; }
        public string Subject { get; set; }
        public bool NewPlayerExperienceFinished { get; set; }
        public Dictionary<string, QueueSkillsData> QueueSkills { get; set; }
        public LatestCompetitiveUpdateData LatestCompetitiveUpdate { get; set; }
        public bool IsLeaderboardAnonymized { get; set; }
        public bool IsActRankBadgeHidden { get; set; }

        public class QueueSkillsData
        {
            public int TotalGamesNeededForRating { get; set; }
            public int TotalGamesNeededForLeaderboard { get; set; }
            public int CurrentSeasonGamesNeededForRating { get; set; }
            public Dictionary<string, SeasonalInfoData> SeasonalInfoBySeasonID { get; set; }
        }

        public class SeasonalInfoData
        {
            public string SeasonID { get; set; }
            public int NumberOfWins { get; set; }
            public int NumberOfWinsWithPlacements { get; set; }
            public int NumberOfGames { get; set; }
            public int Rank { get; set; }
            public int CapstoneWins { get; set; }
            public int LeaderboardRank { get; set; }
            public int CompetitiveTier { get; set; }
            public int RankedRating { get; set; }
            public Dictionary<string, int> WinsByTier { get; set; }
            public int GamesNeededForRating { get; set; }
            public int TotalWinsNeededForRank { get; set; }
        }

        public class LatestCompetitiveUpdateData
        {
            public string MatchID { get; set; }
            public string MapID { get; set; }
            public string SeasonID { get; set; }
            public long MatchStartTime { get; set; }
            public int TierAfterUpdate { get; set; }
            public int TierBeforeUpdate { get; set; }
            public int RankedRatingAfterUpdate { get; set; }
            public int RankedRatingBeforeUpdate { get; set; }
            public int RankedRatingEarned { get; set; }
            public int RankedRatingPerformanceBonus { get; set; }
            public string CompetitiveMovement { get; set; }
            public int AFKPenalty { get; set; }
        }
    }
}
