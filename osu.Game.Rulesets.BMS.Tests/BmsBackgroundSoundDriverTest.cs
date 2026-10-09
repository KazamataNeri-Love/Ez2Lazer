// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Game.Rulesets.BMS.Beatmaps;

namespace osu.Game.Rulesets.BMS.Tests
{
    [TestFixture]
    public class BmsBackgroundSoundDriverTest
    {
        private static readonly Type driver_type = typeof(BMSRuleset).Assembly.GetType("osu.Game.Rulesets.BMS.UI.BmsBackgroundSoundDriver", throwOnError: true)!;

        /// <summary>
        /// The driver resolves its working beatmap, audio manager and config through DI, so it must be
        /// constructible without any arguments.
        /// </summary>
        [Test]
        public void TestConstructorTakesNoArguments()
        {
            object driver = createDriver();

            Assert.That(driver, Is.Not.Null);
            Assert.That(driver, Is.AssignableTo<Drawable>());

            bool alwaysPresent = (bool)driver_type.GetProperty("AlwaysPresent")!.GetValue(driver)!;
            Assert.That(alwaysPresent, Is.True);
        }

        [Test]
        public void TestUpdateBeforeBackgroundDependencyLoadIsSafe()
        {
            object driver = createDriver();

            MethodInfo update = driver_type.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)!;

            Assert.DoesNotThrow(() => update.Invoke(driver, Array.Empty<object>()));
        }

        [Test]
        public void TestDisposeBeforeBackgroundDependencyLoadIsSafe()
        {
            object driver = createDriver();

            // Drawable.Dispose() is public on the framework side and should be a no-op when no AudioManager
            // has been wired up yet.
            Assert.DoesNotThrow(() =>
            {
                MethodInfo? dispose = driver_type.GetMethod("Dispose", new[] { typeof(bool) })
                                      ?? driver_type.GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(bool) }, null);

                dispose?.Invoke(driver, new object[] { true });
            });
        }

        private static object createDriver()
        {
            var ctor = driver_type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null)!;

            return ctor.Invoke(Array.Empty<object>());
        }
    }
}
