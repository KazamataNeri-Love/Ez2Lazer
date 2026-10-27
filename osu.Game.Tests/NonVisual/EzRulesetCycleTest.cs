// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.EzOsuGame.Input;
using osu.Game.Rulesets;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class EzRulesetCycleTest
    {
        private static RulesetInfo ruleset(string shortName) => new RulesetInfo(shortName, shortName, string.Empty, -1);

        private static readonly List<RulesetInfo> four_rulesets = new List<RulesetInfo>
        {
            ruleset("osu"),
            ruleset("taiko"),
            ruleset("catch"),
            ruleset("mania"),
        };

        [Test]
        public void CyclesThroughAllRulesetsInOrder()
        {
            Assert.That(EzRulesetCycleHelper.NextRuleset(four_rulesets, four_rulesets[0])?.ShortName, Is.EqualTo("taiko"));
            Assert.That(EzRulesetCycleHelper.NextRuleset(four_rulesets, four_rulesets[1])?.ShortName, Is.EqualTo("catch"));
            Assert.That(EzRulesetCycleHelper.NextRuleset(four_rulesets, four_rulesets[2])?.ShortName, Is.EqualTo("mania"));
        }

        [Test]
        public void LastRulesetWrapsBackToFirst()
        {
            Assert.That(EzRulesetCycleHelper.NextRuleset(four_rulesets, four_rulesets[3])?.ShortName, Is.EqualTo("osu"));
        }

        [Test]
        public void UnknownOrUnsetCurrentGoesToFirst()
        {
            Assert.That(EzRulesetCycleHelper.NextRuleset(four_rulesets, null)?.ShortName, Is.EqualTo("osu"));
            Assert.That(EzRulesetCycleHelper.NextRuleset(four_rulesets, ruleset("bms"))?.ShortName, Is.EqualTo("osu"));
        }

        [Test]
        public void SingleRulesetStaysOnItself()
        {
            var list = new List<RulesetInfo> { ruleset("osu") };

            Assert.That(EzRulesetCycleHelper.NextRuleset(list, list[0])?.ShortName, Is.EqualTo("osu"));
        }

        [Test]
        public void EmptyListReturnsNull()
        {
            Assert.That(EzRulesetCycleHelper.NextRuleset(new List<RulesetInfo>(), ruleset("osu")), Is.Null);
        }
    }
}
