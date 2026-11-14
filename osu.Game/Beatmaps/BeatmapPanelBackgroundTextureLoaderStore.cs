// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace osu.Game.Beatmaps
{
    // Implementation of this class is based off of `MaxDimensionLimitedTextureLoaderStore`.
    // If issues are found it's worth checking to make sure similar issues exist there.
    public class BeatmapPanelBackgroundTextureLoaderStore : IResourceStore<TextureUpload>
    {
        // The aspect ratio of SetPanelBackground at its maximum size (very tall window).
        private const float minimum_display_ratio = 512 / 80f;

        // Panels are ~500-700px wide on screen; keeping the source at native width wastes VRAM and
        // upload bandwidth several times over for no visible gain.
        private const int maximum_width = 768;

        private readonly IResourceStore<TextureUpload>? textureStore;

        public BeatmapPanelBackgroundTextureLoaderStore(IResourceStore<TextureUpload>? textureStore)
        {
            this.textureStore = textureStore;
        }

        public void Dispose()
        {
            textureStore?.Dispose();
        }

        public TextureUpload Get(string name)
        {
            var textureUpload = textureStore?.Get(name);

            // NRT not enabled on framework side classes (IResourceStore / TextureLoaderStore), welp.
            if (textureUpload == null)
                return null!;

            return limitTextureUploadSize(textureUpload);
        }

        public async Task<TextureUpload> GetAsync(string name, CancellationToken cancellationToken = new CancellationToken())
        {
            // NRT not enabled on framework side classes (IResourceStore / TextureLoaderStore), welp.
            if (textureStore == null)
                return null!;

            var textureUpload = await textureStore.GetAsync(name, cancellationToken).ConfigureAwait(false);

            if (textureUpload == null)
                return null!;

            return await Task.Run(() => limitTextureUploadSize(textureUpload), cancellationToken).ConfigureAwait(false);
        }

        private TextureUpload limitTextureUploadSize(TextureUpload textureUpload)
        {
            var image = Image.LoadPixelData(textureUpload.Data, textureUpload.Width, textureUpload.Height);

            // The original texture upload will no longer be returned or used.
            textureUpload.Dispose();

            // Downscale before cropping: panel backgrounds never need more than ~768px of width.
            if (image.Width > maximum_width)
            {
                int scaledHeight = Math.Max(1, (int)Math.Round(image.Height * (double)maximum_width / image.Width));
                image.Mutate(i => i.Resize(maximum_width, scaledHeight));
            }

            Size size = image.Size;

            // Assume that panel backgrounds are always displayed using `FillMode.Fill`.
            // Also assume that all backgrounds are wider than they are tall, so the
            // fill is always going to be based on width.
            //
            // We need to include enough height to make this work for all ratio panels are displayed at.
            int usableHeight = (int)Math.Ceiling(size.Width * 1 / minimum_display_ratio);

            usableHeight = Math.Min(size.Height, usableHeight);

            // Crop the centre region of the background for now.
            Rectangle cropRectangle = new Rectangle(
                0,
                (size.Height - usableHeight) / 2,
                size.Width,
                usableHeight
            );

            image.Mutate(i => i.Crop(cropRectangle));

            return new TextureUpload(image);
        }

        public Stream? GetStream(string name) => textureStore?.GetStream(name);

        public IEnumerable<string> GetAvailableResources() => textureStore?.GetAvailableResources() ?? Array.Empty<string>();
    }
}
