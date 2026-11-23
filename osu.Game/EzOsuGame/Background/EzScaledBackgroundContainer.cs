// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Performance;
using osu.Game.Graphics.Backgrounds;
using osu.Game.Graphics.Containers;
using osu.Game.Screens.Backgrounds;
using osu.Game.Screens.Play;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.EzOsuGame.Background
{
    /// <summary>
    /// 游玩界面的响应式背景缩放包装容器。
    /// 关闭总开关时退回原生零开销路径；开启总开关时（含满屏 1.00）统一走黑底 + 独立背景层 + 故事板共享缩放路径。
    /// </summary>
    public partial class EzScaledBackgroundContainer : CompositeDrawable
    {
        public readonly IBindable<bool> IsBreakTime = new Bindable<bool>();
        public readonly Bindable<bool> StoryboardReplacesBackground = new Bindable<bool>();

        private readonly WorkingBeatmap beatmap;
        private readonly DimmableStoryboard storyboard;
        private readonly Box blackUnderlay;
        private readonly Container backgroundSlot;
        private readonly Container backgroundWrapper;

        private ScaledDimmableBackground? scaledBackground;
        private BackgroundScreenBeatmap? attachedBackgroundScreen;
        private Player.GameplayBackdropSource? attachedBackdropSource;

        private Bindable<bool> scaleEnabled = null!;
        private Bindable<float> backgroundScale = null!;
        private Bindable<float> backgroundPosX = null!;
        private Bindable<float> backgroundPosY = null!;

        public EzScaledBackgroundContainer(WorkingBeatmap beatmap, DimmableStoryboard storyboard)
        {
            this.beatmap = beatmap;
            this.storyboard = storyboard;

            RelativeSizeAxes = Axes.Both;

            storyboard.RelativeSizeAxes = Axes.Both;
            storyboard.Anchor = Anchor.Centre;
            storyboard.Origin = Anchor.Centre;

            blackUnderlay = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Color4.Black,
                Alpha = 0,
            };

            backgroundSlot = new Container
            {
                RelativeSizeAxes = Axes.Both,
            };

            backgroundWrapper = new Container
            {
                RelativeSizeAxes = Axes.Both,
                RelativePositionAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Children = new Drawable[]
                {
                    backgroundSlot,
                    storyboard,
                },
            };

            InternalChildren = new Drawable[]
            {
                blackUnderlay,
                backgroundWrapper,
            };
        }

        /// <summary>
        /// 按缩放比例与归一化位置（0~1）计算容器相对中心偏移量。
        /// </summary>
        public static Vector2 ComputeRelativePosition(float scale, float posX, float posY)
        {
            float maxPan = Math.Max(0, (1 - scale) / 2);
            return new Vector2(
                (posX - 0.5f) * 2 * maxPan,
                (posY - 0.5f) * 2 * maxPan
            );
        }

        [BackgroundDependencyLoader]
        private void load(Ez2ConfigManager ezConfig)
        {
            scaleEnabled = ezConfig.GetBindable<bool>(Ez2Setting.BackgroundScaleEnabled);
            backgroundScale = ezConfig.GetBindable<float>(Ez2Setting.BackgroundScale);
            backgroundPosX = ezConfig.GetBindable<float>(Ez2Setting.BackgroundPosX);
            backgroundPosY = ezConfig.GetBindable<float>(Ez2Setting.BackgroundPosY);

            storyboard.FitInsideParent.BindTo(scaleEnabled);

            if (scaleEnabled.Value)
                ensureScaledBackground();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            scaleEnabled.BindValueChanged(_ => updateMode(), true);
            backgroundScale.BindValueChanged(_ => updateTransform());
            backgroundPosX.BindValueChanged(_ => updateTransform());
            backgroundPosY.BindValueChanged(_ => updateTransform(), true);
        }

        /// <summary>
        /// 绑定当前屏幕栈背景与背景捕获源，随缩放总开关实时隐藏/恢复底层背景。
        /// </summary>
        public void AttachBackgroundScreen(BackgroundScreenBeatmap backgroundScreen, Player.GameplayBackdropSource? backdropSource = null)
        {
            attachedBackgroundScreen = backgroundScreen;
            attachedBackdropSource = backdropSource;
            syncBackgroundScreen();
        }

        /// <summary>
        /// 在退出或挂起游玩界面时恢复底层屏幕栈背景并解除引用。
        /// </summary>
        public void DetachBackgroundScreen()
        {
            attachedBackgroundScreen?.Show();
            attachedBackgroundScreen = null;
            attachedBackdropSource = null;
        }

        private void ensureScaledBackground()
        {
            if (scaledBackground != null)
                return;

            scaledBackground = new ScaledDimmableBackground(beatmap);
            ((IBindable<bool>)scaledBackground.IsBreakTime).BindTo(IsBreakTime);
            scaledBackground.StoryboardReplacesBackground.BindTo(StoryboardReplacesBackground);

            backgroundSlot.Add(scaledBackground);
        }

        private void updateMode()
        {
            bool enabled = scaleEnabled.Value;
            blackUnderlay.Alpha = enabled ? 1 : 0;

            if (enabled)
            {
                ensureScaledBackground();
                scaledBackground!.Alpha = 1;
            }
            else if (scaledBackground != null)
            {
                scaledBackground.Alpha = 0;
            }

            updateTransform();
            syncBackgroundScreen();
        }

        private void updateTransform()
        {
            if (!scaleEnabled.Value)
            {
                backgroundWrapper.Scale = Vector2.One;
                backgroundWrapper.Position = Vector2.Zero;
                return;
            }

            float scale = backgroundScale.Value;
            backgroundWrapper.Scale = new Vector2(scale);
            backgroundWrapper.Position = ComputeRelativePosition(scale, backgroundPosX.Value, backgroundPosY.Value);
        }

        private void syncBackgroundScreen()
        {
            if (attachedBackgroundScreen == null)
                return;

            if (scaleEnabled.Value)
            {
                attachedBackgroundScreen.Hide();
                attachedBackdropSource?.SetBeatmapBackgroundSource(scaledBackground);
            }
            else
            {
                attachedBackgroundScreen.Show();
                attachedBackdropSource?.SetBeatmapBackgroundSource(attachedBackgroundScreen.CaptureSource);
            }
        }

        private partial class ScaledDimmableBackground : UserDimContainer
        {
            public readonly Bindable<bool> StoryboardReplacesBackground = new Bindable<bool>();

            private readonly WorkingBeatmap beatmap;
            private BeatmapBackground background = null!;
            private Bindable<double> userBlurLevel = null!;

            public ScaledDimmableBackground(WorkingBeatmap beatmap)
            {
                this.beatmap = beatmap;
                RelativeSizeAxes = Axes.Both;
            }

            [BackgroundDependencyLoader]
            private void load(OsuConfigManager config)
            {
                userBlurLevel = config.GetBindable<double>(OsuSetting.BlurLevel);

                background = new BeatmapBackground(beatmap);
                background.Sprite.FillMode = FillMode.Fit;
                Add(background);
            }

            protected override void LoadComplete()
            {
                background.BlurTo(blurTarget);
                base.LoadComplete();
                FinishTransforms(true);

                userBlurLevel.ValueChanged += _ => UpdateVisuals();
                StoryboardReplacesBackground.ValueChanged += _ => UpdateVisuals();
            }

            private Vector2 blurTarget => !IgnoreUserSettings.Value
                ? new Vector2((float)userBlurLevel.Value * BackgroundScreenBeatmap.USER_BLUR_FACTOR)
                : Vector2.Zero;

            protected override float DimLevel
            {
                get
                {
                    if ((IgnoreUserSettings.Value || ShowStoryboard.Value) && StoryboardReplacesBackground.Value)
                        return 1;

                    return base.DimLevel;
                }
            }

            protected override bool ShowDimContent
            {
                get
                {
                    if (EzTurboMode.Active && !IgnoreUserSettings.Value && DimLevel >= 1)
                        return false;

                    return base.ShowDimContent;
                }
            }

            protected override void UpdateVisuals()
            {
                base.UpdateVisuals();

                background?.BlurTo(blurTarget, BACKGROUND_FADE_DURATION, Easing.OutQuint);
            }
        }
    }
}
