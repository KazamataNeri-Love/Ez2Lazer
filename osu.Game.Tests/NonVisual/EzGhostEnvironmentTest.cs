// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.Scoring;
using osu.Game.Rulesets;
using osu.Game.Scoring;
using Realms;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// 角逐 ghost 的判定环境：档位取当前设置，高阶 Offset 取成绩自身记录值。
    /// </summary>
    [TestFixture]
    public class EzGhostEnvironmentTest
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
        public void TestOffsetComesFromScoreNotGlobalSettings()
        {
            var scoreInfo = createManiaScore();
            scoreInfo.SessionOffsetPlusMania = 25;
            scoreInfo.SessionOffsetPlusNonMania = 25;

            var environment = EzScoreTimelineBuilder.ResolveGhostEnvironment(scoreInfo);

            Assert.That(environment.OffsetPlusMania, Is.EqualTo(25));
            Assert.That(environment.OffsetPlusNonMania, Is.EqualTo(25));
        }

        [Test]
        public void TestScoreWithoutRecordedOffsetIgnoresGlobalOffset()
        {
            var scoreInfo = createManiaScore();

            var environment = EzScoreTimelineBuilder.ResolveGhostEnvironment(scoreInfo);
            var legacy = GlobalConfigStore.EzConfig.ResolveEnvironment(ReplayRunPurpose.ForLive, scoreInfo, ignoreOffset: true);

            // 全局设置里 offset 是 12 / -8；成绩没记录时必须仍是 0，且与改动前的解析结果逐字段相同。
            Assert.That(environment, Is.EqualTo(legacy));
            Assert.That(environment.OffsetPlusMania, Is.EqualTo(0));
            Assert.That(environment.OffsetPlusNonMania, Is.EqualTo(0));
        }

        [Test]
        public void TestJudgeModeComesFromLiveSettingsNotScore()
        {
            var scoreInfo = createManiaScore();
            scoreInfo.JudgeMode = (int)EzEnumJudgeMode.StableClassic;
            scoreInfo.HealthMode = (int)EzEnumHealthMode.Lazer;

            var environment = EzScoreTimelineBuilder.ResolveGhostEnvironment(scoreInfo);

            Assert.That(environment.JudgeMode, Is.EqualTo(EzEnumJudgeMode.EZ2AC));
            Assert.That(environment.HealthMode, Is.EqualTo(EzEnumHealthMode.Ez2Ac));
        }

        [Test]
        public void TestNonManiaOffsetComesFromScore()
        {
            var scoreInfo = new ScoreInfo { Ruleset = new RulesetInfo { OnlineID = 0, ShortName = @"osu" } };
            scoreInfo.SessionOffsetPlusNonMania = -7.5;

            var environment = EzScoreTimelineBuilder.ResolveGhostEnvironment(scoreInfo);

            Assert.That(environment.OffsetPlusNonMania, Is.EqualTo(-7.5));
            Assert.That(environment.OffsetPlusMania, Is.EqualTo(0));
        }

        [Test]
        public void TestOffsetColumnsArePersisted()
        {
            foreach (string name in new[] { nameof(ScoreInfo.SessionOffsetPlusMania), nameof(ScoreInfo.SessionOffsetPlusNonMania) })
            {
                var property = typeof(ScoreInfo).GetProperty(name);

                Assert.That(property, Is.Not.Null, $"{name} 不存在");
                Assert.That(property!.GetCustomAttributes(typeof(IgnoredAttribute), false), Is.Empty,
                    $"{name} 必须落盘，否则角逐 ghost 读回的成绩 offset 恒为 0");
            }
        }

        private static ScoreInfo createManiaScore() => new ScoreInfo
        {
            Ruleset = new RulesetInfo { OnlineID = 3, ShortName = @"mania" },
        };
    }
}
