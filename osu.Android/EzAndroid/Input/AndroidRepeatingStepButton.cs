// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Input.Events;
using osu.Framework.Threading;
using osu.Game;
using osu.Game.Graphics.UserInterface;
using osuTK.Input;

namespace osu.Android.EzAndroid.Input
{
    internal sealed partial class AndroidRepeatingStepButton : OsuButton
    {
        internal const float BUTTON_WIDTH = 44;
        private const double initial_delay = 300;
        private const double minimum_delay = 80;

        private readonly BindableBool gameActive = new BindableBool(true);
        private IBindable<bool>? gameActiveSource;
        private readonly Action step;
        private readonly Action? commit;
        private readonly bool playSamples;
        private readonly Action<ValueChangedEvent<bool>> gameActiveChanged;
        private Sample? sample;
        private ScheduledDelegate? repeat;
        private double delay;
        private bool pressed;

        internal AndroidRepeatingStepButton(string text, Action step, Action? commit, bool playSamples = true)
            : base(null)
        {
            Text = text;
            Width = BUTTON_WIDTH;
            this.step = step;
            this.commit = commit;
            this.playSamples = playSamples;
            gameActiveChanged = e => gameActive.Value = e.NewValue;
        }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio, OsuGameBase? game)
        {
            if (playSamples)
                sample = audio.Samples.Get(@"UI/notch-tick");

            if (game != null)
            {
                gameActiveSource = game.IsActive;
                gameActiveSource.BindValueChanged(gameActiveChanged, true);
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Enabled.BindValueChanged(e =>
            {
                if (!e.NewValue)
                {
                    stopRepeating();
                    pressed = false;
                }
            });
            gameActive.BindValueChanged(e =>
            {
                if (!e.NewValue)
                    finishPress();
            });
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            // Android's synthetic right button must neither start nor stop the left-button repeat.
            if (e.Button != MouseButton.Left)
                return true;

            if (Enabled.Value && gameActive.Value)
            {
                finishPress();
                base.OnMouseDown(e);
                pressed = true;
                delay = initial_delay;
                actuate();

                if (pressed)
                    repeat = Scheduler.AddDelayed(repeatStep, delay);
            }

            // Keep ancestors in the original input queue so the scroll container can take the drag.
            return false;
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            if (e.Button != MouseButton.Left)
                return;

            finishPress();
            base.OnMouseUp(e);
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (e.Button == MouseButton.Left)
                stopRepeating();

            return false;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            stopRepeating();
            base.OnHoverLost(e);
        }

        protected override bool OnClick(ClickEvent e) => true;

        private void repeatStep()
        {
            if (!pressed || !IsHovered || !IsAlive || !IsPresent || !Enabled.Value || !gameActive.Value)
                return;

            actuate();
            delay = Math.Max(minimum_delay, delay * 0.9);
            if (pressed)
                repeat = Scheduler.AddDelayed(repeatStep, delay);
        }

        private void actuate()
        {
            step();
            SampleChannel? channel = sample?.GetChannel();

            if (channel != null)
            {
                channel.Frequency.Value = 1 + 0.05 * (initial_delay - delay) / minimum_delay;
                channel.Play();
            }
        }

        private void finishPress()
        {
            stopRepeating();

            if (!pressed)
                return;

            pressed = false;
            commit?.Invoke();
        }

        private void stopRepeating()
        {
            repeat?.Cancel();
            repeat = null;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                stopRepeating();
                if (gameActiveSource != null)
                    gameActiveSource.ValueChanged -= gameActiveChanged;
            }

            base.Dispose(isDisposing);
        }
    }
}
