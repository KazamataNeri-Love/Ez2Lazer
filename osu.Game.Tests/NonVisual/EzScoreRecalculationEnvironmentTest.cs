// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Scoring;
using osu.Game.Rulesets;
using osu.Game.Scoring;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// 成绩重算的判定口径：档位按 Purpose 解析，高阶 Offset 取成绩记录值，并原样写回。
    /// </summary>
    [TestFixture]
    public class EzScoreRecalculationEnvironmentTest
    {
        private Ez2ConfigManager config = null!;

        private EzEnumJudgeMode originalJudgeMode;
        private EzEnumHealthMode originalHealthMode;
        private double originalOffsetMania;
        private double originalOffsetNonMania;

        [SetUp]
        public void SetUp()
        {
            config = GlobalConfigStore.EzConfig;
            GlobalConfigStore.EzConfig = config;

            originalJudgeMode = config.Get<EzEnumJudgeMode>(Ez2Setting.ManiaJudgeMode);
            originalHealthMode = config.Get<EzEnumHealthMode>(Ez2Setting.ManiaHealthMode);
            originalOffsetMania = config.Get<double>(Ez2Setting.OffsetPlusMania);
            originalOffsetNonMania = config.Get<double>(Ez2Setting.OffsetPlusNonMania);

            config.SetValue(Ez2Setting.ManiaJudgeMode, EzEnumJudgeMode.EZ2AC);
            config.SetValue(Ez2Setting.ManiaHealthMode, EzEnumHealthMode.Ez2Ac);
            config.SetValue(Ez2Setting.OffsetPlusMania, 12.0);
            config.SetValue(Ez2Setting.OffsetPlusNonMania, -8.0);
        }

        [TearDown]
        public void TearDown()
        {
            config.SetValue(Ez2Setting.ManiaJudgeMode, originalJudgeMode);
            config.SetValue(Ez2Setting.ManiaHealthMode, originalHealthMode);
            config.SetValue(Ez2Setting.OffsetPlusMania, originalOffsetMania);
            config.SetValue(Ez2Setting.OffsetPlusNonMania, originalOffsetNonMania);
        }

        [Test]
        public void TestStoredRecalculationUsesScoreOffsetAndEmbeddedModes()
        {
            var scoreInfo = createManiaScore();
            scoreInfo.SessionOffsetPlusMania = 15;

            var environment = EzScoreRecalculationService.ResolveRecalculationEnvironment(scoreInfo, ReplayRunPurpose.ForStored);

            Assert.That(environment.OffsetPlusMania, Is.EqualTo(15));
            Assert.That(environment.JudgeMode, Is.EqualTo(EzEnumJudgeMode.IIDX_HD), "ForStored 仍应读成绩嵌入的档位");
        }

        [Test]
        public void TestLiveRecalculationUsesScoreOffsetAndLiveModes()
        {
            var scoreInfo = createManiaScore();
            scoreInfo.SessionOffsetPlusMania = 15;

            var environment = EzScoreRecalculationService.ResolveRecalculationEnvironment(scoreInfo, ReplayRunPurpose.ForLive);

            Assert.That(environment.OffsetPlusMania, Is.EqualTo(15), "重算不得改用当前全局 offset");
            Assert.That(environment.JudgeMode, Is.EqualTo(EzEnumJudgeMode.EZ2AC), "ForLive 仍应读当前档位");
        }

        [Test]
        public void TestRecalculationDoesNotInheritGlobalOffsetWhenScoreHasNone()
        {
            var scoreInfo = createManiaScore();

            var environment = EzScoreRecalculationService.ResolveRecalculationEnvironment(scoreInfo, ReplayRunPurpose.ForLive);

            Assert.That(environment.OffsetPlusMania, Is.EqualTo(0));
            Assert.That(environment.OffsetPlusNonMania, Is.EqualTo(0));
        }

        [Test]
        public void TestRecalculationWritesUsedOffsetBack()
        {
            var scoreInfo = createManiaScore();
            scoreInfo.SessionOffsetPlusMania = 1;

            var sessionInfo = createManiaScore();
            var environment = EzScoreRecalculationService.ResolveRecalculationEnvironment(scoreInfo, ReplayRunPurpose.ForLive);

            ScoreManager.ApplyEzSessionRecalculationToDetachedScoreInfo(scoreInfo, sessionInfo, ReplayRunPurpose.ForLive, environment);

            Assert.That(scoreInfo.SessionOffsetPlusMania, Is.EqualTo(environment.OffsetPlusMania));
            Assert.That(scoreInfo.SessionOffsetPlusNonMania, Is.EqualTo(environment.OffsetPlusNonMania));
        }

        private static ScoreInfo createManiaScore() => new ScoreInfo
        {
            Ruleset = new RulesetInfo { OnlineID = 3, ShortName = @"mania" },
            JudgeMode = (int)EzEnumJudgeMode.IIDX_HD,
            HealthMode = (int)EzEnumHealthMode.IIDX_HD,
        };
    }
}
