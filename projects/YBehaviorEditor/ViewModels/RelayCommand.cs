using System;
using System.Diagnostics;
using System.Windows.Input;

namespace YBehavior.Editor
{
    /// <summary>
    /// A command whose sole purpose is to 
    /// relay its functionality to other
    /// objects by invoking delegates. The
    /// default return value for the CanExecute
    /// method is 'true'.
    /// </summary>
    public class RelayCommand : ICommand
    {
        #region Fields

        readonly Action<object> _execute;
        readonly Predicate<object> _canExecute;
        ICanExecuteChanged _canExecuteChanged;
        static ICanExecuteChanged s_CanExecuteChanged = new CommandManagerCanExecuteChanged();
        #endregion // Fields

        #region Constructors

        /// <summary>
        /// Creates a new command that can always execute.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        public RelayCommand(Action<object> execute)
            : this(execute, null, null)
        {
        }

        /// <summary>
        /// Creates a new command.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute, ICanExecuteChanged canExecuteChanged = null)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            _execute = execute;
            _canExecute = canExecute;
            _canExecuteChanged = canExecuteChanged;
        }

        #endregion // Constructors

        #region ICommand Members

        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (_canExecute != null)
                {
                    if (_canExecuteChanged != null)
                        _canExecuteChanged.CanExecuteChanged += value;
                    else
                        s_CanExecuteChanged.CanExecuteChanged += value;
                }
            }
            remove
            {
                if (_canExecute != null)
                {
                    if (_canExecuteChanged != null)
                        _canExecuteChanged.CanExecuteChanged -= value;
                    else
                        s_CanExecuteChanged.CanExecuteChanged -= value;
                }
            }
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        #endregion // ICommand Members
    }

    public interface ICanExecuteChanged
    {
        event EventHandler CanExecuteChanged;
    }

    public class CommandManagerCanExecuteChanged : ICanExecuteChanged
    {
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}