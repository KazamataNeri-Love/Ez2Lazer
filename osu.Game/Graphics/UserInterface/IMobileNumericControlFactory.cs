// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Numerics;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;

namespace osu.Game.Graphics.UserInterface
{
    /// <summary>
    /// Provides optional alternatives to the default numeric sliders. Returning <see langword="null"/> keeps the original control.
    /// </summary>
    public interface IMobileNumericControlFactory
    {
        Drawable? CreateFormNumericControl<T>(FormNumericControlContext<T> context) where T : struct, INumber<T>, IMinMaxValue<T>;

        Drawable? CreateSettingsNumericControl<T>(SettingsNumericControlContext<T> context) where T : struct, INumber<T>, IMinMaxValue<T>;
    }

    /// <summary>
    /// Numeric state for a form slider's right-hand control. Edit <see cref="CurrentInstantaneous"/> and call
    /// <see cref="Commit"/> to publish pending changes when <see cref="TransferValueOnCommit"/> is enabled.
    /// </summary>
    public sealed record FormNumericControlContext<T>(
        BindableNumber<T> CurrentInstantaneous,
        Func<float> GetKeyboardStep,
        bool DisplayAsPercentage,
        bool TransferValueOnCommit,
        bool PlaySamplesOnAdjust,
        Action Commit,
        Action ResetToDefault,
        Func<T, LocalisableString> FormatValue)
        where T : struct, INumber<T>, IMinMaxValue<T>;

    /// <summary>
    /// Numeric state for a settings slider. The replacement must defer writing to <see cref="Current"/>
    /// until commit when <see cref="GetTransferValueOnCommit"/> returns <see langword="true"/>.
    /// </summary>
    public sealed record SettingsNumericControlContext<T>(
        Bindable<T> Current,
        Func<float> GetKeyboardStep,
        Func<bool> GetDisplayAsPercentage,
        Func<bool> GetTransferValueOnCommit)
        where T : struct, INumber<T>, IMinMaxValue<T>;
}
