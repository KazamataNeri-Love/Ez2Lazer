// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.UI.Scrolling;
using osuTK;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    /// <summary>
    /// Ojama Earthquake — 整个游玩区域按固定折线路径平移：中心 → 右上 → 左上 → 右下 → 左下 → 右上 …
    /// 每一段都是直线运动，段与段首尾相接构成折线；幅度与滚动速度（Hi-Speed）成正比。
    /// </summary>
    /// <remarks>
    /// 参考 pop'n music「地震でぐらぐら」的公开描述（整体二维位移、幅度随 HS 增大、纯视觉干扰）建模，
    /// 不是官方算法。位移只由时间决定，回放可复现；只改渲染位移，不参与判定。
    /// </remarks>
    public class ManiaModOjamaEarthquake : Mod, IApplicableToDrawableRuleset<ManiaHitObject>, IUpdatableByPlayfield
    {
        public override string Name => "Ojama Earthquake";
        public override string Acronym => "OE";
        public override LocalisableString Description => OjamaEarthquakeStrings.DESCRIPTION;
        public override ModType Type => ModType.NeriMod;
        public override bool Ranked => false;
        public override bool ValidForMultiplayer => true;
        public override bool ValidForFreestyleAsRequiredMod => false;

        public override Type[] IncompatibleMods => new[]
        {
            typeof(ManiaModNeriBarrelRoll),
            typeof(ManiaModNeriDance),
            typeof(ManiaModTiltedTrack),
        };

        /// <summary>横向振幅（像素）。纵向固定为其 1/1.5。</summary>
        [SettingSource(typeof(OjamaEarthquakeStrings), nameof(OjamaEarthquakeStrings.AMPLITUDE_LABEL), nameof(OjamaEarthquakeStrings.AMPLITUDE_DESCRIPTION), 0)]
        public BindableFloat Amplitude { get; } = new BindableFloat(6)
        {
            MinValue = 0,
            MaxValue = 40,
            Precision = 0.5f,
        };

        /// <summary>折线每一段（两个顶点之间）的时长（毫秒）。</summary>
        [SettingSource(typeof(OjamaEarthquakeStrings), nameof(OjamaEarthquakeStrings.STEP_LABEL), nameof(OjamaEarthquakeStrings.STEP_DESCRIPTION), 1)]
        public BindableFloat StepDuration { get; } = new BindableFloat(300)
        {
            MinValue = 40,
            MaxValue = 1000,
            Precision = 10,
        };

        /// <summary>按谱面 BPM 决定每段时长（忽略 <see cref="StepDuration"/>）。</summary>
        [SettingSource(typeof(OjamaEarthquakeStrings), nameof(OjamaEarthquakeStrings.SYNC_LABEL), nameof(OjamaEarthquakeStrings.SYNC_DESCRIPTION), 2)]
        public BindableBool SyncToBPM { get; } = new BindableBool(false);

        /// <summary>跟随 BPM 时，每段折线占多少拍。</summary>
        [SettingSource(typeof(OjamaEarthquakeStrings), nameof(OjamaEarthquakeStrings.BEATS_LABEL), nameof(OjamaEarthquakeStrings.BEATS_DESCRIPTION), 3)]
        public BindableFloat BeatsPerStep { get; } = new BindableFloat(1)
        {
            MinValue = 0.125f,
            MaxValue = 4,
            Precision = 0.125f,
        };

        /// <summary>纵向振幅 / 横向振幅。</summary>
        private const float vertical_ratio = 1 / 1.5f;

        /// <summary>中心之后的四个角，按 右上 → 左上 → 右下 → 左下 循环（屏幕坐标，+Y 向下）。</summary>
        private static readonly Vector2[] corners =
        {
            new Vector2(1, -1), // 右上
            new Vector2(-1, -1), // 左上
            new Vector2(1, 1), // 右下
            new Vector2(-1, 1), // 左下
        };

        private IBindable<double>? timeRange;
        private double referenceTimeRange;
        private IBeatmap? beatmap;

        public void ApplyToDrawableRuleset(DrawableRuleset<ManiaHitObject> drawableRuleset)
        {
            beatmap = drawableRuleset.Beatmap;

            if (drawableRuleset is not DrawableScrollingRuleset<ManiaHitObject> scrolling)
                return;

            timeRange = scrolling.ScrollingInfo.TimeRange;
            referenceTimeRange = timeRange.Value;
        }

        public void Update(Playfield playfield)
        {
            float amplitude = Amplitude.Value;

            if (amplitude <= 0)
            {
                playfield.Position = Vector2.Zero;
                return;
            }

            double time = playfield.Time.Current;
            double stepSeconds;
            double elapsed;

            if (SyncToBPM.Value && beatmap != null)
            {
                // 每段长度由当前 BPM 决定；从该 timing point 的起点开始计相位，变速时不会跳相。
                var timingPoint = beatmap.ControlPointInfo.TimingPointAt(time);
                stepSeconds = Math.Max(0.001, BeatsPerStep.Value * timingPoint.BeatLength / 1000.0);
                elapsed = Math.Max(0, (time - timingPoint.Time) / 1000.0);
            }
            else
            {
                stepSeconds = Math.Max(0.001, StepDuration.Value / 1000.0);
                elapsed = Math.Max(0, time / 1000.0);
            }

            int index = (int)Math.Floor(elapsed / stepSeconds);
            float progress = (float)(elapsed / stepSeconds - index);

            Vector2 from = vertexAt(index);
            Vector2 to = vertexAt(index + 1);

            Vector2 offset = from + (to - from) * progress;

            double hsScale = timeRange != null && referenceTimeRange > 0 && timeRange.Value > 0
                ? referenceTimeRange / timeRange.Value
                : 1;

            float amplitudeX = (float)(amplitude * hsScale);
            float amplitudeY = amplitudeX * vertical_ratio;

            playfield.Position = new Vector2(offset.X * amplitudeX, offset.Y * amplitudeY);
        }

        private static Vector2 vertexAt(int index)
        {
            if (index <= 0)
                return Vector2.Zero;

            return corners[(index - 1) % corners.Length];
        }
    }

    public static class OjamaEarthquakeStrings
    {
        public static readonly LocalisableString DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "整个游玩区域沿折线持续平移，幅度随滚动速度增大。（模拟效果，非官方算法）",
            "The play area keeps shifting along a fixed path. Amplitude scales with scroll speed.");

        public static readonly LocalisableString AMPLITUDE_LABEL = new EzLocalizationManager.EzLocalisableString(
            "横向振幅", "Amplitude");

        public static readonly LocalisableString AMPLITUDE_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "横向偏移幅度（像素），纵向为其 1/1.5。", "Horizontal offset amplitude (pixels); vertical is 1/1.5 of it.");

        public static readonly LocalisableString STEP_LABEL = new EzLocalizationManager.EzLocalisableString(
            "步进时长", "Step duration");

        public static readonly LocalisableString STEP_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "折线每一段的时长（毫秒），越小摇得越快。", "Duration of each straight segment (ms); smaller shakes faster.");

        public static readonly LocalisableString SYNC_LABEL = new EzLocalizationManager.EzLocalisableString(
            "跟随 BPM", "Sync to BPM");

        public static readonly LocalisableString SYNC_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "按谱面 BPM 决定每段时长，忽略上面的步进时长。",
            "Derive each segment from the beatmap BPM, ignoring the step duration above.");

        public static readonly LocalisableString BEATS_LABEL = new EzLocalizationManager.EzLocalisableString(
            "每步拍数", "Beats per step");

        public static readonly LocalisableString BEATS_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "跟随 BPM 时，每段折线占多少拍。", "How many beats each segment takes when synced to BPM.");
    }
}
