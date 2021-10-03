using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TrayIcon.Example
{
    /// <summary>
    /// Base class for objects that require property notification.
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        /// <summary>
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/>.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Handles "property changed" operation.
        /// </summary>
        /// <typeparam name="T">Type of property.</typeparam>
        /// <param name="property">Property field.</param>
        /// <param name="value">New value of property.</param>
        /// <param name="propertyName">Property name.</param>
        protected void OnPropertyChanged<T>(ref T property, T value,
            [CallerMemberName] string propertyName = "")
        {
            property = value;
            RaisePropertyChanged(propertyName);
        }

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">Property name.</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
