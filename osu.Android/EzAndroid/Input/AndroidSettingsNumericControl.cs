// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Numerics;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Game.Graphics.UserInterface;

namespace osu.Android.EzAndroid.Input
{
    internal sealed partial class AndroidSettingsNumericControl<T> : CompositeDrawable
        where T : struct, INumber<T>, IMinMaxValue<T>
    {
        private readonly SettingsNumericControlContext<T> context;
        private readonly BindableNumber<T> current;
        private readonly OsuTextBox textBox;
        private readonly AndroidRepeatingStepButton decrease;
        private readonly AndroidRepeatingStepButton increase;
        private T? pendingStep;
        private bool updatingText;
        private bool updatingValueFromText;

        internal AndroidSettingsNumericControl(SettingsNumericControlContext<T> context, BindableNumber<T> current)
        {
            this.context = context;
            this.current = current.GetBoundCopy();

            RelativeSizeAxes = Axes.X;
            Height = 40;

            InternalChildren = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Right = (AndroidRepeatingStepButton.BUTTON_WIDTH + 4) * 2 },
                    Child = textBox = new OsuTextBox
                    {
                        RelativeSizeAxes = Axes.X,
                        InputProperties = new TextInputProperties(AndroidNumericStepCalculator.IsInteger<T>() ? TextInputType.Number : TextInputType.Decimal, false),
                        SelectAllOnFocus = true,
                        CommitOnFocusLost = true,
                    },
                },
                decrease = new AndroidRepeatingStepButton("-", () => step(-1), commitStep)
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    X = -(AndroidRepeatingStepButton.BUTTON_WIDTH + 4),
                },
                increase = new AndroidRepeatingStepButton("+", () => step(1), commitStep)
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            current.BindValueChanged(_ =>
            {
                pendingStep = null;
                if (!updatingValueFromText)
                    updateText();
            }, true);

            current.BindDisabledChanged(disabled =>
            {
                pendingStep = null;
                textBox.ReadOnly = disabled;
                textBox.Alpha = disabled ? 0.3f : 1;
                decrease.Enabled.Value = increase.Enabled.Value = !disabled;
                updateText();
            }, true);

            textBox.Current.BindValueChanged(e =>
            {
                if (!updatingText && !context.GetTransferValueOnCommit())
                    applyText(e.NewValue);
            });
            textBox.OnCommit += (_, _) =>
            {
                applyText(textBox.Text);
                updateText();
            };
        }

        private void applyText(string text)
        {
            if (current.Disabled || !AndroidNumericStepCalculator.TryParse(text, current, context.GetDisplayAsPercentage(), out T value))
                return;

            updatingValueFromText = true;
            try
            {
                current.Value = value;
            }
            finally
            {
                updatingValueFromText = false;
            }
        }

        private void step(int direction)
        {
            if (textBox.HasFocus)
                applyText(textBox.Text);

            if (current.Disabled)
                return;

            T value = AndroidNumericStepCalculator.Step(current, context.GetKeyboardStep(), direction, pendingStep);
            if (context.GetTransferValueOnCommit())
                pendingStep = value;
            else
                current.Value = value;

            updateText();
        }

        private void commitStep()
        {
            T? value = pendingStep;
            pendingStep = null;

            if (value.HasValue && !current.Disabled)
                current.Value = value.Value;

            updateText();
        }

        private void updateText()
        {
            updatingText = true;
            try
            {
                textBox.Text = AndroidNumericStepCalculator.Format(pendingStep ?? current.Value, context.GetDisplayAsPercentage());
            }
            finally
            {
                updatingText = false;
            }
        }
    }
}
