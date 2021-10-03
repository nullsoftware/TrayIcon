namespace NullSoftware.ToolKit
{
    /// <summary>
    /// Defines a set of standardized icons that can be associated with a ToolTip.
    /// </summary>
    public enum NotificationType : byte
    {
        /// <summary>
        /// Not a standard icon.
        /// </summary>
        None,

        /// <summary>
        /// An information icon.
        /// </summary>
        Information,

        /// <summary>
        /// A warning icon.
        /// </summary>
        Warning,

        /// <summary>
        /// An error icon.
        /// </summary>
        Error
    }
}