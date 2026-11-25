// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Screens.Menu;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class EzMenuLogoParticlesConfigTest
    {
        [Test]
        public void TestBackdropStyleValuesRemainStable()
        {
            Assert.That((int)EzLogoBackdropStyle.Solid, Is.EqualTo(0));
            Assert.That((int)EzLogoBackdropStyle.Acrylic, Is.EqualTo(1));
            Assert.That((int)EzLogoBackdropStyle.Clear, Is.EqualTo(2));
            Assert.That((int)EzLogoBackdropStyle.Gradient, Is.EqualTo(3));
        }

        [Test]
        public void TestSunStripeStyleValuesRemainStable()
        {
            Assert.That((int)EzLogoSunStripesStyle.Off, Is.EqualTo(0));
            Assert.That((int)EzLogoSunStripesStyle.Static, Is.EqualTo(1));
            Assert.That((int)EzLogoSunStripesStyle.Scrolling, Is.EqualTo(2));
            Assert.That((int)EzLogoSunStripesStyle.ScrollingUp, Is.EqualTo(3));
        }

        [TestCase("MenuLogoHideBorder", "False", "True")]
        [TestCase("MenuLogoDisableBeatPulse", "False", "True")]
        [TestCase("MenuLogoSunStripesStyle", "Off", "Scrolling")]
        [TestCase("MenuLogoSunStripesStyle", "Off", "ScrollingUp")]
        [TestCase("MenuLogoGlowEnabled", "False", "True")]
        [TestCase("MenuLogoGlowColour", "FF2E88", "FFC966")]
        [TestCase("MenuLogoGlowRadius", "120", "175")]
        [TestCase("MenuLogoParticlesStyle", "Gradient", "Off")]
        [TestCase("MenuLogoParticlesColour", "3A6BB8", "FF804080")]
        [TestCase("MenuLogoParticlesColourTop", "1A2B6B", "AA33CC")]
        [TestCase("MenuLogoParticlesColourBottom", "3A6BB8", "2255DD")]
        [TestCase("MenuLogoBackdropStyle", "Acrylic", "Gradient")]
        [TestCase("MenuLogoBackdropColourTop", "ff66ab", "AA33CC")]
        [TestCase("MenuLogoBackdropColourBottom", "b6346f", "2255DD")]
        public void TestDefaultAndIniRoundTrip(string key, string defaultValue, string customValue)
        {
            using var storage = new TemporaryNativeStorage("ez-menu-logo-particles");
            using var config = new Ez2ConfigManager(storage);

            Assert.That(Enum.TryParse(key, out Ez2Setting setting), Is.True, $"Missing setting: {key}");
            var bindable = config.GetRawBindable(setting);
            Assert.That(bindable, Is.Not.Null);

            object expectedDefault = parseExpected(bindable!, defaultValue);
            object expectedCustom = parseExpected(bindable!, customValue);

            Assert.That(readValue(bindable!), Is.EqualTo(expectedDefault));
            ((IParseable)bindable!).Parse(customValue, CultureInfo.InvariantCulture);
            config.Save();

            using var reader = new StreamReader(storage.GetStream("EzSkinSettings.ini"));
            string savedValue = reader.ReadToEnd().Split('\n').Single(line => line.StartsWith(key + " ", StringComparison.Ordinal)).Split('=', 2)[1].Trim();

            // Ez2ConfigManager skips file loading under NUnit; use the same bindable parser as INI loading.
            using var reloadedConfig = new Ez2ConfigManager(storage);
            var reloaded = reloadedConfig.GetRawBindable(setting)!;
            ((IParseable)reloaded).Parse(savedValue, CultureInfo.InvariantCulture);
            Assert.That(readValue(reloaded), Is.EqualTo(expectedCustom));
        }

        [Test]
        public void TestSynthwavePresetPreservesCustomTextAndTexture()
        {
            using var storage = new TemporaryNativeStorage("ez-menu-logo-preset");
            using var config = new Ez2ConfigManager(storage);

            config.SetValue(Ez2Setting.MenuLogoText, "Custom text,90");
            config.SetValue(Ez2Setting.MenuLogoPath, "Menu/custom");
            EzMenuLogoThemePresets.ApplySynthwaveSunset(config);

            Assert.That(config.Get<string>(Ez2Setting.MenuLogoText), Is.EqualTo("Custom text,90"));
            Assert.That(config.Get<string>(Ez2Setting.MenuLogoPath), Is.EqualTo("Menu/custom"));
            Assert.That(config.Get<bool>(Ez2Setting.MenuLogoHideBorder), Is.True);
            Assert.That(config.Get<bool>(Ez2Setting.MenuLogoDisableBeatPulse), Is.True);
            Assert.That(config.Get<EzLogoVisualisationStyle>(Ez2Setting.MenuLogoVisualisationStyle), Is.EqualTo(EzLogoVisualisationStyle.Off));
            Assert.That(config.Get<EzLogoBackdropStyle>(Ez2Setting.MenuLogoBackdropStyle), Is.EqualTo(EzLogoBackdropStyle.Gradient));
            Assert.That(config.Get<Colour4>(Ez2Setting.MenuLogoBackdropColourTop), Is.EqualTo(Colour4.FromHex("FFE066")));
            Assert.That(config.Get<Colour4>(Ez2Setting.MenuLogoBackdropColourBottom), Is.EqualTo(Colour4.FromHex("FF2E88")));
            Assert.That(config.Get<EzLogoParticlesStyle>(Ez2Setting.MenuLogoParticlesStyle), Is.EqualTo(EzLogoParticlesStyle.Off));
            Assert.That(config.Get<EzLogoSunStripesStyle>(Ez2Setting.MenuLogoSunStripesStyle), Is.EqualTo(EzLogoSunStripesStyle.Scrolling));
            Assert.That(config.Get<bool>(Ez2Setting.MenuLogoGlowEnabled), Is.True);
            Assert.That(config.Get<Colour4>(Ez2Setting.MenuLogoGlowColour), Is.EqualTo(Colour4.FromHex("FF2E88")));
            Assert.That(config.Get<double>(Ez2Setting.MenuLogoGlowRadius), Is.EqualTo(120));
        }

        [TestCase(0f)]
        [TestCase(0.125f)]
        [TestCase(0.5f)]
        [TestCase(0.995f)]
        public void TestSunStripeGapsWidenWithDepth(float phase)
        {
            Span<(float Start, float End)> bands = stackalloc (float, float)[10];
            int count = EzMenuLogoSunStripes.ComputeBands(phase, bands);

            Assert.That(count, Is.GreaterThan(1));
            Assert.That(bands[0].Start, Is.Zero);
            Assert.That(bands[0].End, Is.GreaterThanOrEqualTo(0.44f));

            float lastEnd = -1;
            foreach (var (start, end) in bands[..count])
            {
                Assert.That(start, Is.GreaterThan(lastEnd));
                Assert.That(end, Is.GreaterThan(start).And.LessThanOrEqualTo(1));
                lastEnd = end;
            }

            float previousGapWidth = 0;

            for (int i = 1; i < count; i++)
            {
                float gapWidth = bands[i].Start - bands[i - 1].End;
                Assert.That(gapWidth, Is.GreaterThanOrEqualTo(0.008f - 0.000001f), $"Complete gap {i} must remain above the subpixel range at phase {phase}.");
                Assert.That(gapWidth, Is.GreaterThan(previousGapWidth), $"Gap {i} must be wider than the preceding gap at phase {phase}.");
                previousGapWidth = gapWidth;
            }
        }

        [Test]
        public void TestSunStripeClippedGapShrinksToBottomEdge()
        {
            Span<(float Start, float End)> bands = stackalloc (float, float)[10];
            float previous = 0.008f;
            foreach (float phase in new[] { 0.97f, 0.98f, 0.99f, 0.9999f, 0.999999f })
            {
                int count = EzMenuLogoSunStripes.ComputeBands(phase, bands);
                float clippedWidth = 1 - bands[count - 1].End;
                Assert.That(clippedWidth, Is.GreaterThan(0).And.LessThan(previous), $"Clipped gap at phase {phase}");
                previous = clippedWidth;
            }

            Assert.That(previous, Is.LessThan(0.00001f));
        }

        [Test]
        public void TestSunStripeBirthBridgeFadesAcrossCycle()
        {
            Assert.That(EzMenuLogoSunStripes.ComputeBirthBridgeAlpha(0), Is.EqualTo(1));
            Assert.That(EzMenuLogoSunStripes.ComputeBirthBridgeAlpha(0.5f), Is.EqualTo(0.5f));
            Assert.That(EzMenuLogoSunStripes.ComputeBirthBridgeAlpha(1), Is.EqualTo(0));

            float previous = 1;
            for (int i = 1; i <= 100; i++)
            {
                float alpha = EzMenuLogoSunStripes.ComputeBirthBridgeAlpha(i / 100f);
                Assert.That(alpha, Is.InRange(0, previous));
                previous = alpha;
            }
        }

        [Test]
        public void TestUpwardSunStripeBirthBridgeStartsOpaqueAtBottomBoundary()
        {
            float phase = EzMenuLogoSunStripes.AdvancePhase(0, 0.000001f, EzLogoSunStripesStyle.ScrollingUp);
            Assert.That(phase, Is.GreaterThan(0.99999f));
            Assert.That(EzMenuLogoSunStripes.ComputeBirthBridgeAlpha(phase, EzLogoSunStripesStyle.ScrollingUp), Is.EqualTo(1).Within(0.00001f));

            var (start, end) = EzMenuLogoSunStripes.ComputeBirthBridgeBounds(phase, EzLogoSunStripesStyle.ScrollingUp);
            Assert.That(start, Is.GreaterThan(0.99f));
            Assert.That(end, Is.EqualTo(1));
        }

        [Test]
        public void TestUpwardSunStripeBirthBridgeFadesAsItMovesUp()
        {
            float previousAlpha = EzMenuLogoSunStripes.ComputeBirthBridgeAlpha(1, EzLogoSunStripesStyle.ScrollingUp);

            for (int i = 1; i <= 100; i++)
            {
                float phase = 1 - i / 100f;
                float alpha = EzMenuLogoSunStripes.ComputeBirthBridgeAlpha(phase, EzLogoSunStripesStyle.ScrollingUp);
                Assert.That(alpha, Is.InRange(0, previousAlpha));
                previousAlpha = alpha;
            }

            var (start, end) = EzMenuLogoSunStripes.ComputeBirthBridgeBounds(0, EzLogoSunStripesStyle.ScrollingUp);
            Assert.That(start, Is.LessThan(0.95f));
            Assert.That(end, Is.LessThan(1));
        }

        [TestCase(EzLogoSunStripesStyle.Scrolling)]
        [TestCase(EzLogoSunStripesStyle.ScrollingUp)]
        public void TestSunStripeSlotsRemainContinuousAcrossCycle(EzLogoSunStripesStyle style)
        {
            bool upwards = style == EzLogoSunStripesStyle.ScrollingUp;
            float from = upwards ? 0f : 0.999999f;
            float to = EzMenuLogoSunStripes.AdvancePhase(from, 0.000001f, style);
            Assert.That(to, Is.EqualTo(upwards ? 0.999999f : 0f).Within(0.0000001f));

            float beforeWrapPhase = upwards ? to : from;
            float atWrapPhase = upwards ? from : to;

            Span<(float Start, float End)> start = stackalloc (float, float)[10];
            Span<(float Start, float End)> beforeWrap = stackalloc (float, float)[10];
            int startCount = EzMenuLogoSunStripes.ComputeBands(atWrapPhase, start);
            int beforeWrapCount = EzMenuLogoSunStripes.ComputeBands(beforeWrapPhase, beforeWrap);

            float alphaAtWrap = EzMenuLogoSunStripes.ComputeBirthBridgeAlpha(atWrapPhase, style);
            float alphaBeforeWrap = EzMenuLogoSunStripes.ComputeBirthBridgeAlpha(beforeWrapPhase, style);
            Assert.That(alphaAtWrap, Is.EqualTo(upwards ? 0 : 1).Within(0.00001f));
            Assert.That(alphaBeforeWrap, Is.EqualTo(upwards ? 1 : 0).Within(0.00001f));
            Assert.That(startCount, Is.EqualTo(beforeWrapCount + 1));
            // At phase zero the opaque bridge joins the first two bands into one.
            Assert.That(beforeWrap[0].Start, Is.EqualTo(start[0].Start).Within(0.00001f));
            for (int i = 1; i < startCount; i++)
            {
                if (i > 1)
                    Assert.That(beforeWrap[i - 1].Start, Is.EqualTo(start[i].Start).Within(0.00001f));

                Assert.That(beforeWrap[i - 1].End, Is.EqualTo(start[i].End).Within(0.00001f));
            }
        }

        private static object? readValue(IBindable bindable) => bindable switch
        {
            Bindable<Colour4> colour => colour.Value,
            Bindable<EzLogoParticlesStyle> style => style.Value,
            Bindable<EzLogoBackdropStyle> style => style.Value,
            Bindable<EzLogoSunStripesStyle> style => style.Value,
            Bindable<bool> flag => flag.Value,
            Bindable<double> radius => radius.Value,
            _ => throw new InvalidOperationException($"Unexpected bindable type: {bindable.GetType()}"),
        };

        private static object parseExpected(IBindable bindable, string value) => bindable switch
        {
            Bindable<Colour4> => Colour4.FromHex(value),
            Bindable<EzLogoParticlesStyle> => Enum.Parse<EzLogoParticlesStyle>(value),
            Bindable<EzLogoBackdropStyle> => Enum.Parse<EzLogoBackdropStyle>(value),
            Bindable<EzLogoSunStripesStyle> => Enum.Parse<EzLogoSunStripesStyle>(value),
            Bindable<bool> => bool.Parse(value),
            Bindable<double> => double.Parse(value, CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException($"Unexpected bindable type: {bindable.GetType()}"),
        };
    }
}
