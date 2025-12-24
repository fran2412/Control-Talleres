using System.ComponentModel;

namespace ControlTalleresMVP.Abstractions
{
    public interface INavigatorService : INotifyPropertyChanged
    {
        object? CurrentViewModel { get; }
        void NavigateTo<TViewModel>() where TViewModel : class;
        void NavigateTo(Type viewModelType);
    }
}
