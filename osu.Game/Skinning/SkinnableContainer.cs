// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Game.Skinning
{
    /// <summary>
    /// A container which holds many skinnable components, with functionality to add, remove and reload layouts.
    /// Used to allow user customisation of skin layouts.
    /// </summary>
    /// <remarks>
    /// This is currently used as a means of serialising skin layouts to files.
    /// Currently, one json file in a skin will represent one <see cref="SkinnableContainer"/>, containing
    /// the output of <see cref="ISerialisableDrawableContainer.CreateSerialisedInfo"/>.
    /// </remarks>
    public partial class SkinnableContainer : SkinReloadableDrawable, ISerialisableDrawableContainer
    {
        /// <summary>
        /// Invoked when the skinnable components of this container finish loading.
        /// </summary>
        public event Action<Drawable>? OnComponentsLoaded;

        private Container? content;

        /// <summary>
        /// The lookup criteria which will be used to retrieve components from the active skin.
        /// </summary>
        public GlobalSkinnableContainerLookup Lookup { get; }

        public IBindableList<ISerialisableDrawable> Components => components;

        private readonly BindableList<ISerialisableDrawable> components = new BindableList<ISerialisableDrawable>();

        public override bool IsPresent => base.IsPresent || Scheduler.HasPendingTasks; // ensure that components are loaded even if the target container is hidden (ie. due to user toggle).

        public bool ComponentsLoaded { get; private set; }

        private CancellationTokenSource? cancellationSource;

        /// <summary>
        /// Optional external container. When set, <see cref="Reload(Container?)"/> will
        /// proxy visual output to this target instead of adding children to this instance.
        /// Order of internal cleanup (ClearInternal → components.Clear → ComponentsLoaded → content → cancel)
        /// is identical to the default path to preserve pipeline stability.
        /// </summary>
        public Container? ExternalTarget { get; set; }

        public SkinnableContainer(GlobalSkinnableContainerLookup lookup)
        {
            Lookup = lookup;
        }

        public void Reload() => Reload((
                CurrentSkin.GetDrawableComponent(new UserSkinComponentLookup(Lookup))
                ?? CurrentSkin.GetDrawableComponent(Lookup))
            as Container);

        public void Reload(Container? componentsContainer)
        {
            ClearInternal();
            components.Clear();
            ComponentsLoaded = false;

            content = componentsContainer ?? new Container
            {
                RelativeSizeAxes = Axes.Both
            };

            cancellationSource?.Cancel();
            cancellationSource = null;

            if (ExternalTarget != null)
            {
                LoadComponentAsync(content, wrapper =>
                {
                    ExternalTarget.Clear();
                    // Add the wrapper itself rather than re-parenting its children,
                    // to avoid "May not add a drawable to multiple containers" errors
                    // when children are already parented by the async loading pipeline.
                    ExternalTarget.Add(wrapper);
                    components.AddRange(wrapper.Children.OfType<ISerialisableDrawable>());
                    ComponentsLoaded = true;
                    OnComponentsLoaded?.Invoke(this);
                }, (cancellationSource = new CancellationTokenSource()).Token);
            }
            else
            {
                LoadComponentAsync(content, wrapper =>
                {
                    AddInternal(wrapper);
                    components.AddRange(wrapper.Children.OfType<ISerialisableDrawable>());
                    ComponentsLoaded = true;
                    OnComponentsLoaded?.Invoke(this);
                }, (cancellationSource = new CancellationTokenSource()).Token);
            }
        }

        /// <inheritdoc cref="ISerialisableDrawableContainer"/>
        /// <exception cref="NotSupportedException">Thrown when attempting to add an element to a target which is not supported by the current skin.</exception>
        /// <exception cref="ArgumentException">Thrown if the provided instance is not a <see cref="Drawable"/>.</exception>
        public void Add(ISerialisableDrawable component)
        {
            Container? target = ExternalTarget ?? content;

            if (target == null)
                throw new NotSupportedException("Attempting to add a new component to a target container which is not supported by the current skin.");

            if (!(component is Drawable drawable))
                throw new ArgumentException($"Provided argument must be of type {nameof(Drawable)}.", nameof(component));

            target.Add(drawable);
            components.Add(component);
        }

        /// <inheritdoc cref="ISerialisableDrawableContainer"/>
        /// <exception cref="NotSupportedException">Thrown when attempting to add an element to a target which is not supported by the current skin.</exception>
        /// <exception cref="ArgumentException">Thrown if the provided instance is not a <see cref="Drawable"/>.</exception>
        public void Remove(ISerialisableDrawable component, bool disposeImmediately)
        {
            Container? target = ExternalTarget ?? content;

            if (target == null)
                throw new NotSupportedException("Attempting to remove a new component from a target container which is not supported by the current skin.");

            if (!(component is Drawable drawable))
                throw new ArgumentException($"Provided argument must be of type {nameof(Drawable)}.", nameof(component));

            target.Remove(drawable, disposeImmediately);
            components.Remove(component);
        }

        protected override void SkinChanged(ISkinSource skin)
        {
            base.SkinChanged(skin);

            Reload();
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            OnComponentsLoaded = null;
        }
    }
}
