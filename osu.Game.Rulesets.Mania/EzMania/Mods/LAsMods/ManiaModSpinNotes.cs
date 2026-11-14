// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Objects.Drawables;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.UI.Scrolling;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Rulesets.Mania.EzMania.Mods.LAsMods
{
    /// <summary>
    /// Spin Notes — 音符在下落全过程绕自身中心自转；长条按 Hold Off 的逻辑全部拆成普通音符。
    /// </summary>
    /// <remarks>
    /// 轴心：note 的 Origin 在底边/顶边中点，不能直接转本体（会绕边公转）。
    /// 这里转 note 内部的 SkinnableDrawable：把它的 Origin 改到中心并用 Position 补偿，
    /// 因此旋转轴心落在音符中心，且不依赖滚动容器的定位时序。
    /// </remarks>
    public class ManiaModSpinNotes : Mod, IApplicableAfterBeatmapConversion, IEzApplyOrder,
        IApplicableToDrawableRuleset<ManiaHitObject>, IUpdatableByPlayfield
    {
        public override string Name => "Spin Notes";
        public override string Acronym => "SN";
        public override LocalisableString Description => SpinNotesStrings.DESCRIPTION;
        public override ModType Type => ModType.NeriMod;
        public override bool Ranked => false;
        public override bool ValidForMultiplayer => true;
        public override bool ValidForFreestyleAsRequiredMod => false;

        /// <summary>长条拆除必须排在生成 LN 的转换 Mod 之后。</summary>
        public int ApplyOrder => 100;

        [SettingSource(typeof(SpinNotesStrings), nameof(SpinNotesStrings.DIRECTION_LABEL), nameof(SpinNotesStrings.DIRECTION_DESCRIPTION), 0)]
        public Bindable<RotationDirection> Direction { get; } = new Bindable<RotationDirection>(RotationDirection.Counterclockwise);

        [SettingSource(typeof(SpinNotesStrings), nameof(SpinNotesStrings.TURNS_LABEL), nameof(SpinNotesStrings.TURNS_DESCRIPTION), 1)]
        public BindableInt Turns { get; } = new BindableInt(1)
        {
            MinValue = 1,
            MaxValue = 10,
            Precision = 1,
        };

        private IBindable<double>? timeRange;

        private static readonly ConditionalWeakTable<DrawableHitObject, SkinnableDrawable[]> piece_cache = new();

        public void ApplyToBeatmap(IBeatmap beatmap) => new ManiaModHoldOff().ApplyToBeatmap(beatmap);

        public void ApplyToDrawableRuleset(DrawableRuleset<ManiaHitObject> drawableRuleset)
        {
            if (drawableRuleset is DrawableScrollingRuleset<ManiaHitObject> scrolling)
                timeRange = scrolling.ScrollingInfo.TimeRange;
        }

        public void Update(Playfield playfield)
        {
            if (timeRange == null || playfield is not ManiaPlayfield maniaPlayfield)
                return;

            double range = timeRange.Value;

            if (range <= 0)
                return;

            double now = playfield.Time.Current;
            float direction = Direction.Value == RotationDirection.Clockwise ? 1 : -1;
            float total = 360f * Turns.Value;

            foreach (var stage in maniaPlayfield.Stages)
            {
                foreach (var column in stage.Columns)
                {
                    foreach (var entry in column.HitObjectContainer.AliveEntries)
                    {
                        var drawable = entry.Value;

                        if (drawable.Judged || drawable is not DrawableNote)
                            continue;

                        double progress = (now - (drawable.HitObject.StartTime - range)) / range;

                        if (progress < 0 || progress > 1)
                            continue;

                        float angle = direction * total * (float)progress;

                        foreach (var piece in piecesFor(drawable))
                            applyPiece(piece, angle);
                    }
                }
            }
        }

        private static SkinnableDrawable[] piecesFor(DrawableHitObject drawable)
            => piece_cache.GetValue(drawable, d => d.ChildrenOfType<SkinnableDrawable>().Where(p => isOutermost(p, d)).ToArray());

        private static bool isOutermost(Drawable piece, Drawable owner)
        {
            for (var p = piece.Parent; p != null && p != owner; p = p.Parent)
            {
                if (p is SkinnableDrawable)
                    return false;
            }

            return true;
        }

        private static void applyPiece(SkinnableDrawable piece, float angle)
        {
            if (piece.Origin != Anchor.Centre)
                piece.Origin = Anchor.Centre;

            // 原 Origin 在顶/底边中点，改成中心后要按半高补偿，才能保持音符位置不变。
            var target = new Vector2(0, (piece.Anchor == Anchor.TopCentre ? 1 : -1) * piece.DrawHeight / 2);

            if (piece.Position != target)
                piece.Position = target;

            piece.Rotation = angle;
        }
    }

    public static class SpinNotesStrings
    {
        public static readonly LocalisableString DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "音符在下落全过程绕自身中心自转；长条会被拆成普通音符。",
            "Notes spin around their centre while falling; hold notes are flattened into notes.");

        public static readonly LocalisableString DIRECTION_LABEL = new EzLocalizationManager.EzLocalisableString(
            "旋转方向", "Direction");

        public static readonly LocalisableString DIRECTION_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "顺时针或逆时针自转。", "Clockwise or counter-clockwise.");

        public static readonly LocalisableString TURNS_LABEL = new EzLocalizationManager.EzLocalisableString(
            "旋转圈数", "Turns");

        public static readonly LocalisableString TURNS_DESCRIPTION = new EzLocalizationManager.EzLocalisableString(
            "整个下落过程转多少圈。", "How many full turns over the whole fall.");
    }
}
