// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.
//
// Neri_Edge: geometric six-point snowflake particles replacing TrianglesV2
// in the OsuLogo. Each snowflake has six straight, equal-width branches
// radiating at 60° intervals. Each branch splits into two symmetrical
// sub-branches at ~2/3 length (45° angle). All ends are flat rectangles.
// Particles fall downward from above the visible area, without rotation.

#nullable disable

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Utils;
using osuTK;

namespace osu.Game.Graphics.Backgrounds
{
    public partial class SnowflakesV2 : Drawable
    {
        private const float base_velocity = 50;

        // ── Snowflake geometry (relative to DrawSize) ──
        private const float branch_length = 0.045f;   // main branch length
        private const float branch_width  = 0.010f;   // branch width (constant)
        private const float sub_length    = 0.028f;   // sub-branch length
        private const float sub_angle_deg = 45f;       // sub-branch angle from main

        public float Velocity = 1;

        public float SpawnRatio
        {
            get => spawnRatio.Value;
            set => spawnRatio.Value = value;
        }

        public float ScaleAdjust { get; set; } = 1;

        /// <summary>No-op, kept for API compatibility.</summary>
        public float Thickness { get; set; }

        protected virtual bool CreateNewSnowflakes => true;

        private readonly BindableFloat spawnRatio = new BindableFloat(1f);
        private readonly List<SnowflakeParticle> parts = new List<SnowflakeParticle>();
        private Random stableRandom;

        // ── Precomputed branch geometry ──
        private static readonly BranchDef[] branch_defs;
        private const float sub_rad = sub_angle_deg * MathF.PI / 180f;

        static SnowflakesV2()
        {
            var defs = new List<BranchDef>();
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.PI / 3f;
                float c = MathF.Cos(a), s = MathF.Sin(a);
                Vector2 tip = new Vector2(c, s);

                // Main branch: center → tip
                defs.Add(new BranchDef(Vector2.Zero, tip));

                // Split point at 2/3 of main
                Vector2 split = tip * 0.667f;

                // Sub-branches: ±45° from main
                float ca1 = MathF.Cos(a + sub_rad), sa1 = MathF.Sin(a + sub_rad);
                defs.Add(new BranchDef(split, split + new Vector2(ca1, sa1) * sub_length / branch_length));

                float ca2 = MathF.Cos(a - sub_rad), sa2 = MathF.Sin(a - sub_rad);
                defs.Add(new BranchDef(split, split + new Vector2(ca2, sa2) * sub_length / branch_length));
            }
            branch_defs = defs.ToArray();
        }

        private readonly struct BranchDef
        {
            public readonly Vector2 Start;
            public readonly Vector2 End;
            public BranchDef(Vector2 start, Vector2 end) { Start = start; End = end; }
        }

        public SnowflakesV2(int? seed = null)
        {
            if (seed != null) stableRandom = new Random(seed.Value);
        }

        private Texture texture;
        private IShader shader;

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders, IRenderer renderer)
        {
            shader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            texture = renderer.WhitePixel;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            spawnRatio.BindValueChanged(_ => Reset(), true);
        }

        protected override void Update()
        {
            base.Update();
            if (CreateNewSnowflakes) addSnowflakes(false);

            float dt = (float)Time.Elapsed / 1000;
            if (dt == 0) return;

            // ── Falling downward (positive Y) ──
            float moveY = dt * Velocity * base_velocity / DrawHeight;

            for (int i = parts.Count - 1; i >= 0; i--)
            {
                var p = parts[i];
                p.Position.Y += moveY * Math.Max(0.5f, p.SpeedMultiplier);
                parts[i] = p;

                // Remove when fully below the visible area
                if (p.Position.Y > 1.2f)
                    parts.RemoveAt(i);
            }

            Invalidate(Invalidation.DrawNode);
        }

        public void Reset(int? seed = null)
        {
            if (seed != null) stableRandom = new Random(seed.Value);
            parts.Clear();
            addSnowflakes(true);
        }

        protected int AimCount { get; private set; }

        private void addSnowflakes(bool randomY)
        {
            const int max_particles = ushort.MaxValue / 120;
            AimCount = (int)Math.Clamp(DrawWidth * 0.02f * SpawnRatio, 1, max_particles);

            int current = parts.Count;
            if (AimCount - current == 0) return;

            for (int i = 0; i < AimCount - current; i++)
                parts.Add(createParticle(randomY));

            Invalidate(Invalidation.DrawNode);
        }

        private SnowflakeParticle createParticle(bool randomY)
        {
            float y = -0.15f; // spawn above the visible area, falling down
            if (randomY)
            {
                // Spread existing particles across the full height on init
                y = -0.15f + nextRandom() * 1.35f;
            }

            float u1 = 1 - nextRandom();
            float u2 = 1 - nextRandom();
            float speed = 0.5f + 0.16f * (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2));
            if (speed < 0.1f) speed = 0.1f;

            return new SnowflakeParticle
            {
                Position = new Vector2(nextRandom(), y),
                SpeedMultiplier = speed,
                Scale = 0.5f + nextRandom() * 0.8f,
            };
        }

        private float nextRandom() => (float)(stableRandom?.NextDouble() ?? RNG.NextSingle());

        protected override DrawNode CreateDrawNode() => new SnowflakeDrawNode(this);

        private struct SnowflakeParticle
        {
            public Vector2 Position;
            public float SpeedMultiplier;
            public float Scale;
        }

        private class SnowflakeDrawNode : DrawNode
        {
            protected new SnowflakesV2 Source => (SnowflakesV2)base.Source;

            private Texture texture;
            private IShader shader;
            private Vector2 drawSize;
            private ColourInfo colour;
            private readonly List<SnowflakeParticle> particles = new List<SnowflakeParticle>();
            private IVertexBatch<TexturedVertex2D> vertexBatch;

            public SnowflakeDrawNode(SnowflakesV2 source) : base(source) { }

            public override void ApplyState()
            {
                base.ApplyState();
                shader = Source.shader;
                texture = Source.texture;
                drawSize = Source.DrawSize;
                colour = Source.DrawColourInfo.Colour;
                particles.Clear();
                particles.AddRange(Source.parts);
            }

            protected override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);
                if (particles.Count == 0 || !texture.Available) return;

                int maxQuads = particles.Count * 18;
                vertexBatch ??= renderer.CreateQuadBatch<TexturedVertex2D>(maxQuads, 1);

                shader.Bind();

                float bl = branch_length * Source.ScaleAdjust;
                float bw = branch_width * Source.ScaleAdjust;
                Vector2 size = drawSize;

                foreach (var p in particles)
                {
                    float sc = p.Scale;

                    foreach (var def in branch_defs)
                    {
                        // Branch start/end in relative coordinates (no rotation)
                        Vector2 startRel = p.Position + def.Start * sc * bl;
                        Vector2 endRel   = p.Position + def.End   * sc * bl;

                        drawFlatBranch(renderer, startRel, endRel, bw * sc, size);
                    }
                }

                shader.Unbind();
                vertexBatch.Draw();
            }

            private void drawFlatBranch(IRenderer renderer, Vector2 start, Vector2 end, float w, Vector2 size)
            {
                Vector2 dir = end - start;
                float len = dir.Length;
                if (len < 0.0001f) return;
                dir /= len;

                Vector2 perp = new Vector2(-dir.Y, dir.X) * w * 0.5f;

                // Flat rectangular branch: just a quad, no semicircle cap
                var relQuad = new Quad(
                    start - perp,  // TL
                    start + perp,  // TR
                    end   + perp,  // BR
                    end   - perp   // BL
                );

                var screenQuad = new Quad(
                    Vector2Extensions.Transform((start - perp) * size, DrawInfo.Matrix),
                    Vector2Extensions.Transform((start + perp) * size, DrawInfo.Matrix),
                    Vector2Extensions.Transform((end   + perp) * size, DrawInfo.Matrix),
                    Vector2Extensions.Transform((end   - perp) * size, DrawInfo.Matrix)
                );

                renderer.DrawQuad(texture, screenQuad, colour.Interpolate(relQuad), null, vertexBatch.AddAction, Vector2.One);
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);
                vertexBatch?.Dispose();
            }
        }
    }
}
