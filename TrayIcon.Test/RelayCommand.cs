using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TrayIcon.Example
{
    /// <summary>
    /// Basic implementation of implementing a windows input command.
    /// </summary>
    public class RelayCommand : IRefreshableCommand
    {
        #region Fields

        readonly Action _execute;
        readonly Func<bool> _canExecute;

        #endregion

        #region Events

        private event EventHandler _canExecuteChanged;

        public event EventHandler CanExecuteChanged
        {
            add
            {
                _canExecuteChanged += value;
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                _canExecuteChanged -= value;
                CommandManager.RequerySuggested -= value;
            }
        }

        #endregion

        #region Constructors

        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            if (execute == null)
                throw new NullReferenceException(nameof(execute));

            _execute = execute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute) : this(execute, null)
        {

        }

        public RelayCommand(Func<Task> execute) : this(async () => { await execute(); }, null)
        {

        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute();
        }

        /// <inheritdoc/>
        public void Execute(object parameter)
        {
            _execute.Invoke();
        }

        /// <summary>
        /// Raises <see cref="CanExecuteChanged"/> event.
        /// </summary>
        public void Refresh()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }

    /// <summary>
    /// Basic implementation of implementing a windows input command with parameter support.
    /// </summary>
    /// <typeparam name="T">Type of command parameter.</typeparam>
    public class RelayCommand<T> : IRefreshableCommand
    {
        #region Fields

        readonly Action<T> _execute;
        readonly Func<T, bool> _canExecute;

        #endregion

        #region Events

        private event EventHandler _canExecuteChanged;

        public event EventHandler CanExecuteChanged
        {
            add
            {
                _canExecuteChanged += value;
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                _canExecuteChanged -= value;
                CommandManager.RequerySuggested -= value;
            }
        }

        #endregion

        #region Constructors

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute)
        {
            if (execute == null)
                throw new NullReferenceException(nameof(execute));

            _execute = execute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action<T> execute) : this(execute, null)
        {

        }

        public RelayCommand(Func<T, Task> execute) : this(async (arg) => await execute(arg), null)
        {

        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute((T)parameter);
        }

        /// <inheritdoc/>
        public void Execute(object parameter)
        {
            _execute.Invoke((T)parameter);
        }

        /// <summary>
        /// Raises <see cref="CanExecuteChanged"/> event.
        /// </summary>
        public void Refresh()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }

}
