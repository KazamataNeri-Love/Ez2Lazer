// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Scoring;
using osu.Game.Skinning;

namespace osu.Game.Screens.Play.HUD
{
    /// <summary>
    /// 彩率计算器 — 计算 Perfect / Great (或 Good) 比值，显示两位小数。
    /// 如果当前规则集不存在 Great 判定（如 O2Jam），自动切换为 Perfect / Good。
    /// 使用与 DefaultAccuracyCounter / DefaultScoreCounter 相同的 OsuSpriteText 字体。
    /// </summary>
    public partial class Default300GRateCounter : CompositeDrawable, ISerialisableDrawable
    {
        public bool UsesFixedAnchor { get; set; }

        private OsuSpriteText rateText = null!;

        [Resolved]
        private ScoreProcessor scoreProcessor { get; set; } = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        private bool hasGreatJudgement;

        public Default300GRateCounter()
        {
            Anchor = Anchor.TopCentre;
            Origin = Anchor.TopCentre;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            Colour = colours.BlueLighter;
            AutoSizeAxes = Axes.Both;

            // 通过 ruleset 获取有效判定结果，判断 Great 是否可用
            var hitResults = ruleset.Value.CreateInstance().GetHitResultsForDisplay();
            hasGreatJudgement = hitResults.Any(r => r.result == HitResult.Great);

            InternalChild = rateText = new OsuSpriteText
            {
                Font = OsuFont.Default.With(size: 20f, fixedWidth: true),
            };

            updateDisplay();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            scoreProcessor.NewJudgement += _ => updateDisplay();
            scoreProcessor.JudgementReverted += _ => updateDisplay();
        }

        private void updateDisplay()
        {
            var statistics = scoreProcessor.Statistics;

            int perfectCount = statistics.GetValueOrDefault(HitResult.Perfect);
            int secondaryCount;

            if (hasGreatJudgement)
                secondaryCount = statistics.GetValueOrDefault(HitResult.Great);
            else
                secondaryCount = statistics.GetValueOrDefault(HitResult.Good);

            // 计算 Perfect / Secondary 比值，显示两位小数
            double rate = secondaryCount > 0 ? (double)perfectCount / secondaryCount : 0;
            rateText.Text = rate.ToString("F2");
        }
    }
}
