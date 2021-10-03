namespace NullSoftware.ToolKit
{
    /// <summary>
    /// Provides the ability to notify user using balloon tip.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Displays a balloon tip with the specified title, text, and empty icon in the taskbar.
        /// </summary>
        /// <param name="title">The title to display on the balloon tip.</param>
        /// <param name="text">The text to display on the balloon tip.</param>
        void Notify(string title, string text);

        /// <summary>
        /// Displays a balloon tip with the specified title, text, and icon in the taskbar.
        /// </summary>
        /// <param name="title">The title to display on the balloon tip.</param>
        /// <param name="text">The text to display on the balloon tip.</param>
        /// <param name="notificationType">One of the <see cref="NotificationType"/> values.</param>
        void Notify(string title, string text, NotificationType notificationType);
    }
}