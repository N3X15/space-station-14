using JetBrains.Annotations;

namespace Content.Client.IconSmoothing
{
    /// <summary>
    ///     Makes sprites of other grid-aligned entities like us connect.
    /// </summary>
    /// <remarks>
    ///     The system is based on Baystation12's smoothwalling, and thus will work with those.
    ///     To use, set <c>base</c> equal to the prefix of the corner states in the sprite base RSI.
    ///     Any objects with the same <c>key</c> will connect.
    /// </remarks>
    [RegisterComponent]
    public sealed class IconSmoothComponent : Component
    {
        public (EntityUid, Vector2i)? LastPosition;

        /// <summary>
        ///     We will smooth with other objects with the same key.
        /// </summary>
        [DataField("key")]
        public string? SmoothKey { get; }

        /// <summary>
        ///     Prepended to the RSI state.
        /// </summary>
        [DataField("base")]
        public string StateBase { get; } = string.Empty;

        /// <summary>
        ///     Mode that controls how the icon should be selected.
        /// </summary>
        [DataField("mode")]
        public IconSmoothingMode Mode = IconSmoothingMode.Corners;

        /// <summary>
        ///     Used by <see cref="IconSmoothSystem"/> to reduce redundant updates.
        /// </summary>
        internal int UpdateGeneration { get; set; }
    }

    /// <summary>
    ///     Controls the mode with which icon smoothing is calculated.
    /// </summary>
    [PublicAPI]
    public enum IconSmoothingMode : byte
    {
        /// <summary>
        ///     Each icon is made up of 4 corners, each of which can get a different state depending on
        ///     adjacent entities clockwise, counter-clockwise and diagonal with the corner.
        /// </summary>
        Corners,

        /// <summary>
        ///     There are 16 icons, only one of which is used at once.
        ///     The icon selected is a bit field made up of the cardinal direction flags that have adjacent entities.
        /// </summary>
        CardinalFlags,

        /// <summary>
        ///     Where this component contributes to our neighbors being calculated but we do not update our own sprite.
        /// </summary>
        NoSprite,
    }
}
