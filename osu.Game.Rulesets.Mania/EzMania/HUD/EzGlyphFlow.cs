// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Localisation;
using osu.Game.EzOsuGame;
using osu.Game.EzOsuGame.Configuration;
using osu.Game.EzOsuGame.HUD;
using osu.Game.EzOsuGame.WarmUp;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Mania.EzMania.HUD
{
    /// <summary>
    /// 逐字符渲染的数字流。静止为水平排布，触发时整体缩放并让字符沿对称斜线、按限定宽度展开。
    /// </summary>
    /// <remarks>
    /// 字符尺寸按「整串最高字形缩放到 <see cref="GLYPH_TARGET_HEIGHT"/>」统一换算，与文本渲染的计数器同高。
    /// 逐字符位移按序号对称均分，最高位与最低位的绝对偏移等于设定值；位移按时间进度逐帧写入，
    /// 布局重排不会吃掉正在播放的位移。缩放锚点取外部传入的锚位置，使皮肤的 Origin 设置直接决定放大方向。
    /// </remarks>
    public partial class EzGlyphFlow : CompositeDrawable, IHasText, IEzGameplayWarmUp
    {
        internal const float GLYPH_TARGET_HEIGHT = 25;

        public Bindable<string> ThemeName { get; }

        /// <summary>模板动画与内置动效的作用目标。</summary>
        public Container TextContainer { get; }

        /// <summary>模板动画的源图像素位移折算比例（整串字形高 ≠ 源图高时需要）。</summary>
        public float SourcePixelScale => referenceHeight > 0 ? GLYPH_TARGET_HEIGHT / referenceHeight : 1;

        public LocalisableString Text
        {
            get => text;
            set => setText(value.ToString());
        }

        private string text = string.Empty;

        private Colour4 glyphColour = Colour4.White;

        private readonly List<Sprite> glyphs = new List<Sprite>();

        private readonly List<Vector2> homePositions = new List<Vector2>();

        private readonly Stack<Sprite> pool = new Stack<Sprite>();

        private readonly Dictionary<char, Texture?> textures = new Dictionary<char, Texture?>();

        private EzGlyphStore? store;

        private float referenceHeight = 1;

        private bool motionActive;

        private double motionStartTime;

        private double motionForwardDuration;

        private double motionReturnDuration;

        private float motionPeakScale = 1;

        private float motionTilt;

        private float motionExpand;

        private float motionShift;

        private float motionShiftY;

        private float motionProgress;

        [Resolved]
        private EzResourceStore resources { get; set; } = null!;

        [Resolved]
        private Ez2ConfigManager ezConfig { get; set; } = null!;

        [Resolved]
        private EzGameplayWarmUpService warmUpService { get; set; } = null!;

        public EzGlyphFlow(Bindable<string>? externalThemeName = null)
        {
            AutoSizeAxes = Axes.None;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            ThemeName = externalThemeName ?? new Bindable<string>(EzSelectorEnumList.DEFAULT_NAME);

            InternalChild = TextContainer = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            store = new EzGlyphStore(resources, c => c.ToString(), getPossiblePaths);

            ThemeName.BindValueChanged(_ => onThemeChanged());
            ezConfig.GetBindable<string>(Ez2Setting.GameThemeName).BindValueChanged(e => ThemeName.Value = EzGameThemeNames.Normalize(e.NewValue));

            warmUpService.Register(this);

            updateGlyphs();
        }

        protected override void Dispose(bool isDisposing)
        {
            warmUpService?.Unregister(this);
            base.Dispose(isDisposing);
        }

        public void SetColour(Colour4 colour)
        {
            glyphColour = colour;

            foreach (Sprite glyph in glyphs)
                glyph.Colour = colour;
        }

        /// <summary>缩放锚点。与组件的 Origin 同步后，放大方向由皮肤设置决定。</summary>
        public void SetEffectOrigin(Anchor origin)
        {
            if (TextContainer.Anchor == origin && TextContainer.Origin == origin)
                return;

            TextContainer.Anchor = origin;
            TextContainer.Origin = origin;
            TextContainer.Position = Vector2.Zero;
        }

        /// <summary>
        /// 复位到静止布局。模板动画播放前也通过它清掉上一段位移。
        /// </summary>
        public void ResetMotion()
        {
            motionActive = false;
            motionProgress = 0;

            TextContainer.FinishTransforms();
            TextContainer.Scale = Vector2.One;
            TextContainer.Position = Vector2.Zero;

            layoutGlyphs();
        }

        /// <summary>
        /// 播一次复合动效：整体缩放到峰值，同时逐字符沿对称斜线、按限定宽度展开；全部字符同一时间轴。
        /// </summary>
        /// <param name="tiltHeight">最高位与最低位的绝对竖直偏移，单位为字高。</param>
        /// <param name="expandWidth">最高位与最低位的绝对水平偏移，单位为字高。</param>
        /// <param name="shiftWidth">整体水平位移，单位为字高，正值向右。</param>
        /// <param name="shiftHeight">整体竖直位移，单位为字高，正值向下；仅当锚点在顶部或底部时才生效。</param>
        public void PlayComposite(bool wasIncrease, bool wasMiss, float peakScale, float tiltHeight, float expandWidth, float shiftWidth, float shiftHeight, double forwardDuration, double returnDuration)
        {
            ResetMotion();

            if (!wasIncrease)
            {
                if (wasMiss)
                    TextContainer.FlashColour(Color4.Red, returnDuration, Easing.OutQuint);

                return;
            }

            motionPeakScale = peakScale;
            motionTilt = Math.Abs(tiltHeight) * GLYPH_TARGET_HEIGHT;
            motionExpand = expandWidth * GLYPH_TARGET_HEIGHT;
            motionShift = shiftWidth * GLYPH_TARGET_HEIGHT;
            motionShiftY = shiftHeight * GLYPH_TARGET_HEIGHT;
            motionForwardDuration = forwardDuration;
            motionReturnDuration = returnDuration;
            motionStartTime = Time.Current;
            motionProgress = 0;
            motionActive = true;
        }

        public Task WarmUpAsync(CancellationToken cancellationToken)
        {
            EzGlyphStore? localStore = store;

            if (localStore == null)
                return Task.CompletedTask;

            string themeName = EzGameThemeNames.Normalize(ThemeName.Value);

            return EzGameplayWarmUpService.RunBlocking(() =>
            {
                var uploads = new List<Texture>();

                for (int i = 0; i < 10; i++)
                {
                    Texture? texture = localStore.Get(themeName, (char)('0' + i))?.Texture;

                    if (texture != null)
                        uploads.Add(texture);
                }

                EzGameplayWarmUpService.WaitForUpload(uploads, cancellationToken);
            }, cancellationToken);
        }

        protected override void Update()
        {
            base.Update();

            if (!motionActive)
                return;

            double elapsed = Time.Current - motionStartTime;
            double total = motionForwardDuration + motionReturnDuration;

            if (elapsed >= total)
            {
                motionActive = false;
                motionProgress = 0;
            }
            else if (elapsed < motionForwardDuration)
            {
                motionProgress = motionForwardDuration <= 0 ? 1 : (float)(elapsed / motionForwardDuration);
            }
            else
            {
                motionProgress = motionReturnDuration <= 0 ? 0 : 1 - (float)((elapsed - motionForwardDuration) / motionReturnDuration);
            }

            applyMotion();
        }

        private void applyMotion()
        {
            float scale = 1 + (motionPeakScale - 1) * motionProgress;

            TextContainer.Scale = new Vector2(scale);

            int count = Math.Min(glyphs.Count, homePositions.Count);

            if (count == 0)
                return;

            float compensation = scale <= 0 ? 1 : 1f / scale;

            for (int i = 0; i < count; i++)
            {
                Vector2 home = homePositions[i];

                // 竖直位移只在锚点贴顶或贴底时生效：锚点在中间时上下移动会破坏居中布局。
                bool verticalShiftApplies = (TextContainer.Origin & (Anchor.y0 | Anchor.y2)) != 0;
                float shiftY = verticalShiftApplies ? motionShiftY : 0;

                Vector2 offset = new Vector2(motionShift, shiftY) * motionProgress * compensation;

                if (count >= 2 && motionProgress > 0)
                {
                    float ratio = (i - (count - 1) / 2f) / ((count - 1) / 2f);
                    offset += new Vector2(motionExpand, motionTilt) * ratio * motionProgress * compensation;
                }

                glyphs[i].Position = home + offset;
            }
        }

        private void setText(string value)
        {
            if (value == text)
                return;

            text = value;
            updateGlyphs();
        }

        private void onThemeChanged()
        {
            textures.Clear();
            store?.ClearCache();
            updateGlyphs();
        }

        private void updateGlyphs()
        {
            int count = text.Length;

            while (glyphs.Count > count)
            {
                Sprite last = glyphs[^1];
                glyphs.RemoveAt(glyphs.Count - 1);
                TextContainer.Remove(last, false);
                pool.Push(last);
            }

            while (glyphs.Count < count)
            {
                Sprite sprite = pool.Count > 0
                    ? pool.Pop()
                    : new Sprite
                    {
                        Anchor = Anchor.TopLeft,
                        Origin = Anchor.Centre,
                    };

                glyphs.Add(sprite);
                TextContainer.Add(sprite);
            }

            applyTextures();
            layoutGlyphs();
        }

        private void applyTextures()
        {
            float maxHeight = 0;

            for (int i = 0; i < text.Length; i++)
                maxHeight = Math.Max(maxHeight, getTexture(text[i])?.Height ?? 0);

            if (maxHeight <= 0)
                maxHeight = 1;

            referenceHeight = getTexture('5')?.Height ?? maxHeight;

            float scale = GLYPH_TARGET_HEIGHT / maxHeight;

            for (int i = 0; i < text.Length; i++)
            {
                Sprite glyph = glyphs[i];
                Texture? texture = getTexture(text[i]);

                glyph.Texture = texture;
                glyph.Size = texture == null ? Vector2.Zero : new Vector2(texture.Width, texture.Height) * scale;
                glyph.Colour = glyphColour;
            }
        }

        private void layoutGlyphs()
        {
            float totalWidth = 0;

            for (int i = 0; i < glyphs.Count; i++)
                totalWidth += glyphs[i].Size.X;

            Size = new Vector2(totalWidth, GLYPH_TARGET_HEIGHT);
            TextContainer.Size = Size;

            homePositions.Clear();

            float x = 0;

            for (int i = 0; i < glyphs.Count; i++)
            {
                Sprite glyph = glyphs[i];
                var home = new Vector2(x + glyph.Size.X / 2, GLYPH_TARGET_HEIGHT / 2);

                homePositions.Add(home);
                glyph.Position = home;
                x += glyph.Size.X;
            }

            applyMotion();
        }

        private Texture? getTexture(char character)
        {
            if (textures.TryGetValue(character, out Texture? cached))
                return cached;

            if (store == null)
                return null;

            Texture? texture = store.Get(EzGameThemeNames.Normalize(ThemeName.Value), character)?.Texture;
            textures[character] = texture;

            return texture;
        }

        private static string[] getPossiblePaths(string themeName, string lookup, char character)
            => new[] { $"GameTheme/{themeName}/combo/number/{lookup}" };
    }
}
