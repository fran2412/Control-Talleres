using ControlTalleresMVP.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace ControlTalleresMVP.Services.Navigation
{
    public class NavigatorService : INavigatorService
    {
        private readonly IServiceProvider _serviceProvider;
        private object? _currentViewModel;

        public object? CurrentViewModel
        {
            get => _currentViewModel;
            private set
            {
                _currentViewModel = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentViewModel)));
            }
        }

        public NavigatorService(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public void NavigateTo<TViewModel>() where TViewModel : class
            => CurrentViewModel = _serviceProvider.GetRequiredService<TViewModel>();

        public void NavigateTo(Type viewModelType)
            => CurrentViewModel = _serviceProvider.GetRequiredService(viewModelType);

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}