// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.Numerics;
using osu.Framework.Bindables;

namespace osu.Android.EzAndroid.Input
{
    internal static class AndroidNumericStepCalculator
    {
        internal static bool IsInteger<T>() where T : INumber<T> => T.IsInteger(T.One / (T.One + T.One));

        internal static T Step<T>(BindableNumber<T> current, float keyboardStep, int direction, T? pending = null)
            where T : struct, INumber<T>, IMinMaxValue<T>
        {
            T value = pending ?? current.Value;
            bool hasKeyboardStep = float.IsFinite(keyboardStep) && keyboardStep > 0;

            if (IsInteger<T>())
            {
                decimal min = decimal.CreateChecked(current.MinValue);
                decimal max = decimal.CreateChecked(current.MaxValue);
                decimal step = hasKeyboardStep ? decimal.CreateSaturating(keyboardStep) : decimal.CreateChecked(current.Precision);
                step = Math.Clamp(Math.Round(step, MidpointRounding.AwayFromZero), 1, Math.Max(1, max - min));
                return T.CreateChecked(Math.Clamp(decimal.CreateChecked(value) + direction * step, min, max));
            }

            double minFloat = double.CreateChecked(current.MinValue);
            double maxFloat = double.CreateChecked(current.MaxValue);
            double precision = double.CreateChecked(current.Precision);
            // The floating-point bindables use epsilon as the sentinel for unconstrained precision.
            double epsilon = typeof(T) == typeof(float) ? float.Epsilon : double.Epsilon;
            double stepFloat = hasKeyboardStep ? double.Parse(keyboardStep.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)
                : precision > epsilon && double.IsFinite(precision) ? precision
                : fallbackStep(minFloat, maxFloat);
            double adjusted = double.CreateChecked(value) + direction * stepFloat;
            return T.CreateSaturating(Math.Clamp(adjusted, minFloat, maxFloat));
        }

        private static double fallbackStep(double min, double max)
        {
            double step = (max - min) / 100;
            if (!double.IsFinite(step))
                step = max / 100 - min / 100;

            if (step <= 0)
                return double.Epsilon;

            double scale = Math.Pow(10, Math.Max(-323, Math.Floor(Math.Log10(step))));
            return Math.Max(double.Epsilon, Math.Round(step / scale, MidpointRounding.AwayFromZero) * scale);
        }

        internal static string Format<T>(T value, bool displayAsPercentage) where T : INumber<T>
        {
            if (T.IsZero(value))
                return "0";

            string text = value.ToString(null, CultureInfo.CurrentCulture);
            if (IsInteger<T>())
                return text;

            // Expand the round-trip representation without losing tiny values or emitting an exponent the numeric IME cannot edit.
            int exponentIndex = text.IndexOf('E');
            int shift = displayAsPercentage ? 2 : 0;
            if (exponentIndex < 0 && shift == 0 || !T.IsFinite(value))
                return text;

            if (exponentIndex >= 0)
            {
                shift += int.Parse(text.AsSpan(exponentIndex + 1), NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture);
                text = text[..exponentIndex];
            }

            NumberFormatInfo format = CultureInfo.CurrentCulture.NumberFormat;
            string sign = text.StartsWith(format.NegativeSign, StringComparison.Ordinal) ? format.NegativeSign : string.Empty;
            text = text[sign.Length..];
            string separator = format.NumberDecimalSeparator;
            int decimalIndex = text.IndexOf(separator, StringComparison.Ordinal);
            int position = (decimalIndex < 0 ? text.Length : decimalIndex) + shift;
            string digits = decimalIndex < 0 ? text : text.Remove(decimalIndex, separator.Length);

            int leadingZeroes = 0;
            while (leadingZeroes < digits.Length - 1 && digits[leadingZeroes] == '0')
                leadingZeroes++;
            digits = digits[leadingZeroes..];
            position -= leadingZeroes;

            if (position <= 0)
                return sign + "0" + separator + new string('0', -position) + digits;
            if (position >= digits.Length)
                return sign + digits + new string('0', position - digits.Length);

            string fraction = digits[position..].TrimEnd('0');
            return sign + digits[..position] + (fraction.Length > 0 ? separator + fraction : string.Empty);
        }

        internal static bool TryParse<T>(string text, BindableNumber<T> current, bool displayAsPercentage, out T value)
            where T : struct, INumber<T>, IMinMaxValue<T>
        {
            value = current.Value;

            if (IsInteger<T>())
            {
                if (decimal.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out decimal integral))
                {
                    value = T.CreateChecked(Math.Clamp(integral, decimal.CreateChecked(current.MinValue), decimal.CreateChecked(current.MaxValue)));
                    return true;
                }

                if (!double.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out double overflow))
                    return false;

                value = overflow < 0 ? current.MinValue : current.MaxValue;
                return true;
            }

            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double number) || double.IsNaN(number))
                return false;

            if (displayAsPercentage)
                number /= 100;

            value = T.CreateSaturating(Math.Clamp(number, double.CreateChecked(current.MinValue), double.CreateChecked(current.MaxValue)));
            return true;
        }
    }
}
