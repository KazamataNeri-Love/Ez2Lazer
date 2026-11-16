// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using osu.Game.Rulesets.UI.Scrolling.Algorithms;

namespace osu.Game.Tests.ScrollAlgorithms
{
    [TestFixture]
    public class PositionDependentScrollTest
    {
        private const double time_range = 1000;
        private const float scroll_length = 500;

        [Test]
        public void TestBuiltInAlgorithmsAreTranslationInvariant()
        {
            IScrollAlgorithm algorithm = new ConstantScrollAlgorithm();
            ClassicAssert.IsFalse(algorithm.IsPositionDependent);
        }

        [Test]
        public void TestConstantDifferenceMatchesLengthAtAnyTime()
        {
            var algorithm = new ConstantScrollAlgorithm();
            float length = algorithm.GetLength(100, 300, time_range, scroll_length);

            foreach (double currentTime in new[] { 0.0, 250.0, 900.0 })
                ClassicAssert.AreEqual(length, positionDifference(algorithm, 100, 300, currentTime), 1e-4f);
        }

        [Test]
        public void TestRemappedAlgorithmReportsPositionDependence()
        {
            IScrollAlgorithm algorithm = new RemappingScrollAlgorithm();

            ClassicAssert.IsTrue(algorithm.IsPositionDependent);

            // The length measured at the origin no longer describes the object at the current time.
            float originLength = algorithm.GetLength(0, 100, time_range, scroll_length);
            float currentLength = positionDifference(algorithm, 0, 100, 0);

            ClassicAssert.Greater(originLength - currentLength, 1f);
        }

        private static float positionDifference(IScrollAlgorithm algorithm, double startTime, double endTime, double currentTime)
        {
            float start = algorithm.PositionAt(startTime, currentTime, time_range, scroll_length);
            float end = algorithm.PositionAt(endTime, currentTime, time_range, scroll_length);
            return Math.Abs(end - start);
        }

        /// <summary>
        /// A stand-in for a position-dependent algorithm: it squares the normalised position inside the
        /// container and extends linearly outside, keeping the endpoints intact.
        /// </summary>
        private class RemappingScrollAlgorithm : IScrollAlgorithm
        {
            private readonly ConstantScrollAlgorithm inner = new ConstantScrollAlgorithm();

            public bool IsPositionDependent => true;

            public double GetDisplayStartTime(double originTime, float offset, double timeRange, float scrollLength)
                => inner.GetDisplayStartTime(originTime, offset, timeRange, scrollLength);

            public float GetLength(double startTime, double endTime, double timeRange, float scrollLength)
                => inner.GetLength(startTime, endTime, timeRange, scrollLength);

            public float PositionAt(double time, double currentTime, double timeRange, float scrollLength, double? originTime = null)
            {
                float position = inner.PositionAt(time, currentTime, timeRange, scrollLength, originTime);
                float normalised = position / scrollLength;

                if (normalised < 0 || normalised > 1)
                    return position;

                return normalised * normalised * scrollLength;
            }

            public double TimeAt(float position, double currentTime, double timeRange, float scrollLength)
                => throw new NotSupportedException();

            public void Reset()
            {
            }
        }
    }
}
