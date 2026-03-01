using System;
using System.Collections.Generic;
using System.Text;

namespace ValSharp_Demo
{
    public static class FuzzyMatcher
    {
        public static IEnumerable<T> GetMatches<T>(IEnumerable<T>? items, string query, Func<T, string> selector, double threshold = 0.7)
        {
            if (items == null) return Enumerable.Empty<T>();

            string lowerQuery = query.ToLower();

            return items
                .Select(item => new { Value = item, Score = CalculateSimilarity(selector(item).ToLower(), lowerQuery) })
                .Where(temp => temp.Score >= threshold)
                .OrderByDescending(temp => temp.Score)
                .Select(temp => temp.Value);
        }

        public static T? GetBestMatch<T>(IEnumerable<T>? items, string query, Func<T, string> selector, double threshold = 0.5)
        {
            if (items is null)
                return default;

            T? bestItem = default;
            double bestScore = 0;

            foreach (var item in items)
            {
                double score = CalculateSimilarity(selector(item).ToLower(), query.ToLower());
                if (score > bestScore)
                {
                    bestScore = score;
                    bestItem = item;
                }
            }

            return bestScore >= threshold ? bestItem : default;
        }

        private static double CalculateSimilarity(string source, string target)
        {
            if (source == target) return 1.0;
            if (source.Contains(target) || target.Contains(source)) return 0.8;

            int stepsToSame = LevenshteinDistance(source, target);
            return 1.0 - ((double)stepsToSame / Math.Max(source.Length, target.Length));
        }

        private static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];
            if (n == 0) return m;
            if (m == 0) return n;
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
    }
}
