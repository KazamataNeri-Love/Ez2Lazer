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

        [TestCase("MenuLogoHideBorder", "False", "True")]
        [TestCase("MenuLogoSunStripesStyle", "Off", "Scrolling")]
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
                Assert.That(gapWidth, Is.GreaterThan(previousGapWidth), $"Gap {i} must be wider than the preceding gap at phase {phase}.");
                previousGapWidth = gapWidth;
            }
        }

        [Test]
        public void TestSunStripePhaseWrapIsContinuous()
        {
            var start = new (float Start, float End)[10];
            var beforeWrap = new (float Start, float End)[10];
            int count = EzMenuLogoSunStripes.ComputeBands(0, start);
            int beforeWrapCount = EzMenuLogoSunStripes.ComputeBands(0.999999f, beforeWrap);

            Assert.That(beforeWrapCount, Is.EqualTo(count));
            for (int i = 0; i < count; i++)
            {
                Assert.That(beforeWrap[i].Start, Is.EqualTo(start[i].Start).Within(0.00001f));
                Assert.That(beforeWrap[i].End, Is.EqualTo(start[i].End).Within(0.00001f));
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
