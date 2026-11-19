// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Configuration;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class RulesetConfigManagerRawBindableTest
    {
        [Test]
        public void TestKnownKeyReturnsRegisteredBindable()
        {
            using var manager = new TestRulesetConfigManager();

            var bindable = manager.GetRawBindable(nameof(TestSetting.SomeValue));

            Assert.That(bindable, Is.InstanceOf<Bindable<int>>());
            Assert.That(((Bindable<int>)bindable!).Value, Is.EqualTo(5));
        }

        [Test]
        public void TestUnknownKeyReturnsNull()
        {
            using var manager = new TestRulesetConfigManager();

            Assert.That(manager.GetRawBindable("NotASetting"), Is.Null);
        }

        private enum TestSetting
        {
            SomeValue,
        }

        private class TestRulesetConfigManager : RulesetConfigManager<TestSetting>
        {
            public TestRulesetConfigManager()
                : base(null, new RulesetInfo("test", "Test", "osu.Game.Tests.NonVisual.TestRuleset", -1))
            {
            }

            protected override void InitialiseDefaults()
            {
                SetDefault(TestSetting.SomeValue, 5, 0, 10);
            }
        }
    }
}
