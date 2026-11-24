// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Numerics;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Graphics.UserInterface;

namespace osu.Android.EzAndroid.Input
{
    internal sealed class AndroidMobileNumericControlFactory : IMobileNumericControlFactory
    {
        public Drawable CreateFormNumericControl<T>(FormNumericControlContext<T> context)
            where T : struct, INumber<T>, IMinMaxValue<T> => new AndroidFormNumericStepper<T>(context);

        public Drawable? CreateSettingsNumericControl<T>(SettingsNumericControlContext<T> context)
            where T : struct, INumber<T>, IMinMaxValue<T> =>
            context.Current is BindableNumber<T> number ? new AndroidSettingsNumericControl<T>(context, number) : null;
    }
}
