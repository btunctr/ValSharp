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
            if (items is null) return default;
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
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0.0;

            if (source.Contains(target) || target.Contains(source))
            {
                int shorter = Math.Min(source.Length, target.Length);
                int longer = Math.Max(source.Length, target.Length);
                return 0.65 + 0.35 * ((double)shorter / longer);
            }

            double wordScore = WordOverlapScore(source, target);

            int distance = LevenshteinDistance(source, target);
            double levenshteinScore = 1.0 - ((double)distance / Math.Max(source.Length, target.Length));

            return 0.4 * levenshteinScore + 0.6 * wordScore;
        }

        private static double WordOverlapScore(string source, string target)
        {
            var srcWords = source.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var tgtWords = target.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (srcWords.Length == 0 || tgtWords.Length == 0) return 0.0;

            int hits = 0;
            foreach (var tw in tgtWords)
            {
                if (srcWords.Any(sw => sw == tw))
                {
                    hits += 2;
                    continue;
                }
                if (srcWords.Any(sw => WordSimilar(sw, tw)))
                    hits++;
            }

            int maxWords = Math.Max(srcWords.Length, tgtWords.Length);
            return Math.Min(1.0, (double)hits / (maxWords * 2));
        }

        private static bool WordSimilar(string a, string b)
        {
            if (a == b) return true;
            int maxLen = Math.Max(a.Length, b.Length);
            if (maxLen == 0) return true;
            int dist = LevenshteinDistance(a, b);
            return (double)dist / maxLen <= 0.35;
        }

        private static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length, m = t.Length;
            if (n == 0) return m;
            if (m == 0) return n;
            int[,] d = new int[n + 1, m + 1];
            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;
            for (int i = 1; i <= n; i++)
                for (int j = 1; j <= m; j++)
                {
                    int cost = t[j - 1] == s[i - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            return d[n, m];
        }
    }
}
