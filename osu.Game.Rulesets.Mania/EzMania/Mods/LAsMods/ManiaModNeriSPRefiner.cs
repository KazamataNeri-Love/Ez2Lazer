// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    /// <summary>
    /// Neri SP Refiner — SpaceMix 14K 键型顺手器。
    ///
    /// 三层处理：
    ///   Layer 1 [ERROR]: 三方冲突消解 + Hold 释放排斥区（1/4拍内禁止另两区同时有note）。
    ///   Layer 2 [HOLD]:   Hold 冲突检测。
    ///   Layer 3 [TRANSITION]: 切换缓冲。
    ///
    /// 5 指对 5 键，区域内无需排列字典。
    /// </summary>
    public class ManiaModNeriSPRefiner : Mod, IApplicableAfterBeatmapConversion, IEzApplyOrder, IHasSeed
    {
        public override string Name => "Neri SP Refiner";
        public override string Acronym => "NSPR";
        public override double ScoreMultiplier => 1;
        public override LocalisableString Description => NeriSPRefinerStrings.DESCRIPTION;
        public override IconUsage? Icon => FontAwesome.Solid.Stream;
        public override ModType Type => ModType.NeriMod;
        public override bool Ranked => false;
        public override bool ValidForMultiplayer => true;
        public override bool ValidForFreestyleAsRequiredMod => false;

        // ── 参数 ──

        [SettingSource(typeof(NeriSPRefinerStrings), nameof(NeriSPRefinerStrings.TRANSITION_WINDOW_LABEL), nameof(NeriSPRefinerStrings.TRANSITION_WINDOW_DESCRIPTION))]
        public BindableNumber<int> TransitionWindowBeats { get; } = new BindableInt(1)
        {
            MinValue = 1,
            MaxValue = 4,
            Precision = 1
        };

        [SettingSource(typeof(NeriSPRefinerStrings), nameof(NeriSPRefinerStrings.MAX_DELETE_LABEL), nameof(NeriSPRefinerStrings.MAX_DELETE_DESCRIPTION))]
        public BindableNumber<int> MaxDeletePerChord { get; } = new BindableInt(2)
        {
            MinValue = 1,
            MaxValue = 5,
            Precision = 1
        };

        [SettingSource(typeof(EzCommonModStrings), nameof(EzCommonModStrings.SEED_LABEL), nameof(EzCommonModStrings.SEED_DESCRIPTION), SettingControlType = typeof(SettingsNumberBox))]
        public Bindable<int?> Seed { get; } = new Bindable<int?>();

        [SettingSource(typeof(EzCommonModStrings), nameof(EzCommonModStrings.APPLY_ORDER_LABEL), nameof(EzCommonModStrings.APPLY_ORDER_DESCRIPTION))]
        public BindableNumber<int> ApplyOrderIndex { get; } = new BindableInt(550)
        {
            MinValue = 0,
            MaxValue = 1000
        };

        public int ApplyOrder => ApplyOrderIndex.Value;

        public override IEnumerable<(LocalisableString setting, LocalisableString value)> SettingDescription
        {
            get
            {
                yield return (NeriSPRefinerStrings.SETTING_TRANSITION_LABEL, $"{TransitionWindowBeats.Value} beat(s)");
                yield return (NeriSPRefinerStrings.SETTING_MAX_DELETE_LABEL, $"{MaxDeletePerChord.Value}");
                yield return (EzCommonModStrings.SEED_LABEL, Seed.Value?.ToString() ?? "Random");
                yield return (EzCommonModStrings.APPLY_ORDER_LABEL, $"{ApplyOrderIndex.Value}");
            }
        }

        private const int TOTAL_COLUMNS = 14;

        // 区域定义
        private const int DP_LEFT_START = 0;
        private const int DP_LEFT_END = 4;
        private const int EFFECT_START = 5;
        private const int EFFECT_END = 8;
        private const int DP_RIGHT_START = 9;
        private const int DP_RIGHT_END = 13;

        private const int ZONE_DP_LEFT = 0;
        private const int ZONE_EFFECT = 1;
        private const int ZONE_DP_RIGHT = 2;

        public void ApplyToBeatmap(IBeatmap beatmap)
        {
            var maniaBeatmap = (ManiaBeatmap)beatmap;

            if ((int)maniaBeatmap.Difficulty.CircleSize != TOTAL_COLUMNS)
                return;

            Seed.Value ??= RNG.Next();
            int seed = Seed.Value.Value;
            var rng = new Random(seed);

            var hitObjects = maniaBeatmap.HitObjects;
            if (hitObjects.Count == 0)
                return;

            var controlPoints = beatmap.ControlPointInfo;
            double refBeatLength = controlPoints.TimingPointAt(hitObjects[0].StartTime).BeatLength;

            // 按 StartTime 分组为和弦
            var chords = hitObjects
                .GroupBy(h => h.StartTime)
                .OrderBy(g => g.Key)
                .ToList();

            // ── 追踪状态 ──

            // 左手 / 右手上一次活动
            double leftLastTime = -9999;
            int leftMinCol = 5, leftMaxCol = -1;
            double rightLastTime = -9999;
            int rightMinCol = 14, rightMaxCol = -1;

            double transitionWindowMs = refBeatLength * TransitionWindowBeats.Value;
            double releaseBufferMs = refBeatLength / 2.0; // Hold 释放后 1/2 拍缓冲
            double exclusionWindowMs = refBeatLength / 4.0; // Hold 释放后 1/4 拍排斥区

            // 活跃 Hold 列表
            var activeHolds = new List<ActiveHoldInfo>();
            // 释放缓冲列表
            var releaseBuffer = new List<ReleaseBufferInfo>();
            // Hold 释放排斥区列表
            var exclusionZones = new List<ExclusionZoneInfo>();

            var notesToRemove = new List<ManiaHitObject>();

            foreach (var chord in chords)
            {
                double currentTime = chord.Key;
                var notes = chord.ToList();
                int deleted = 0;

                // ── 维护释放缓冲：清理过期 ──
                releaseBuffer.RemoveAll(r => r.BufferEndTime < currentTime);

                // ── 维护排斥区：清理过期 ──
                exclusionZones.RemoveAll(e => e.ExpireTime < currentTime);

                // ── Hold 结束时登记排斥区 ──
                //   刚结束的 hold 所属区域 → 1/4 拍内禁止另两区同时有 note
                for (int i = activeHolds.Count - 1; i >= 0; i--)
                {
                    var h = activeHolds[i];
                    if (h.EndTime < currentTime)
                    {
                        // 记录排斥区：hold 的 zone + 1/4 拍的排斥窗口
                        int holdZone = getZone(h.Column);
                        exclusionZones.Add(new ExclusionZoneInfo
                        {
                            HoldZone = holdZone,
                            ExpireTime = h.EndTime + exclusionWindowMs,
                            HoldColumn = h.Column
                        });

                        releaseBuffer.Add(new ReleaseBufferInfo
                        {
                            Column = h.Column,
                            ReleaseTime = h.EndTime,
                            BufferEndTime = h.EndTime + releaseBufferMs,
                            IsLeftHand = h.IsLeftHand
                        });
                        activeHolds.RemoveAt(i);
                    }
                }

                // ── 识别新 Hold（当前和弦中才开始的 HoldNote）──
                foreach (var note in notes)
                {
                    if (note is HoldNote hold && hold.EndTime > currentTime + 1)
                    {
                        bool isLeft = assignHoldHand(hold.Column, activeHolds, rng);
                        activeHolds.Add(new ActiveHoldInfo
                        {
                            Note = hold,
                            Column = hold.Column,
                            EndTime = hold.EndTime,
                            IsLeftHand = isLeft
                        });
                    }
                }

                // ── Layer 1: Hold 释放排斥区 + 三方冲突检测 ──

                // 1a. 排斥区检查：若有 Hold 刚在某区结束（1/4拍内），禁止另两区同时有 note
                foreach (var excl in exclusionZones)
                {
                    if (currentTime > excl.ExpireTime) continue;

                    bool hasD_ex = notes.Any(n => n.Column >= DP_LEFT_START && n.Column <= DP_LEFT_END && n.Column != -1);
                    bool hasE_ex = notes.Any(n => n.Column >= EFFECT_START && n.Column <= EFFECT_END && n.Column != -1);
                    bool hasR_ex = notes.Any(n => n.Column >= DP_RIGHT_START && n.Column <= DP_RIGHT_END && n.Column != -1);

                    int other1Has = 0, other2Has = 0;
                    if (excl.HoldZone == ZONE_DP_LEFT) { if (hasE_ex) other1Has++; if (hasR_ex) other2Has++; }
                    else if (excl.HoldZone == ZONE_EFFECT) { if (hasD_ex) other1Has++; if (hasR_ex) other2Has++; }
                    else if (excl.HoldZone == ZONE_DP_RIGHT) { if (hasD_ex) other1Has++; if (hasE_ex) other2Has++; }

                    if (other1Has > 0 && other2Has > 0)
                    {
                        // 排斥区违规：另两区同时有 note
                        // 删除非 hold 区的 note（优先删 note 数少的区）
                        int dCount = hasD_ex ? notes.Count(n => n.Column >= DP_LEFT_START && n.Column <= DP_LEFT_END && n.Column != -1) : 0;
                        int eCount = hasE_ex ? notes.Count(n => n.Column >= EFFECT_START && n.Column <= EFFECT_END && n.Column != -1) : 0;
                        int rCount = hasR_ex ? notes.Count(n => n.Column >= DP_RIGHT_START && n.Column <= DP_RIGHT_END && n.Column != -1) : 0;

                        // 确定要删除的区域（非 hold 区中 note 数少的）
                        int targetStart, targetEnd;
                        if (excl.HoldZone == ZONE_DP_LEFT)
                        {
                            // 删 E 或 R 中较少的一方
                            if (eCount <= rCount && eCount > 0) { targetStart = EFFECT_START; targetEnd = EFFECT_END; }
                            else { targetStart = DP_RIGHT_START; targetEnd = DP_RIGHT_END; }
                        }
                        else if (excl.HoldZone == ZONE_EFFECT)
                        {
                            if (dCount <= rCount && dCount > 0) { targetStart = DP_LEFT_START; targetEnd = DP_LEFT_END; }
                            else { targetStart = DP_RIGHT_START; targetEnd = DP_RIGHT_END; }
                        }
                        else // ZONE_DP_RIGHT
                        {
                            if (dCount <= eCount && dCount > 0) { targetStart = DP_LEFT_START; targetEnd = DP_LEFT_END; }
                            else { targetStart = EFFECT_START; targetEnd = EFFECT_END; }
                        }

                        foreach (var n in notes.Where(n => n.Column >= targetStart && n.Column <= targetEnd && n.Column != -1).ToList())
                        {
                            if (deleted >= MaxDeletePerChord.Value) break;
                            if (n is not HoldNote)
                            {
                                n.Column = -1;
                                notesToRemove.Add(n);
                                deleted++;
                            }
                        }
                    }
                }

                // 1b. 三方冲突检测
                bool hasD = notes.Any(n => n.Column >= DP_LEFT_START && n.Column <= DP_LEFT_END && n.Column != -1);
                bool hasE = notes.Any(n => n.Column >= EFFECT_START && n.Column <= EFFECT_END && n.Column != -1);
                bool hasR = notes.Any(n => n.Column >= DP_RIGHT_START && n.Column <= DP_RIGHT_END && n.Column != -1);

                if (hasD && hasE && hasR)
                {
                    var effectNotes = notes.Where(n => n.Column >= EFFECT_START && n.Column <= EFFECT_END && n.Column != -1).ToList();

                    // 优先删非 hold
                    foreach (var en in effectNotes)
                    {
                        if (deleted >= MaxDeletePerChord.Value) break;
                        if (en is not HoldNote)
                        {
                            en.Column = -1;
                            notesToRemove.Add(en);
                            deleted++;
                        }
                    }

                    // 不够再删 hold
                    if (deleted < MaxDeletePerChord.Value)
                    {
                        foreach (var en in effectNotes)
                        {
                            if (deleted >= MaxDeletePerChord.Value) break;
                            if (en.Column == -1) continue;
                            en.Column = -1;
                            notesToRemove.Add(en);
                            deleted++;
                            // 同步移除活跃 Hold
                            activeHolds.RemoveAll(h => h.Note == en);
                        }
                    }

                    hasE = notes.Any(n => n.Column >= EFFECT_START && n.Column <= EFFECT_END && n.Column != -1);
                }

                // ── Layer 2: Hold 冲突（活跃 Hold 与当前 note 共存）──
                foreach (var hold in activeHolds)
                {
                    foreach (var note in notes)
                    {
                        if (note.Column == -1) continue;
                        if (note == hold.Note) continue; // 就是它自己

                        int span = Math.Abs(note.Column - hold.Column);

                        // 同手 span > 3 → 卡手
                        if (isSameHand(note.Column, hold.Column, hold.IsLeftHand) && span > 3)
                        {
                            if (deleted < MaxDeletePerChord.Value)
                            {
                                // 优先删 note（不动 hold）
                                if (note is not HoldNote)
                                {
                                    note.Column = -1;
                                    notesToRemove.Add(note);
                                    deleted++;
                                }
                            }
                        }
                    }
                }

                // ── Layer 3: 切换缓冲（含释放缓冲跨区检测）──

                // 过滤有效 note
                var activeD = notes.Where(n => n.Column >= DP_LEFT_START && n.Column <= DP_LEFT_END && n.Column != -1).ToList();
                var activeE = notes.Where(n => n.Column >= EFFECT_START && n.Column <= EFFECT_END && n.Column != -1).ToList();
                var activeR = notes.Where(n => n.Column >= DP_RIGHT_START && n.Column <= DP_RIGHT_END && n.Column != -1).ToList();

                if (activeD.Count == 0 && activeE.Count == 0 && activeR.Count == 0)
                    continue;

                // 手部分配
                var leftNotes = new List<ManiaHitObject>();
                var rightNotes = new List<ManiaHitObject>();
                leftNotes.AddRange(activeD);
                rightNotes.AddRange(activeR);

                foreach (var en in activeE)
                {
                    if (en.Column <= 6) leftNotes.Add(en);
                    else rightNotes.Add(en);
                }

                // 检查左手切换急促程度
                if (leftNotes.Count > 0 && leftLastTime > -9999)
                {
                    double gap = currentTime - leftLastTime;
                    int curLeftMin = leftNotes.Min(n => n.Column);
                    int curLeftMax = leftNotes.Max(n => n.Column);

                    bool wasInDP = leftMaxCol <= DP_LEFT_END;
                    bool wasInEffect = leftMinCol >= EFFECT_START && leftMaxCol <= EFFECT_END;
                    bool nowInDP = curLeftMax <= DP_LEFT_END;
                    bool nowInEffect = curLeftMin >= EFFECT_START && curLeftMax <= EFFECT_END;
                    bool leftTransitioning = (wasInDP && nowInEffect) || (wasInEffect && nowInDP);

                    // 额外：释放缓冲跨区检测
                    bool leftInReleaseBuffer = releaseBuffer.Any(r => r.IsLeftHand && r.BufferEndTime >= currentTime);
                    bool leftCrossZoneFromRelease = leftInReleaseBuffer && nowInEffect != wasInEffect;

                    if ((leftTransitioning || leftCrossZoneFromRelease) && gap < transitionWindowMs)
                    {
                        foreach (var en in activeE.Where(n => n.Column <= 6 && n.Column != -1).ToList())
                        {
                            if (rightNotes.Count == 0 || (rightNotes.Count > 0 && (en.Column - rightNotes.Min(n2 => n2.Column)) >= -2))
                            {
                                leftNotes.Remove(en);
                                rightNotes.Add(en);
                            }
                            else if (deleted < MaxDeletePerChord.Value)
                            {
                                en.Column = -1;
                                notesToRemove.Add(en);
                                deleted++;
                            }
                        }
                    }
                }

                // 检查右手切换急促程度
                if (rightNotes.Count > 0 && rightLastTime > -9999)
                {
                    double gap = currentTime - rightLastTime;
                    int curRightMin = rightNotes.Min(n => n.Column);
                    int curRightMax = rightNotes.Max(n => n.Column);

                    bool wasInDP_R = rightMinCol >= DP_RIGHT_START;
                    bool wasInEffect_R = rightMinCol >= EFFECT_START && rightMaxCol <= EFFECT_END;
                    bool nowInDP_R = curRightMin >= DP_RIGHT_START;
                    bool nowInEffect_R = curRightMin >= EFFECT_START && curRightMax <= EFFECT_END;
                    bool rightTransitioning = (wasInDP_R && nowInEffect_R) || (wasInEffect_R && nowInDP_R);

                    bool rightInReleaseBuffer = releaseBuffer.Any(r => !r.IsLeftHand && r.BufferEndTime >= currentTime);
                    bool rightCrossZoneFromRelease = rightInReleaseBuffer && nowInEffect_R != wasInEffect_R;

                    if ((rightTransitioning || rightCrossZoneFromRelease) && gap < transitionWindowMs)
                    {
                        foreach (var en in activeE.Where(n => n.Column >= 7 && n.Column != -1).ToList())
                        {
                            if (leftNotes.Count == 0 || (leftNotes.Count > 0 && (leftNotes.Max(n2 => n2.Column) - en.Column) >= -2))
                            {
                                rightNotes.Remove(en);
                                leftNotes.Add(en);
                            }
                            else if (deleted < MaxDeletePerChord.Value)
                            {
                                en.Column = -1;
                                notesToRemove.Add(en);
                                deleted++;
                            }
                        }
                    }
                }

                // 更新追踪
                if (leftNotes.Count > 0)
                {
                    leftLastTime = currentTime;
                    leftMinCol = leftNotes.Min(n => n.Column);
                    leftMaxCol = leftNotes.Max(n => n.Column);
                }
                if (rightNotes.Count > 0)
                {
                    rightLastTime = currentTime;
                    rightMinCol = rightNotes.Min(n => n.Column);
                    rightMaxCol = rightNotes.Max(n => n.Column);
                }
            }

            // 清理
            foreach (var note in notesToRemove)
            {
                if (note.Column == -1)
                    hitObjects.Remove(note);
            }
        }

        // ── 辅助方法 ──

        /// <summary>判定 Hold 应由哪只手负责</summary>
        private static bool assignHoldHand(int col, List<ActiveHoldInfo> activeHolds, Random rng)
        {
            if (col <= DP_LEFT_END) return true; // DP-Left
            if (col >= DP_RIGHT_START) return false; // DP-Right

            // Effect 区：偏向空闲手
            int leftHolds = activeHolds.Count(h => h.IsLeftHand);
            int rightHolds = activeHolds.Count(h => !h.IsLeftHand);

            if (col <= 6)
                return leftHolds <= rightHolds || rng.Next(2) == 0; // E5-6 偏左
            else
                return rightHolds < leftHolds || rng.Next(2) == 1; // E7-8 偏右
        }

        /// <summary>判定两个列是否由同一只手负责</summary>
        private static bool isSameHand(int col1, int col2, bool holdIsLeft)
        {
            // 确定 col1 的手
            bool col1IsLeft = col1 <= DP_LEFT_END || (col1 <= 6 && holdIsLeft);

            // 确定 col2 的手
            bool col2IsLeft;
            if (col2 <= DP_LEFT_END) col2IsLeft = true;
            else if (col2 >= DP_RIGHT_START) col2IsLeft = false;
            else col2IsLeft = col2 <= 6; // Effect 默认左偏

            return col1IsLeft == col2IsLeft;
        }

        /// <summary>判定列所属区域</summary>
        private static int getZone(int col)
        {
            if (col <= DP_LEFT_END) return ZONE_DP_LEFT;
            if (col >= DP_RIGHT_START) return ZONE_DP_RIGHT;
            return ZONE_EFFECT;
        }

        // ── 内部结构 ──

        private class ActiveHoldInfo
        {
            public ManiaHitObject Note = null!;
            public int Column;
            public double EndTime;
            public bool IsLeftHand;
        }

        private class ReleaseBufferInfo
        {
            public int Column;
            public double ReleaseTime;
            public double BufferEndTime;
            public bool IsLeftHand;
        }

        private class ExclusionZoneInfo
        {
            /// <summary>Hold 所在区域（ZONE_DP_LEFT / ZONE_EFFECT / ZONE_DP_RIGHT）</summary>
            public int HoldZone;
            /// <summary>排斥区过期时间</summary>
            public double ExpireTime;
            /// <summary>Hold 的列</summary>
            public int HoldColumn;
        }
    }
}
