// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Models;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Ranking;
using osu.Game.Screens.Ranking.Expanded;
using osu.Game.Screens.Ranking.Expanded.Statistics;
using osu.Game.Tests.Beatmaps;
using osu.Game.Tests.Resources;
using osuTK;

namespace osu.Game.Tests.Visual.Ranking
{
    public partial class TestSceneExpandedPanelMiddleContent : OsuTestScene
    {
        [Resolved]
        private RulesetStore rulesetStore { get; set; }

        [Test]
        public void TestMapWithKnownMapper()
        {
            var author = new RealmUser { Username = "mapper_name" };

            AddStep("show example score", () => showPanel(TestResources.CreateTestScoreInfo(createTestBeatmap(author))));
        }

        [Test]
        public void TestExcessMods()
        {
            AddStep("show excess mods score", () =>
            {
                var author = new RealmUser { Username = "mapper_name" };

                var score = TestResources.CreateTestScoreInfo(createTestBeatmap(author));
                score.Mods = score.BeatmapInfo!.Ruleset.CreateInstance().CreateAllMods().ToArray();

                showPanel(score);
            });

            AddAssert("mapper name present", () => this.ChildrenOfType<OsuSpriteText>().Any(spriteText => spriteText.Current.Value == "mapper_name"));
        }

        [Test]
        public void TestMapWithUnknownMapper()
        {
            AddStep("show example score", () => showPanel(TestResources.CreateTestScoreInfo(createTestBeatmap(new RealmUser()))));

            AddAssert("mapped by text not present", () =>
                this.ChildrenOfType<OsuSpriteText>().All(spriteText => !containsAny(spriteText.Text.ToString(), "mapped", "by")));

            AddAssert("play time displayed", () => this.ChildrenOfType<PlayedOnText>().Any());
        }

        [Test]
        public void TestPPShownAsProvisionalWhenBeatmapHasNoLeaderboard()
        {
            AddStep("show example score", () =>
            {
                var beatmap = createTestBeatmap(new RealmUser());
                beatmap.Status = BeatmapOnlineStatus.Graveyard;
                showPanel(TestResources.CreateTestScoreInfo(beatmap));
            });

            AddUntilStep("pp display faded out", () =>
            {
                var ppDisplay = this.ChildrenOfType<PerformanceStatistic>().Single();
                return ppDisplay.Alpha == 0.5 && ppDisplay.TooltipText == ResultsScreenStrings.NoPPForUnrankedBeatmaps;
            });
        }

        [Test]
        public void TestPPShownAsProvisionalWhenUnrankedModsArePresent()
        {
            AddStep("show example score", () =>
            {
                var score = TestResources.CreateTestScoreInfo(createTestBeatmap(new RealmUser()));
                score.Mods = score.Mods.Append(new OsuModDifficultyAdjust()).ToArray();
                showPanel(score);
            });

            AddUntilStep("pp display faded out", () =>
            {
                var ppDisplay = this.ChildrenOfType<PerformanceStatistic>().Single();
                return ppDisplay.Alpha == 0.5 && ppDisplay.TooltipText == ResultsScreenStrings.NoPPForUnrankedMods;
            });
        }

        [Test]
        public void TestPPNotShownAsProvisionalIfClassicModIsPresentDueToLegacyScore()
        {
            AddStep("show example score", () =>
            {
                var score = TestResources.CreateTestScoreInfo(createTestBeatmap(new RealmUser()));
                score.PP = 400;
                score.Mods = score.Mods.Append(new OsuModClassic()).ToArray();
                score.IsLegacyScore = true;
                showPanel(score);
            });

            AddUntilStep("pp display faded out", () => this.ChildrenOfType<PerformanceStatistic>().Single().Alpha == 1);
        }

        [Test]
        public void TestWithDefaultDate()
        {
            AddStep("show autoplay score", () =>
            {
                var ruleset = new OsuRuleset();

                var mods = new Mod[] { ruleset.GetAutoplayMod() };
                var beatmap = createTestBeatmap(new RealmUser());

                var score = TestResources.CreateTestScoreInfo(beatmap);

                score.Mods = mods;
                score.Date = default;

                showPanel(score);
            });

            AddAssert("play time not displayed", () => !this.ChildrenOfType<PlayedOnText>().Any());
        }

        [Test]
        public void TestFailedSDisplay([Values] bool withFlair)
        {
            AddStep("show failed S score", () =>
            {
                var score = TestResources.CreateTestScoreInfo(createTestBeatmap(new RealmUser()));
                score.Rank = ScoreRank.A;
                score.Accuracy = 0.975;
                showPanel(score, withFlair);
            });
        }

        [TestCase(EzEnumJudgeMode.IIDX_HD, 938, 62, "1938 / 2000", "− 62", "AAA")]
        [TestCase(EzEnumJudgeMode.LR2_HD, 938, 62, "1938 / 2000", "− 62", "AAA")]
        [TestCase(EzEnumJudgeMode.Raja_NM, 777, 223, "1777 / 2000", "− 223", "AA")]
        [TestCase(EzEnumJudgeMode.Raja_NM, 778, 222, "1778 / 2000", "− 222", "AAA")]
        [TestCase(EzEnumJudgeMode.IIDX_HD, 1000, 0, "2000 / 2000", "0", "AAA")]
        public void TestIidxStatistics(EzEnumJudgeMode judgeMode, int perfect, int great, string exScore, string gap, string level)
        {
            AddStep("show Mania score", () =>
            {
                var score = TestResources.CreateTestScoreInfo(rulesetStore.GetRuleset(3));
                score.JudgeMode = (int)judgeMode;
                score.Statistics.Clear();
                score.Statistics[HitResult.Perfect] = perfect;
                score.Statistics[HitResult.Great] = great;
                score.MaximumStatistics.Clear();
                score.MaximumStatistics[HitResult.Perfect] = 1000;
                Child = new ExpandedPanelMiddleContentContainer(score, false) { Height = ScorePanel.EXPANDED_HEIGHT };
            });

            AddUntilStep("show the EX score, MAX gap and DJ level below judgements", () =>
            {
                var judgements = this.ChildrenOfType<HitResultStatistic>().ToArray();
                var playedOn = this.ChildrenOfType<PlayedOnText>().SingleOrDefault();

                if (judgements.Length == 0 || playedOn == null)
                    return false;

                float judgementBottom = judgements.Max(j => j.ScreenSpaceDrawQuad.BottomLeft.Y);
                float dateTop = playedOn.ScreenSpaceDrawQuad.TopLeft.Y;
                var row = this.ChildrenOfType<OsuSpriteText>().Where(t => t.Alpha == 1
                    && t.ScreenSpaceDrawQuad.TopLeft.Y > judgementBottom && t.ScreenSpaceDrawQuad.BottomLeft.Y < dateTop);
                return new[] { "EX SCORE", exScore, "MAX", gap, "DJ LEVEL", level }.All(value => row.Any(t => t.Text.ToString() == value));
            });
        }

        [TestCase(0, EzEnumJudgeMode.IIDX_HD, 1000, 938, 62)]
        [TestCase(3, EzEnumJudgeMode.IIDX_HD, 0, 938, 62)]
        [TestCase(3, EzEnumJudgeMode.IIDX_HD, 1000, 938, 63)]
        [TestCase(3, EzEnumJudgeMode.Lazer, 1000, 938, 62)]
        [TestCase(3, EzEnumJudgeMode.StableClassic, 1000, 938, 62)]
        [TestCase(3, EzEnumJudgeMode.EZ2AC, 1000, 938, 62)]
        [TestCase(3, EzEnumJudgeMode.O2Jam, 1000, 938, 62)]
        [TestCase(3, EzEnumJudgeMode.Malody_E, 1000, 938, 62)]
        [TestCase(3, EzEnumJudgeMode.Malody_B, 1000, 938, 62)]
        [TestCase(3, (EzEnumJudgeMode)(-1), 1000, 938, 62)]
        [TestCase(3, (EzEnumJudgeMode)99, 1000, 938, 62)]
        public void TestIidxStatisticsHidden(int rulesetId, EzEnumJudgeMode judgeMode, int maximumPerfect, int perfect, int great)
        {
            AddStep("show score without applicable EX statistics", () =>
            {
                var score = TestResources.CreateTestScoreInfo(rulesetStore.GetRuleset(rulesetId));
                score.JudgeMode = (int)judgeMode;
                score.HealthMode = (int)EzEnumHealthMode.IIDX_HD;
                score.Statistics[HitResult.Perfect] = perfect;
                score.Statistics[HitResult.Great] = great;
                score.MaximumStatistics.Clear();
                score.MaximumStatistics[HitResult.Perfect] = maximumPerfect;
                showPanel(score);
            });

            AddUntilStep("wait for the score statistics", () => this.ChildrenOfType<HitResultStatistic>().Any());
            AddAssert("hide all three EX statistics", () => !this.ChildrenOfType<OsuSpriteText>().Any(t => t.Text.ToString() is "EX SCORE" or "MAX" or "DJ LEVEL"));
        }

        private void showPanel(ScoreInfo score, bool withFlair = false) =>
            Child = new ExpandedPanelMiddleContentContainer(score, withFlair);

        private BeatmapInfo createTestBeatmap([NotNull] RealmUser author)
        {
            var beatmap = new TestBeatmap(rulesetStore.GetRuleset(0)!).BeatmapInfo;

            beatmap.Metadata.Author = author;
            beatmap.Metadata.Title = "Verrrrrrrrrrrrrrrrrrry looooooooooooooooooooooooong beatmap title";
            beatmap.Metadata.Artist = "Verrrrrrrrrrrrrrrrrrry looooooooooooooooooooooooong beatmap artist";
            beatmap.DifficultyName = "Verrrrrrrrrrrrrrrrrrry looooooooooooooooooooooooong difficulty name";

            return beatmap;
        }

        private bool containsAny(string text, params string[] stringsToMatch) => stringsToMatch.Any(text.Contains);

        private partial class ExpandedPanelMiddleContentContainer : Container
        {
            public ExpandedPanelMiddleContentContainer(ScoreInfo score, bool withFlair)
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
                Size = new Vector2(ScorePanel.EXPANDED_WIDTH, 700);
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4Extensions.FromHex("#444"),
                    },
                    new ExpandedPanelMiddleContent(score, withFlair)
                };
            }
        }
    }
}
