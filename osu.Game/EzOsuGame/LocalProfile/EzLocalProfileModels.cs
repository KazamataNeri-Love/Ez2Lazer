// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Scoring;

namespace osu.Game.EzOsuGame.LocalProfile
{
    public static class EzLocalProfileConstants
    {
        public const string UNKNOWN_USERNAME = "(unknown)";

        /// <summary>
        /// Sentinel value for the player filter dropdown: show merged archive totals.
        /// </summary>
        public const string ALL_PLAYERS = "All";

        public const int OSU_RULESET_ID = 0;
        public const int MANIA_RULESET_ID = 3;

        /// <summary>Key 成绩展示白名单：仅列出这些键数档。</summary>
        public static readonly int[] MANIA_DISPLAYED_KEY_COUNTS = { 4, 5, 6, 7, 8, 9, 10, 12, 14, 16, 18 };
    }

    public readonly record struct EzLocalProfileUsernameCount(string Username, int ScoreCount);

    /// <summary>
    /// Progress payload for local profile compute (scores processed / total, then save).
    /// </summary>
    public readonly record struct EzLocalProfileComputeProgress(int Processed, int Total, bool Saving);

    public sealed class EzLocalProfileSnapshot
    {
        public bool HasData { get; init; }

        /// <summary>
        /// True when stored stats were produced by an older analysis logic version and should be recomputed.
        /// </summary>
        public bool NeedsRecompute { get; init; }

        public DateTimeOffset? LastComputedAt { get; init; }
        public IReadOnlyList<string> IncludedUsernames { get; init; } = Array.Empty<string>();
        public IReadOnlyList<EzLocalProfileRulesetStats> RulesetStats { get; init; } = Array.Empty<EzLocalProfileRulesetStats>();
        public IReadOnlyList<EzLocalProfileManiaKeyStats> ManiaKeyStats { get; init; } = Array.Empty<EzLocalProfileManiaKeyStats>();
        public IReadOnlyList<EzLocalProfileManiaColumnStats> ManiaColumnStats { get; init; } = Array.Empty<EzLocalProfileManiaColumnStats>();
        public IReadOnlyList<EzLocalProfileGradeCount> GradeCounts { get; init; } = Array.Empty<EzLocalProfileGradeCount>();
        public IReadOnlyList<EzLocalProfileStarPlayCount> StarPlayCounts { get; init; } = Array.Empty<EzLocalProfileStarPlayCount>();
        public IReadOnlyList<EzLocalProfileXxyPlayCount> XxyPlayCounts { get; init; } = Array.Empty<EzLocalProfileXxyPlayCount>();
        public IReadOnlyList<EzLocalProfileStdAttrAffinity> StdAttrAffinities { get; init; } = Array.Empty<EzLocalProfileStdAttrAffinity>();
        public IReadOnlyList<EzLocalProfileDrillScoreRow> DrillScores { get; init; } = Array.Empty<EzLocalProfileDrillScoreRow>();
        public IReadOnlyList<EzLocalProfileTopPpScore> TopPpScores { get; init; } = Array.Empty<EzLocalProfileTopPpScore>();
        public IReadOnlyList<EzLocalProfileTopPlayedBeatmap> TopPlayedBeatmaps { get; init; } = Array.Empty<EzLocalProfileTopPlayedBeatmap>();
    }

    public readonly record struct EzLocalProfileRulesetStats(
        int RulesetId,
        long TotalKeys,
        double AvgKps,
        double MaxKps,
        int ScoreCount,
        int KpsSampleCount,
        double TotalPp,
        long TotalDurationMs);

    public readonly record struct EzLocalProfileManiaKeyStats(
        int KeyCount,
        long TotalKeys,
        double AvgKps,
        double MaxKps,
        int ScoreCount,
        int KpsSampleCount,
        double TotalPp,
        long TotalDurationMs);

    public readonly record struct EzLocalProfileManiaColumnStats(
        int KeyCount,
        int ColumnIndex,
        long TotalKeys,
        double AvgKps,
        double MaxKps,
        int ScoreCount,
        int KpsSampleCount);

    public readonly record struct EzLocalProfileGradeCount(int RulesetId, ScoreRank Rank, int Count);

    public readonly record struct EzLocalProfileStarPlayCount(int RulesetId, int StarBucket, int Count);

    /// <summary>
    /// xxy SR play distribution; bucket semantics mirror <see cref="EzLocalProfileStarPlayCount"/>.
    /// </summary>
    public readonly record struct EzLocalProfileXxyPlayCount(int RulesetId, int StarBucket, int Count);

    public enum EzLocalProfileStdAttr
    {
        ApproachRate = 0,
        CircleSize = 1,
    }

    public readonly record struct EzLocalProfileStdAttrAffinity(
        EzLocalProfileStdAttr Attr,
        double Value,
        int PlayCount,
        int HighGradeCount);

    /// <summary>
    /// 最优 PP 榜单条目（按规则集）。
    /// </summary>
    public readonly record struct EzLocalProfileTopPpScore(int RulesetId, Guid BeatmapId, string BeatmapName, double Pp, ScoreRank Rank);

    /// <summary>
    /// 最多游玩榜单条目（按规则集）。
    /// </summary>
    public readonly record struct EzLocalProfileTopPlayedBeatmap(int RulesetId, Guid BeatmapId, string BeatmapName, int PlayCount);

    /// <summary>
    /// Online API score metadata persisted for profile stats when local .osr import is unavailable.
    /// </summary>
    public readonly record struct EzLocalProfileOnlineScoreContribution(
        long OnlineId,
        int RulesetId,
        ScoreRank Rank,
        double StarRating,
        float CircleSize,
        float ApproachRate,
        long KeyCount,
        double Pp,
        long DurationMs);

    /// <summary>
    /// In-memory aggregation buffer written atomically to SQLite.
    /// </summary>
    public sealed class EzLocalProfileAggregationResult
    {
        public IReadOnlyList<string> IncludedUsernames { get; set; } = Array.Empty<string>();
        public DateTimeOffset ComputedAt { get; set; } = DateTimeOffset.UtcNow;
        public Dictionary<int, MutableRulesetStats> RulesetStats { get; } = new Dictionary<int, MutableRulesetStats>();
        public Dictionary<int, MutableManiaKeyStats> ManiaKeyStats { get; } = new Dictionary<int, MutableManiaKeyStats>();
        public Dictionary<(int KeyCount, int Column), MutableManiaColumnStats> ManiaColumnStats { get; } = new Dictionary<(int KeyCount, int Column), MutableManiaColumnStats>();
        public Dictionary<(int RulesetId, ScoreRank Rank), int> GradeCounts { get; } = new Dictionary<(int RulesetId, ScoreRank Rank), int>();
        public Dictionary<(int RulesetId, int StarBucket), int> StarPlayCounts { get; } = new Dictionary<(int RulesetId, int StarBucket), int>();
        public Dictionary<(int RulesetId, int StarBucket), int> XxyPlayCounts { get; } = new Dictionary<(int RulesetId, int StarBucket), int>();
        public Dictionary<(EzLocalProfileStdAttr Attr, double Value), MutableStdAttr> StdAttrAffinities { get; } = new Dictionary<(EzLocalProfileStdAttr Attr, double Value), MutableStdAttr>();
        public List<EzLocalProfileDrillScoreRow> DrillScores { get; } = new List<EzLocalProfileDrillScoreRow>();

        /// <summary>
        /// 每规则集的每谱面聚合（谱面Key → 最佳PP/游玩次数），供最优PP与最多游玩榜单使用。
        /// </summary>
        public Dictionary<int, Dictionary<string, MutableBeatmapAggregate>> BeatmapAggregates { get; } = new Dictionary<int, Dictionary<string, MutableBeatmapAggregate>>();

        public sealed class MutableBeatmapAggregate
        {
            public Guid BeatmapId;
            public string BeatmapName = string.Empty;
            public double BestPp;
            public ScoreRank BestPpRank;
            public int PlayCount;
        }

        public sealed class MutableRulesetStats
        {
            public long TotalKeys;
            public double KpsSum;
            public int KpsSampleCount;
            public double MaxKps;
            public int ScoreCount;
            public double TotalPp;
            public long TotalDurationMs;
        }

        public sealed class MutableManiaKeyStats
        {
            public long TotalKeys;
            public double KpsSum;
            public int KpsSampleCount;
            public double MaxKps;
            public int ScoreCount;
            public double TotalPp;
            public long TotalDurationMs;
        }

        public sealed class MutableManiaColumnStats
        {
            public long TotalKeys;
            public double KpsSum;
            public int KpsSampleCount;
            public double MaxKps;
            public int ScoreCount;
        }

        public sealed class MutableStdAttr
        {
            public int PlayCount;
            public int HighGradeCount;
        }
    }
}
