// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.EzOsuGame.Configuration;
using osuTK;

namespace osu.Game.Graphics.Cursor
{
    public partial class MenuCursorContainer : CursorContainer
    {
        private readonly IBindable<bool> screenshotCursorVisibility = new Bindable<bool>(true);
        public override bool IsPresent => screenshotCursorVisibility.Value && base.IsPresent;

        private bool hideCursorOnNonMouseInput;

        public bool HideCursorOnNonMouseInput
        {
            get => hideCursorOnNonMouseInput;
            set
            {
                if (hideCursorOnNonMouseInput == value)
                    return;

                hideCursorOnNonMouseInput = value;
                updateState();
            }
        }

        protected override Drawable CreateCursor() => activeCursor = new Cursor();

        private Cursor activeCursor = null!;

        private DragRotationState dragRotationState;
        private Vector2 positionMouseDown;
        private Vector2 lastMovePosition;

        private Bindable<bool> cursorRotate = null!;
        private Sample tapSample = null!;

        private MouseInputDetector mouseInputDetector = null!;

        private EzLegacyCursorTrail? skinCursorTrail;
        private Container? skinCursorTrailContainer;

        private bool visible;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, ScreenshotManager? screenshotManager, AudioManager audio)
        {
            cursorRotate = config.GetBindable<bool>(OsuSetting.CursorRotation);

            if (screenshotManager != null)
                screenshotCursorVisibility.BindTo(screenshotManager.CursorVisibility);

            tapSample = audio.Samples.Get(@"UI/cursor-tap");

            Add(mouseInputDetector = new MouseInputDetector());

            // the trail needs a stationary full-screen space (it must not follow the cursor drawable), and must be
            // drawn *behind* the cursor art (lower Depth is drawn in front in osu.Framework, so it uses a higher
            // depth than the cursor).
            // it lives in its own container so that its visibility can mirror the cursor's, otherwise it would keep
            // drawing over the playfield / preview cursors while the menu cursor is hidden.
            Add(skinCursorTrailContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Depth = 1,
                Alpha = 0,
                Child = skinCursorTrail = new EzLegacyCursorTrail(),
            });

            activeCursor.SkinTrail = skinCursorTrail;

            // the container is created after the visibility state may already have been evaluated, so sync it.
            skinCursorTrailContainer.Alpha = getCursorVisibility() ? 1 : 0;
        }

        [Resolved]
        private OsuGame? game { get; set; }

        private readonly IBindable<bool> lastInputWasMouse = new BindableBool();
        private readonly IBindable<bool> gameActive = new BindableBool(true);
        private readonly IBindable<bool> gameIdle = new BindableBool();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            lastInputWasMouse.BindTo(mouseInputDetector.LastInputWasMouseSource);
            lastInputWasMouse.BindValueChanged(_ => updateState(), true);

            if (game != null)
            {
                gameIdle.BindTo(game.IsIdle);
                gameIdle.BindValueChanged(_ => updateState());

                gameActive.BindTo(game.IsActive);
                gameActive.BindValueChanged(_ => updateState());
            }
        }

        protected override void UpdateState(ValueChangedEvent<Visibility> state) => updateState();

        private void updateState()
        {
            bool combinedVisibility = getCursorVisibility();

            if (visible == combinedVisibility)
                return;

            visible = combinedVisibility;

            if (visible)
                PopIn();
            else
                PopOut();
        }

        private bool getCursorVisibility()
        {
            // do not display when explicitly set to hidden state.
            if (State.Value == Visibility.Hidden)
                return false;

            // only hide cursor when game is focused, otherwise it should always be displayed.
            if (gameActive.Value)
            {
                // do not display when last input is not mouse.
                if (hideCursorOnNonMouseInput && !lastInputWasMouse.Value)
                    return false;

                // do not display when game is idle.
                if (gameIdle.Value)
                    return false;
            }

            return true;
        }

        protected override void Update()
        {
            base.Update();

            if (dragRotationState != DragRotationState.NotDragging
                && Vector2.Distance(positionMouseDown, lastMovePosition) > 60)
            {
                // make the rotation centre point floating.
                positionMouseDown = Interpolation.ValueAt(0.04f, positionMouseDown, lastMovePosition, 0, Clock.ElapsedFrameTime);
            }
        }

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            if (dragRotationState != DragRotationState.NotDragging)
            {
                lastMovePosition = e.MousePosition;

                float distance = Vector2Extensions.Distance(lastMovePosition, positionMouseDown);

                // don't start rotating until we're moved a minimum distance away from the mouse down location,
                // else it can have an annoying effect.
                if (dragRotationState == DragRotationState.DragStarted && distance > 80)
                    dragRotationState = DragRotationState.Rotating;

                // don't rotate when distance is zero to avoid NaN
                if (dragRotationState == DragRotationState.Rotating && distance > 0)
                {
                    Vector2 offset = e.MousePosition - positionMouseDown;
                    float degrees = float.RadiansToDegrees(MathF.Atan2(-offset.X, offset.Y)) + 24.3f;

                    // Always rotate in the direction of least distance
                    float diff = (degrees - activeCursor.Rotation) % 360;
                    if (diff < -180) diff += 360;
                    if (diff > 180) diff -= 360;
                    degrees = activeCursor.Rotation + diff;

                    activeCursor.RotateTo(degrees, 120, Easing.OutQuint);
                }
            }

            return base.OnMouseMove(e);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (State.Value == Visibility.Visible)
            {
                // only trigger animation for main mouse buttons
                if (activeCursor.UsingSkinCursor)
                {
                    // skin cursors handle their own pressed state (matching gameplay).
                    activeCursor.Expand();
                }
                else
                {
                    activeCursor.Scale = new Vector2(1);
                    activeCursor.ScaleTo(0.90f, 800, Easing.OutQuint);

                    activeCursor.AdditiveLayer.Alpha = 0;
                    activeCursor.AdditiveLayer.FadeInFromZero(800, Easing.OutQuint);
                }

                if (cursorRotate.Value && dragRotationState != DragRotationState.Rotating)
                {
                    // if cursor is already rotating don't reset its rotate origin
                    dragRotationState = DragRotationState.DragStarted;
                    positionMouseDown = e.MousePosition;
                }

                playTapSample();
            }

            return base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            if (!e.HasAnyButtonPressed)
            {
                if (activeCursor.UsingSkinCursor)
                    activeCursor.Contract();
                else
                {
                    activeCursor.AdditiveLayer.FadeOutFromOne(500, Easing.OutQuint);
                    activeCursor.ScaleTo(1, 500, Easing.OutElastic);
                }

                if (dragRotationState != DragRotationState.NotDragging)
                {
                    activeCursor.RotateTo(0, 400 * (0.5f + Math.Abs(activeCursor.Rotation / 960)), Easing.OutElasticQuarter);
                    dragRotationState = DragRotationState.NotDragging;
                }

                if (State.Value == Visibility.Visible)
                    playTapSample(0.8);
            }

            base.OnMouseUp(e);
        }

        protected override void PopIn()
        {
            // prevent the trail from connecting to a stale position from before the cursor was hidden.
            skinCursorTrail?.ResetTrail();

            skinCursorTrailContainer?.FadeTo(1, 250, Easing.OutQuint);

            activeCursor.FadeTo(1, 250, Easing.OutQuint);
            activeCursor.ScaleTo(1, 400, Easing.OutQuint);

            if (dragRotationState == DragRotationState.NotDragging)
                activeCursor.RotateTo(0, 400, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            skinCursorTrail?.ResetTrail();

            skinCursorTrailContainer?.FadeTo(0, 250, Easing.OutQuint);

            activeCursor.FadeTo(0, 250, Easing.OutQuint);
            activeCursor.ScaleTo(0.6f, 250, Easing.In);

            if (dragRotationState == DragRotationState.NotDragging)
                activeCursor.RotateTo(0, 400, Easing.OutQuint);
        }

        private void playTapSample(double baseFrequency = 1f)
        {
            const float random_range = 0.02f;
            SampleChannel channel = tapSample.GetChannel();

            // Scale to [-0.75, 0.75] so that the sample isn't fully panned left or right (sounds weird)
            channel.Balance.Value = ((activeCursor.X / DrawWidth) * 2 - 1) * OsuGameBase.SFX_STEREO_STRENGTH;
            channel.Frequency.Value = baseFrequency - (random_range / 2f) + RNG.NextDouble(random_range);
            channel.Volume.Value = baseFrequency;

            channel.Play();
        }

        public partial class Cursor : Container
        {
            private Container cursorContainer = null!;
            private Container defaultCursorContainer = null!;
            private Container skinCursorContainer = null!;
            private EzSkinCursor skinCursor = null!;
            private Bindable<float> cursorScale = null!;
            private Bindable<bool> useSkinCursor = null!;
            private const float base_scale = 0.15f;

            public Sprite AdditiveLayer = null!;

            /// <summary>
            /// The skin cursor trail, if one has been attached by the enclosing <see cref="MenuCursorContainer"/>.
            /// </summary>
            public EzLegacyCursorTrail? SkinTrail;

            public Cursor()
            {
                AutoSizeAxes = Axes.Both;
            }

            [BackgroundDependencyLoader]
            private void load(OsuConfigManager config, Ez2ConfigManager ezConfig, TextureStore textures, OsuColour colour)
            {
                Children = new Drawable[]
                {
                    cursorContainer = new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            defaultCursorContainer = new Container
                            {
                                AutoSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new Sprite
                                    {
                                        Texture = textures.Get(@"Cursor/menu-cursor"),
                                    },
                                    AdditiveLayer = new Sprite
                                    {
                                        Blending = BlendingParameters.Additive,
                                        Colour = colour.Pink,
                                        Alpha = 0,
                                        Texture = textures.Get(@"Cursor/menu-cursor-additive"),
                                    },
                                }
                            },
                            skinCursorContainer = new Container
                            {
                                // kept present at all times (via AlwaysPresent) so the skin lookup stays up to date,
                                // visibility is handled below with Alpha instead.
                                AlwaysPresent = true,
                                Alpha = 0,
                                Child = skinCursor = new EzSkinCursor
                                {
                                    // gameplay/preview cursors are centred on the pointer, so mirror that here.
                                    Origin = Anchor.Centre,
                                },
                            },
                        }
                    }
                };

                // whether the current skin can provide a cursor is only known once the skin cursor has loaded, hence the callback.
                skinCursor.OnSkinChanged += updateCursorStyle;

                cursorScale = config.GetBindable<float>(OsuSetting.MenuCursorSize);
                cursorScale.BindValueChanged(scale =>
                {
                    defaultCursorContainer.Scale = new Vector2(scale.NewValue * base_scale);
                    // EzSkinCursor already presents itself at gameplay size, so no additional base scale is applied here.
                    skinCursorContainer.Scale = new Vector2(scale.NewValue);
                }, true);

                useSkinCursor = ezConfig.GetBindable<bool>(Ez2Setting.UseSkinCursor);
                useSkinCursor.BindValueChanged(_ => updateCursorStyle(), true);
            }

            private void updateCursorStyle()
            {
                if (useSkinCursor == null)
                    return;

                bool showSkinCursor = useSkinCursor.Value && skinCursor.HasSkinCursor;

                defaultCursorContainer.Alpha = showSkinCursor ? 0 : 1;
                skinCursorContainer.Alpha = showSkinCursor ? 1 : 0;

                if (SkinTrail != null)
                    SkinTrail.Alpha = showSkinCursor && SkinTrail.HasTrail ? 1 : 0;
            }

            /// <summary>
            /// Whether the skin cursor is currently being displayed instead of the default menu cursor.
            /// </summary>
            public bool UsingSkinCursor => useSkinCursor?.Value == true && skinCursor?.HasSkinCursor == true;

            /// <summary>
            /// Expands the skin cursor, mirroring the gameplay cursor's click feedback.
            /// </summary>
            public void Expand() => skinCursor.Expand();

            /// <summary>
            /// Contracts the skin cursor, mirroring the gameplay cursor's click feedback.
            /// </summary>
            public void Contract() => skinCursor.Contract();
        }

        private partial class MouseInputDetector : Component
        {
            /// <summary>
            /// Whether the last input applied to the game is sourced from mouse.
            /// </summary>
            public IBindable<bool> LastInputWasMouseSource => lastInputWasMouseSource;

            private readonly Bindable<bool> lastInputWasMouseSource = new Bindable<bool>();

            public MouseInputDetector()
            {
                RelativeSizeAxes = Axes.Both;
            }

            protected override bool Handle(UIEvent e)
            {
                switch (e)
                {
                    case MouseDownEvent:
                    case MouseMoveEvent:
                        lastInputWasMouseSource.Value = true;
                        return false;

                    case KeyDownEvent keyDown when !keyDown.Repeat:
                    case JoystickPressEvent:
                    case MidiDownEvent:
                        lastInputWasMouseSource.Value = false;
                        return false;
                }

                return false;
            }
        }

        private enum DragRotationState
        {
            NotDragging,
            DragStarted,
            Rotating,
        }
    }
}
