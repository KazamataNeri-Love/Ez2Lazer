// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Graphics.Textures;
using osu.Framework.Text;

namespace osu.Game.EzOsuGame.HUD
{
    /// <summary>
    /// 按主题与字符查找 EzResources 里的字形纹理，带按字符的缓存。
    /// 文本渲染（<see cref="EzSpriteText"/>）与逐字符渲染共用同一份路径规则与缓存口径。
    /// </summary>
    public class EzGlyphStore : ITexturedGlyphLookupStore
    {
        /// <summary>
        /// 度量先缩到纹理的 1/8，字体大小再决定绘制大小（与 Argon 位图字体一致）。
        /// </summary>
        public const float GLYPH_METRIC_SCALE = 0.125f;

        private readonly EzResourceStore resources;
        private readonly Func<char, string> getLookup;
        private readonly Func<string, string, char, string[]> getPathResolver;

        private readonly Dictionary<char, ITexturedCharacterGlyph?> cache = new Dictionary<char, ITexturedCharacterGlyph?>();
        private readonly Lock cacheLock = new Lock();

        public EzGlyphStore(EzResourceStore resources, Func<char, string> getLookup, Func<string, string, char, string[]> getPathResolver)
        {
            this.resources = resources;
            this.getLookup = getLookup;
            this.getPathResolver = getPathResolver;
        }

        public ITexturedCharacterGlyph? Get(string? textureName, char character)
        {
            lock (cacheLock)
            {
                if (cache.TryGetValue(character, out var cached))
                    return cached;
            }

            string lookup = getLookup(character);
            TexturedCharacterGlyph? glyph = null;

            if (textureName != null)
            {
                string themeName = textureName.Replace(" ", "_");
                string[] possiblePaths = getPathResolver(themeName, lookup, character);
                Texture? texture = resources.GetGlyph(possiblePaths);

                if (texture != null)
                {
                    glyph = new TexturedCharacterGlyph(
                        new CharacterGlyph(character, 0, 0, texture.Width, texture.Height, null),
                        texture,
                        GLYPH_METRIC_SCALE);
                }
            }

            lock (cacheLock)
            {
                if (cache.TryGetValue(character, out var cached))
                    return cached;

                cache[character] = glyph;
                return glyph;
            }
        }

        public Task<ITexturedCharacterGlyph?> GetAsync(string fontName, char character)
            => Task.Run(() => Get(fontName, character));

        public void ClearCache()
        {
            lock (cacheLock)
                cache.Clear();
        }
    }
}
