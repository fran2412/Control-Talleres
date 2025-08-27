using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Abstractions
{
    public interface INavigatorService: INotifyPropertyChanged
    {
        object? CurrentViewModel { get; }
        void NavigateTo<TViewModel>() where TViewModel : class;
        void NavigateTo(Type viewModelType);
    }
}
