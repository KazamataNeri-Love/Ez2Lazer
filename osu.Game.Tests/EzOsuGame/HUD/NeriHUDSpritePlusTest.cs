// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.EzOsuGame.HUD;

namespace osu.Game.Tests.EzOsuGame.HUD
{
    [TestFixture]
    public class NeriHUDSpritePlusTest
    {
        [Test]
        public void TestResolveFirstTimingPointTime()
        {
            Assert.That(NeriHUDSpritePlus.ResolveFirstTimingPointTime(null), Is.EqualTo(0));
            Assert.That(NeriHUDSpritePlus.ResolveFirstTimingPointTime(new Beatmap()), Is.EqualTo(0));

            var beatmap = new Beatmap();
            beatmap.ControlPointInfo.Add(5000, new TimingControlPoint { BeatLength = 400 });
            beatmap.ControlPointInfo.Add(1500, new TimingControlPoint { BeatLength = 500 });

            Assert.That(NeriHUDSpritePlus.ResolveFirstTimingPointTime(beatmap), Is.EqualTo(1500));
        }

        [Test]
        public void TestBeforeFirstTimingPointResetsToFrameZero()
        {
            double playback = NeriHUDSpritePlus.AdvanceGameplayPlayback(
                currentPlaybackTime: 120,
                songTime: 1499,
                previousSongTime: 1480,
                elapsedRealTime: 16,
                firstTimingPointTime: 1500,
                isPaused: false,
                useBeatmapBpm: true,
                beatDivision: 4,
                beatLength: 500,
                fps: 60);

            Assert.That(playback, Is.EqualTo(0));
        }

        [Test]
        public void TestCrossingOffsetOnlyAdvancesPostOffsetPortion()
        {
            // First timing point at 1500ms; frame goes from 1495ms to 1511ms => only 11ms after offset.
            double playback = NeriHUDSpritePlus.AdvanceGameplayPlayback(
                currentPlaybackTime: 0,
                songTime: 1511,
                previousSongTime: 1495,
                elapsedRealTime: 16,
                firstTimingPointTime: 1500,
                isPaused: false,
                useBeatmapBpm: true,
                beatDivision: 4,
                beatLength: 500,
                fps: 60);

            double expected = 11.0 * (NeriHUDSpritePlus.BASE_FRAME_LENGTH * 4 / 500.0);
            Assert.That(playback, Is.EqualTo(expected).Within(0.0001));
        }

        [Test]
        public void TestPausedGameplayFreezesPlayback()
        {
            double bpmPlayback = NeriHUDSpritePlus.AdvanceGameplayPlayback(
                currentPlaybackTime: 85.5,
                songTime: 3000,
                previousSongTime: 2984,
                elapsedRealTime: 16,
                firstTimingPointTime: 1500,
                isPaused: true,
                useBeatmapBpm: true,
                beatDivision: 4,
                beatLength: 500,
                fps: 60);

            double fpsPlayback = NeriHUDSpritePlus.AdvanceGameplayPlayback(
                currentPlaybackTime: 85.5,
                songTime: 3000,
                previousSongTime: 2984,
                elapsedRealTime: 16,
                firstTimingPointTime: 1500,
                isPaused: true,
                useBeatmapBpm: false,
                beatDivision: 4,
                beatLength: 500,
                fps: 60);

            Assert.That(bpmPlayback, Is.EqualTo(85.5));
            Assert.That(fpsPlayback, Is.EqualTo(85.5));
        }

        [Test]
        public void TestFpsModeAdvancesWhenRunningAfterOffset()
        {
            double playback = NeriHUDSpritePlus.AdvanceGameplayPlayback(
                currentPlaybackTime: 10,
                songTime: 2016,
                previousSongTime: 2000,
                elapsedRealTime: 20,
                firstTimingPointTime: 1500,
                isPaused: false,
                useBeatmapBpm: false,
                beatDivision: 4,
                beatLength: 500,
                fps: 30);

            double expected = 10 + 20.0 * (30 * NeriHUDSpritePlus.BASE_FRAME_LENGTH / 1000.0);
            Assert.That(playback, Is.EqualTo(expected).Within(0.0001));
        }

        [Test]
        public void TestResolveFallbackFrameKeysSupportsTwoDigitZeroPadded()
        {
            string[] keys =
            {
                "Modify/Tachie/xxx-00",
                "Modify/Tachie/xxx-01",
                "Modify/Tachie/xxx-02",
                "Modify/Tachie/xxx-09",
                "Modify/Tachie/xxx-10",
                "Modify/Tachie/other-00",
            };

            // 00..02 contiguous, gap before 09 stops cleanly.
            var contiguous = NeriHUDSpritePlus.ResolveFallbackFrameKeys(keys, "xxx-", "{00}");
            Assert.That(contiguous, Is.EqualTo(new[]
            {
                "Modify/Tachie/xxx-00",
                "Modify/Tachie/xxx-01",
                "Modify/Tachie/xxx-02",
            }));
        }

        [Test]
        public void TestResolveFallbackFrameKeysSupportsUnseparatedAndOneBasedAndSeparatorTemplate()
        {
            string[] noSeparatorKeys =
            {
                "Modify/Tachie/foo0",
                "Modify/Tachie/foo1",
                "Modify/Tachie/foo2",
                "Modify/Tachie/foo10",
            };

            var noSepResult = NeriHUDSpritePlus.ResolveFallbackFrameKeys(noSeparatorKeys, "foo", "{0}");
            Assert.That(noSepResult, Is.EqualTo(new[]
            {
                "Modify/Tachie/foo0",
                "Modify/Tachie/foo1",
                "Modify/Tachie/foo2",
            }));

            string[] oneBasedJpeg =
            {
                "Modify/Tachie/bar-01.jpeg",
                "Modify/Tachie/bar-02.jpeg",
                "Modify/Tachie/bar-03.jpeg",
            };

            var oneBasedResult = NeriHUDSpritePlus.ResolveFallbackFrameKeys(oneBasedJpeg, "bar", "-{00}");
            Assert.That(oneBasedResult, Is.EqualTo(new[]
            {
                "Modify/Tachie/bar-01.jpeg",
                "Modify/Tachie/bar-02.jpeg",
                "Modify/Tachie/bar-03.jpeg",
            }));
        }
    }
}
