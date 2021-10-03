using System.Windows.Input;

namespace TrayIcon.Example
{
    /// <summary>
    /// Refreshable command, wehere can be manually raised <see cref="ICommand.CanExecuteChanged"/> event.
    /// </summary>
    public interface IRefreshableCommand : ICommand
    {
        /// <summary>
        /// Raises <see cref="ICommand.CanExecuteChanged"/> event.
        /// </summary>
        void Refresh();
    }
}
