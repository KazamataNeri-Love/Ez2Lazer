// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Animation;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.HUD;
using osu.Game.EzOsuGame.Localization;
using osu.Game.Localisation.SkinComponents;
using osu.Game.Rulesets.Mania.EzMania.Localization;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play.HUD;

namespace osu.Game.Rulesets.Mania.EzMania.HUD
{
    /// <summary>
    /// 逐字符渲染的连击计数器。峰值时整体缩放，并让数字沿对称斜线、按限定宽度展开，全部字符同一时间轴。
    /// </summary>
    public partial class NeriComboCounter : ComboCounter, IEzHudThemeTarget
    {
        EzHudThemeSlot IEzHudThemeTarget.Slot => EzHudThemeSlot.Combo;

        Bindable<string> IEzHudThemeTarget.Theme => ThemeName;

        [SettingSource(typeof(EzHUDManiaStrings), nameof(EzHUDManiaStrings.ANIMATION_TEMPLATE_LABEL), nameof(EzHUDManiaStrings.ANIMATION_TEMPLATE_DESCRIPTION))]
        public Bindable<EzEnumDeformTemplate> AnimationTemplate { get; } = new Bindable<EzEnumDeformTemplate>();

        [SettingSource(typeof(EzHUDManiaStrings), nameof(EzHUDManiaStrings.FONT_LABEL), nameof(EzHUDManiaStrings.FONT_DESCRIPTION), SettingControlType = typeof(EzSelectorEnumList))]
        public Bindable<string> ThemeName { get; } = new Bindable<string>(EzSelectorEnumList.DEFAULT_NAME);

        [SettingSource(typeof(EzHUDManiaStrings), nameof(EzHUDManiaStrings.PEAK_SCALE_LABEL), nameof(EzHUDManiaStrings.PEAK_SCALE_DESCRIPTION))]
        public BindableNumber<float> PeakScale { get; } = new BindableNumber<float>(1.2f)
        {
            MinValue = 1f,
            MaxValue = 3f,
            Precision = 0.05f,
        };

        [SettingSource(typeof(EzHUDManiaStrings), nameof(EzHUDManiaStrings.TILT_HEIGHT_LABEL), nameof(EzHUDManiaStrings.TILT_HEIGHT_DESCRIPTION))]
        public BindableNumber<float> TiltHeight { get; } = new BindableNumber<float>(0)
        {
            MinValue = 0f,
            MaxValue = 3f,
            Precision = 0.05f,
        };

        [SettingSource(typeof(EzHUDManiaStrings), nameof(EzHUDManiaStrings.EXPAND_WIDTH_LABEL), nameof(EzHUDManiaStrings.EXPAND_WIDTH_DESCRIPTION))]
        public BindableNumber<float> ExpandWidth { get; } = new BindableNumber<float>(0)
        {
            MinValue = 0f,
            MaxValue = 3f,
            Precision = 0.05f,
        };

        [SettingSource(typeof(EzHUDManiaStrings), nameof(EzHUDManiaStrings.SHIFT_WIDTH_LABEL), nameof(EzHUDManiaStrings.SHIFT_WIDTH_DESCRIPTION))]
        public BindableNumber<float> ShiftWidth { get; } = new BindableNumber<float>(0)
        {
            MinValue = -3f,
            MaxValue = 3f,
            Precision = 0.05f,
        };

        [SettingSource(typeof(EzHUDManiaStrings), nameof(EzHUDManiaStrings.SHIFT_HEIGHT_LABEL), nameof(EzHUDManiaStrings.SHIFT_HEIGHT_DESCRIPTION))]
        public BindableNumber<float> ShiftHeight { get; } = new BindableNumber<float>(0)
        {
            MinValue = -3f,
            MaxValue = 3f,
            Precision = 0.05f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.EFFECT_START_DURATION_LABEL), nameof(EzHUDStrings.EFFECT_START_DURATION_DESCRIPTION))]
        public BindableNumber<float> MotionStartTime { get; } = new BindableNumber<float>(10)
        {
            MinValue = 1f,
            MaxValue = 300f,
            Precision = 1f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.EFFECT_END_DURATION_LABEL), nameof(EzHUDStrings.EFFECT_END_DURATION_DESCRIPTION))]
        public BindableNumber<float> MotionEndDuration { get; } = new BindableNumber<float>(300)
        {
            MinValue = 10f,
            MaxValue = 500f,
            Precision = 10f,
        };

        [SettingSource(typeof(EzHUDManiaStrings), nameof(EzHUDManiaStrings.ROLL_DURATION_LABEL), nameof(EzHUDManiaStrings.ROLL_DURATION_DESCRIPTION))]
        public BindableNumber<float> RollDuration { get; } = new BindableNumber<float>(0)
        {
            MinValue = 0f,
            MaxValue = 500f,
            Precision = 10f,
        };

        [SettingSource(typeof(EzHUDStrings), nameof(EzHUDStrings.ALPHA_LABEL), nameof(EzHUDStrings.ALPHA_DESCRIPTION))]
        public BindableNumber<float> AccentAlpha { get; } = new BindableNumber<float>(1)
        {
            MinValue = 0f,
            MaxValue = 1f,
            Precision = 0.01f,
        };

        [SettingSource(typeof(SkinnableComponentStrings), nameof(SkinnableComponentStrings.Colour))]
        public BindableColour4 AccentColour { get; } = new BindableColour4(Colour4.White);

        public EzGlyphFlow Text = null!;

        protected override double RollingDuration => RollDuration.Value;

        public NeriComboCounter()
        {
            Origin = Anchor.Centre;
        }

        [BackgroundDependencyLoader]
        private void load(ScoreProcessor scoreProcessor, Ez2ConfigManager ezConfig)
        {
            Current.BindTo(scoreProcessor.Combo);
            Current.BindValueChanged(combo =>
            {
                bool wasIncrease = combo.NewValue > combo.OldValue;
                bool wasMiss = combo.OldValue > 1 && combo.NewValue == 0;

                applyAnimation(wasIncrease, wasMiss);
            });

            AccentAlpha.BindValueChanged(alpha => Text.Alpha = alpha.NewValue, true);
            AccentColour.BindValueChanged(_ => Text.SetColour(AccentColour.Value), true);

            ezConfig.GetBindable<string>(Ez2Setting.GameThemeName).BindValueChanged(theme =>
            {
                if (Enum.TryParse(theme.NewValue, out EzEnumDeformTemplate template))
                    AnimationTemplate.Value = template;
            });
        }

        private void applyAnimation(bool wasIncrease, bool wasMiss)
        {
            if (EzAnimationLibrary.TryPlayCombo(AnimationTemplate.Value, Text.TextContainer, false, Text.SourcePixelScale))
                return;

            Text.PlayComposite(wasIncrease, wasMiss, PeakScale.Value, TiltHeight.Value, ExpandWidth.Value, ShiftWidth.Value, ShiftHeight.Value, MotionStartTime.Value, MotionEndDuration.Value);
        }

        protected override void Update()
        {
            base.Update();

            Text.SetEffectOrigin(Origin);
        }

        protected override IHasText CreateText() => Text = new EzGlyphFlow(ThemeName);
    }
}
