// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Utils;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.UI;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests
{
    /// <summary>
    /// 棱锥几何的契约：比例只由深度系数决定，且 <c>1/ratio</c> 随进度线性。
    /// 后者是「匀速深度」的唯一判据，任何偏离都会让透视退化成手工缩放。
    /// </summary>
    public class PolytoneGeometryTest
    {
        private const float view_angle = 30;
        private const float spawn_depth = 1665;

        private static PolytoneGeometry create() => new PolytoneGeometry(view_angle, spawn_depth);

        private static float expectedDepthScale()
            => spawn_depth / (OsuPlayfield.BASE_SIZE.X * 0.5f / MathF.Tan(MathHelper.DegreesToRadians(view_angle)));

        [Test]
        public void TestDepthScaleDerivedFromViewAngle()
        {
            Assert.That(create().DepthScale, Is.EqualTo(expectedDepthScale()).Within(1e-5f));
        }

        [Test]
        public void TestSpawnRatioIsOriginOfCurve()
        {
            var geometry = create();
            Assert.That(geometry.SpawnRatio, Is.EqualTo(geometry.RatioAt(0)).Within(1e-6f));
            Assert.That(geometry.SpawnRatio, Is.EqualTo(1 / (1 + geometry.DepthScale)).Within(1e-6f));
        }

        [Test]
        public void TestHitPlaneIsIdentity()
        {
            Assert.That(create().RatioAt(1), Is.EqualTo(1f).Within(1e-6f));
        }

        [Test]
        public void TestReciprocalRatioIsLinearInProgress()
        {
            var geometry = create();

            for (int i = 0; i <= 10; i++)
            {
                double u = i / 10.0;
                float expected = 1 + geometry.DepthScale * (float)(1 - u);
                Assert.That(1 / geometry.RatioAt(u), Is.EqualTo(expected).Within(1e-4f));
            }
        }

        [Test]
        public void TestRatioIsMonotonicInProgress()
        {
            var geometry = create();
            float previous = 0;

            for (int i = 0; i <= 10; i++)
            {
                float ratio = geometry.RatioAt(i / 10.0);
                Assert.That(ratio, Is.GreaterThan(previous));
                previous = ratio;
            }
        }

        [Test]
        public void TestProjectIsRadialAboutVanishingPoint()
        {
            var geometry = create();
            var position = new Vector2(420, 88);
            Vector2 projected = geometry.Project(position, 0.25f);
            Vector2 expected = geometry.VanishingPoint + (position - geometry.VanishingPoint) * 0.25f;

            Assert.That(projected.X, Is.EqualTo(expected.X).Within(1e-4f));
            Assert.That(projected.Y, Is.EqualTo(expected.Y).Within(1e-4f));
        }

        [Test]
        public void TestProgressIsClamped()
        {
            var geometry = create();
            Assert.That(geometry.RatioAt(-5), Is.EqualTo(geometry.SpawnRatio));
            Assert.That(geometry.RatioAt(5), Is.EqualTo(1f));
        }
    }
}
