using System.Windows.Input;

namespace ControlTalleresMVP.Helpers.Commands
{
    public sealed class RelayCommandGeneric<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;

        public RelayCommandGeneric(Action<T?> execute, Predicate<T?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter is null && typeof(T).IsValueType)
                return _canExecute?.Invoke(default) ?? true;

            return _canExecute?.Invoke((T?)parameter) ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute(parameter is null ? default : (T?)parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
