using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ControlTalleresMVP.Messages;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Services.Clases;
using System.Collections.ObjectModel;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class ResumenAsistenciaDiaViewModel : ObservableObject
    {
        private readonly IClaseService _claseService;

        public ResumenAsistenciaDiaViewModel(IClaseService claseService)
        {
            _claseService = claseService;
            FechaResumen = DateTime.Today;

            WeakReferenceMessenger.Default.Register<FechaClasesSeleccionadaCambiadaMessage>(this, (_, m) =>
            {
                if (FechaResumen.Date != m.Fecha.Date)
                {
                    FechaResumen = m.Fecha.Date;
                }
            });

            WeakReferenceMessenger.Default.Register<ClasesActualizadasMessage>(this, (_, m) =>
            {
                _ = CargarResumenAsync();
            });

            _ = CargarResumenAsync();
        }

        [ObservableProperty]
        private DateTime fechaResumen;

        [ObservableProperty]
        private ObservableCollection<ResumenAsistenciaTallerDTO> resumenTalleres = new();

        [ObservableProperty]
        private bool hayTalleresHoy;

        [ObservableProperty]
        private string fechaTexto = string.Empty;

        partial void OnFechaResumenChanged(DateTime value)
        {
            _ = CargarResumenAsync();
        }

        [RelayCommand]
        private async Task CargarResumenAsync()
        {
            try
            {
                var fecha = FechaResumen;
                var resumen = await _claseService.ObtenerResumenAsistenciaDiaAsync(fecha);

                ResumenTalleres = new ObservableCollection<ResumenAsistenciaTallerDTO>(resumen);
                HayTalleresHoy = resumen.Count > 0;

                var cultura = new System.Globalization.CultureInfo("es-MX");
                FechaTexto = fecha.ToString("dddd d 'de' MMMM", cultura);
                FechaTexto = char.ToUpper(FechaTexto[0]) + FechaTexto.Substring(1);
            }
            catch
            {
                ResumenTalleres = new ObservableCollection<ResumenAsistenciaTallerDTO>();
                HayTalleresHoy = false;
            }
        }
    }
}
