// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Skinning;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Osu.Mods
{
    /// <summary>
    /// Neri Hidden — 仅隐藏缩圈（Approach Circle），绝不导致 Note 闪烁。
    ///
    /// ── 与 OsuModHidden 的区别 ──
    /// 原版 OsuModHidden 在 OnlyFadeApproachCircles = true 时使用
    ///   circle.BeginAbsoluteSequence(time).Hide()
    /// 即 FadeTo(0, 0) 零时长变换。但 ApproachCircle 自身的
    ///   FadeTo(0.9, fadeInDuration)
    /// 变换与其同时启动，零时长变换结束后默认变换接管，导致缩圈短暂显现 → 闪烁。
    ///
    /// 本 Mod 改用 ClearTransforms() + 直接 Alpha = 0，
    /// 彻底移除默认动画变换，从根本上杜绝闪烁。
    /// </summary>
    public class OsuModNeriHidden : ModWithVisibilityAdjustment, IApplicableToScoreProcessor
    {
        public override string Name => "Neri Hidden";

        public override string Acronym => "NH";

        public override IconUsage? Icon => OsuIcon.ModHidden;

        public override ModType Type => ModType.NeriMod;

        public override LocalisableString Description => @"仅隐藏缩圈，不会闪烁。";

        public override double ScoreMultiplier => 1;

        public override Type[] IncompatibleMods => new[]
        {
            typeof(IRequiresApproachCircles),
            typeof(OsuModSpinIn),
            typeof(OsuModDepth),
            typeof(OsuModFreezeFrame),
            typeof(OsuModHidden),
        };

        protected override bool IsFirstAdjustableObject(HitObject hitObject) => !(hitObject is Spinner || hitObject is SpinnerTick);

        public override void ApplyToBeatmap(IBeatmap beatmap)
        {
            base.ApplyToBeatmap(beatmap);

            // 缩短 FadeIn 时长（与原版 Hidden 一致）
            foreach (var obj in beatmap.HitObjects.OfType<OsuHitObject>())
                applyFadeInAdjustment(obj);

            static void applyFadeInAdjustment(OsuHitObject osuObject)
            {
                osuObject.TimeFadeIn = osuObject.TimePreempt * 0.4;
                foreach (var nested in osuObject.NestedHitObjects.OfType<OsuHitObject>())
                    applyFadeInAdjustment(nested);
            }
        }

        protected override void ApplyIncreasedVisibilityState(DrawableHitObject hitObject, ArmedState state)
        {
            applyHiddenState(hitObject, true);
        }

        protected override void ApplyNormalVisibilityState(DrawableHitObject hitObject, ArmedState state)
        {
            applyHiddenState(hitObject, false);
        }

        private void applyHiddenState(DrawableHitObject drawableObject, bool increaseVisibility)
        {
            if (!(drawableObject is DrawableOsuHitObject drawableOsuObject))
                return;

            var hitObject = drawableOsuObject.HitObject;

            // ── 永久隐藏 Approach Circle ──────────────────────
            // 使用 ClearTransforms + 直接 Alpha 赋值，
            // 避免原版 FadeTo(0,0) 零时长变换被默认 FadeTo(0.9) 覆盖导致的闪烁。
            switch (drawableObject)
            {
                case DrawableHitCircle circle:
                    circle.ApproachCircle.ClearTransforms();
                    circle.ApproachCircle.Alpha = 0;
                    break;

                case DrawableSpinner spinner:
                    spinner.Body.OnSkinChanged += () => hideSpinnerApproachCircle(spinner);
                    hideSpinnerApproachCircle(spinner);
                    break;
            }

            if (increaseVisibility)
                return;

            // ── 主物件淡出（本 Mod 不启用，保留接口对齐）──────
            // 当前仅隐藏缩圈，不移除物件主体。如需扩展可在此添加。
        }

        private static void hideSpinnerApproachCircle(DrawableSpinner spinner)
        {
            var approachCircle = (spinner.Body.Drawable as IHasApproachCircle)?.ApproachCircle;
            if (approachCircle == null)
                return;

            approachCircle.ClearTransforms();
            approachCircle.Alpha = 0;
        }

        public void ApplyToScoreProcessor(ScoreProcessor scoreProcessor)
        {
        }

        public ScoreRank AdjustRank(ScoreRank rank, double accuracy)
        {
            switch (rank)
            {
                case ScoreRank.X:
                    return ScoreRank.XH;

                case ScoreRank.S:
                    return ScoreRank.SH;

                default:
                    return rank;
            }
        }
    }
}
