// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.EzOsuGame;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.HUD;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Rulesets.Mania.EzMania.Helper;
using osu.Game.Rulesets.Mania.EzMania.Localization;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play.HUD;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Mania.EzMania.HUD
{
    /// <summary>
    /// 逐字符 Sprite 渲染的 Combo 计数器（DJMAX 风格）。
    /// 与 Plus 的区别：每次触发动画时缩放峰值和间距位移都是固定值，
    /// 不依赖当前缩放状态，因此连打时数字不会越弹越开。
    /// </summary>
    public partial class EzHUDComboCounterDJMAX : CompositeDrawable, ISerialisableDrawable
    {
        public bool UsesFixedAnchor { get; set; }

        [SettingSource(typeof(EzHUDManiaStrings), nameof(EzHUDManiaStrings.FONT_LABEL), nameof(EzHUDManiaStrings.FONT_DESCRIPTION), SettingControlType = typeof(EzSelectorEnumList))]
        public Bindable<EzEnumGameThemeName> ThemeName { get; } = new Bindable<EzEnumGameThemeName>(EzSelectorEnumList.DEFAULT_NAME);

        /// <summary>每次触发时的固定缩放峰值（不累积）。</summary>
        [SettingSource(typeof(EzHUDManiaStrings), nameof(EzHUDManiaStrings.EFFECT_START_FACTOR_LABEL), nameof(EzHUDManiaStrings.EFFECT_START_FACTOR_DESCRIPTION))]
        public BindableNumber<float> FixedScalePeak { get; } = new BindableNumber<float>(1.5f)
        {
            MinValue = 0.1f,
            MaxValue = 5f,
            Precision = 0.05f,
        };

        [SettingSource(typeof(EzHUDManiaStrings), nameof(EzHUDManiaStrings.EFFECT_START_DURATION_LABEL), nameof(EzHUDManiaStrings.EFFECT_START_DURATION_DESCRIPTION))]
        public BindableNumber<float> EffectStartTime { get; } = new BindableNumber<float>(10)
        {
            MinValue = 1,
            MaxValue = 300,
            Precision = 1f,
        };

        [SettingSource(typeof(EzHUDManiaStrings), nameof(EzHUDManiaStrings.EFFECT_END_DURATION_LABEL), nameof(EzHUDManiaStrings.EFFECT_END_DURATION_DESCRIPTION))]
        public BindableNumber<float> EffectEndDuration { get; } = new BindableNumber<float>(300)
        {
            MinValue = 10,
            MaxValue = 500,
            Precision = 10f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.ALPHA_LABEL), nameof(EzHUDStrings.ALPHA_DESCRIPTION))]
        public BindableNumber<float> AccentAlpha { get; } = new BindableNumber<float>(1)
        {
            MinValue = 0,
            MaxValue = 1,
            Precision = 0.01f,
        };

        /// <summary>
        /// 位移倍数。间距 = 字符宽度 × (固定缩放增量) × 倍数。
        /// 1.0 = 间距与缩放同步，0 = 无位移。
        /// </summary>
        [SettingSource(typeof(EzHUDManiaStrings), nameof(EzHUDManiaStrings.EFFECT_START_FACTOR_LABEL), nameof(EzHUDManiaStrings.EFFECT_START_FACTOR_DESCRIPTION))]
        public BindableNumber<float> DisplacementMultiplier { get; } = new BindableNumber<float>(1f)
        {
            MinValue = 0f,
            MaxValue = 5f,
            Precision = 0.1f,
        };

        /// <summary>每个字符的 Sprite 列表，供外部按索引操作。</summary>
        public IReadOnlyList<Sprite> CharacterSprites => charSprites;

        private readonly List<Sprite> charSprites = new List<Sprite>();

        private FillFlowContainer charFlow = null!;

        /// <summary>参考字符 '5' 的纹理宽度，用作 Spacing 位移单位。</summary>
        private float refCharWidth = 20f;

        private string currentText = string.Empty;
        private int previousCombo;

        [Resolved(canBeNull: true)]
        private EzResourceStore? resources { get; set; }

        [Resolved]
        private ScoreProcessor scoreProcessor { get; set; } = null!;

        public EzHUDComboCounterDJMAX()
        {
            AutoSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            charFlow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = Vector2.Zero,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            };

            InternalChild = charFlow;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            ThemeName.BindValueChanged(_ => updateDisplay(), true);
            AccentAlpha.BindValueChanged(alpha => charFlow.Alpha = alpha.NewValue, true);

            scoreProcessor.Combo.BindValueChanged(combo =>
            {
                bool wasIncrease = combo.NewValue > combo.OldValue;
                bool wasMiss = previousCombo > 1 && combo.NewValue == 0;

                updateDisplay();
                applyAnimation(wasIncrease, wasMiss);

                previousCombo = combo.NewValue;
            }, true);
        }

        private void applyAnimation(bool wasIncrease, bool wasMiss)
        {
            if (!wasIncrease) return;

            // 每次触发都用固定的缩放峰值，不依赖当前缩放状态
            float targetScale = Math.Clamp(FixedScalePeak.Value, 0.5f, 3f);

            // Spacing 位移峰值 = 字符宽度 × (固定缩放增量) × 位移倍数 — 固定值
            float peakSpacing = refCharWidth * (targetScale - 1f) * DisplacementMultiplier.Value;

            charFlow
                .ScaleTo(targetScale, EffectStartTime.Value, Easing.OutQuint)
                .Then()
                .ScaleTo(1f, EffectEndDuration.Value, Easing.OutQuint);

            charFlow
                .TransformTo(nameof(charFlow.Spacing), new Vector2(peakSpacing, 0), EffectStartTime.Value, Easing.OutQuint)
                .Then()
                .TransformTo(nameof(charFlow.Spacing), Vector2.Zero, EffectEndDuration.Value, Easing.OutQuint);

            if (wasMiss)
                charFlow.FlashColour(Color4.Red, EffectEndDuration.Value, Easing.OutQuint);
        }

        private void updateDisplay()
        {
            string newText = scoreProcessor.Combo.Value.ToString();
            if (newText == currentText) return;

            charFlow.ClearTransforms();
            charFlow.Spacing = Vector2.Zero;

            currentText = newText;
            rebuildSprites();
        }

        private void rebuildSprites()
        {
            charFlow.Clear();
            charSprites.Clear();

            string themeRoot = $"GameTheme/{ThemeName.Value.ToString().Replace(" ", "_")}/";

            // 获取参考字符 '5' 的纹理宽度，用作位移单位
            var refTexture = resources?.Get($"{themeRoot}combo/number/5");
            refCharWidth = refTexture?.Width ?? 20f;

            foreach (char c in currentText)
            {
                string path = $"{themeRoot}combo/number/{c}";
                var texture = resources?.Get(path);

                var sprite = new Sprite
                {
                    Texture = texture,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                };

                charSprites.Add(sprite);
                charFlow.Add(sprite);
            }
        }
    }
}
