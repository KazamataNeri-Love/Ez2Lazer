// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Numerics;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osuTK;

namespace osu.Android.EzAndroid.Input
{
    internal sealed partial class AndroidFormNumericStepper<T> : CompositeDrawable
        where T : struct, INumber<T>, IMinMaxValue<T>
    {
        private readonly FormNumericControlContext<T> context;
        private readonly BindableNumber<T> current;
        private readonly AndroidRepeatingStepButton decrease;
        private readonly AndroidRepeatingStepButton increase;
        private readonly ResetButton reset;

        internal AndroidFormNumericStepper(FormNumericControlContext<T> context)
        {
            this.context = context;
            current = context.CurrentInstantaneous.GetBoundCopy();

            Height = 40;
            InternalChildren = new Drawable[]
            {
                reset = new ResetButton
                {
                    Action = () =>
                    {
                        if (!current.Disabled && !current.IsDefault)
                            context.ResetToDefault();
                    },
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                },
                decrease = new AndroidRepeatingStepButton("-", () => step(-1), context.TransferValueOnCommit ? context.Commit : null, context.PlaySamplesOnAdjust)
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    X = -(AndroidRepeatingStepButton.BUTTON_WIDTH + 4),
                },
                increase = new AndroidRepeatingStepButton("+", () => step(1), context.TransferValueOnCommit ? context.Commit : null, context.PlaySamplesOnAdjust)
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            current.BindValueChanged(_ => updateResetVisibility(), true);
            current.DefaultChanged += _ => updateResetVisibility();
            current.BindDisabledChanged(disabled =>
            {
                decrease.Enabled.Value = increase.Enabled.Value = !disabled;
                updateResetVisibility();
            }, true);
        }

        private void step(int direction)
        {
            if (!current.Disabled)
                current.Value = AndroidNumericStepCalculator.Step(current, context.GetKeyboardStep(), direction);
        }

        private void updateResetVisibility()
        {
            bool visible = !current.Disabled && !current.IsDefault;
            reset.Enabled.Value = visible;
            reset.Alpha = visible ? 1 : 0;
        }

        private sealed partial class ResetButton : OsuButton, osu.Framework.Graphics.Cursor.IHasTooltip
        {
            public osu.Framework.Localisation.LocalisableString TooltipText => CommonStrings.RevertToDefault;

            public ResetButton()
            {
                Width = AndroidRepeatingStepButton.BUTTON_WIDTH;
                Add(new SpriteIcon
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Icon = FontAwesome.Solid.Undo,
                    Size = new osuTK.Vector2(16),
                });
            }
        }
    }
}
