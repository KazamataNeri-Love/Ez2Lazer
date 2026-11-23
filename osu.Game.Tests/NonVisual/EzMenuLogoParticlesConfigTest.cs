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

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class EzMenuLogoParticlesConfigTest
    {
        [TestCase("MenuLogoParticlesStyle", "Gradient", "Off")]
        [TestCase("MenuLogoParticlesColour", "3A6BB8", "FF804080")]
        [TestCase("MenuLogoParticlesColourTop", "1A2B6B", "AA33CC")]
        [TestCase("MenuLogoParticlesColourBottom", "3A6BB8", "2255DD")]
        public void TestDefaultAndIniRoundTrip(string key, string defaultValue, string customValue)
        {
            using var storage = new TemporaryNativeStorage("ez-menu-logo-particles");
            using var config = new Ez2ConfigManager(storage);

            Assert.That(Enum.TryParse(key, out Ez2Setting setting), Is.True, $"Missing setting: {key}");
            var bindable = config.GetRawBindable(setting);
            Assert.That(bindable, Is.Not.Null);

            object expectedDefault = bindable is Bindable<Colour4> ? Colour4.FromHex(defaultValue) : Enum.Parse<EzLogoParticlesStyle>(defaultValue);
            object expectedCustom = bindable is Bindable<Colour4> ? Colour4.FromHex(customValue) : Enum.Parse<EzLogoParticlesStyle>(customValue);

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

        private static object? readValue(IBindable bindable) => bindable switch
        {
            Bindable<Colour4> colour => colour.Value,
            Bindable<EzLogoParticlesStyle> style => style.Value,
            _ => throw new InvalidOperationException($"Unexpected bindable type: {bindable.GetType()}"),
        };
    }
}
