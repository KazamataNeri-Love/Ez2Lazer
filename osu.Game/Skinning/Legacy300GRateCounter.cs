// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Skinning
{
    /// <summary>
    /// 彩率计算器 - 计算 Perfect / Great (或 Good) 的比值，显示为两位小数浮点值。
    /// 如果当前 HitMode 不存在 Great 判定（如 O2Jam），则自动切换为 Perfect / Good。
    /// 使用 Legacy 皮肤字体（与 LegacyAccuracyCounter 相同），不加百分号。
    /// </summary>
    public partial class Legacy300GRateCounter : CompositeDrawable, ISerialisableDrawable
    {
        public bool UsesFixedAnchor { get; set; }

        private LegacySpriteText rateText = null!;

        [Resolved]
        private ScoreProcessor scoreProcessor { get; set; } = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        private bool hasGreatJudgement;

        public Legacy300GRateCounter()
        {
            Anchor = Anchor.TopRight;
            Origin = Anchor.TopRight;

            Scale = new Vector2(0.6f * 0.96f);
            Margin = new MarginPadding { Vertical = 9, Horizontal = 17 };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

            // 通过 ruleset 获取当前规则集的有效判定结果，判断 Great 是否可用
            var hitResults = ruleset.Value.CreateInstance().GetHitResultsForDisplay();
            hasGreatJudgement = hitResults.Any(r => r.result == HitResult.Great);

            InternalChild = rateText = createSpriteText();

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

        private LegacySpriteText createSpriteText()
        {
            return new LegacySpriteText(LegacyFont.Score)
            {
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
                FixedWidth = true,
            };
        }
    }
}
