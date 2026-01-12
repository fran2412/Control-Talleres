using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Services.Sedes;
using ControlTalleresMVP.Services.Sesion;
using System.Collections.ObjectModel;

namespace ControlTalleresMVP.ViewModel.Sesion
{
    public partial class SeleccionSedeViewModel : ObservableObject
    {
        private readonly ISedeService _sedeService;
        private readonly ISesionService _sesionService;

        [ObservableProperty]
        private ObservableCollection<SedeDTO> _sedes = new();

        [ObservableProperty]
        private SedeDTO? _sedeSeleccionada;

        /// <summary>
        /// Indica si la selección fue confirmada (para cerrar la ventana).
        /// </summary>
        public bool Confirmado { get; private set; }

        public SeleccionSedeViewModel(ISedeService sedeService, ISesionService sesionService)
        {
            _sedeService = sedeService;
            _sesionService = sesionService;
        }

        public async Task CargarSedesAsync()
        {
            var sedes = await _sedeService.ObtenerSedesParaGridAsync();
            Sedes = new ObservableCollection<SedeDTO>(sedes);

            // Seleccionar la primera sede por defecto si hay alguna
            if (Sedes.Count > 0)
            {
                SedeSeleccionada = Sedes[0];
            }
        }

        [RelayCommand]
        private void Confirmar()
        {
            if (SedeSeleccionada is null) return;

            _sesionService.EstablecerSede(SedeSeleccionada.Id, SedeSeleccionada.Nombre);
            Confirmado = true;
        }
    }
}
