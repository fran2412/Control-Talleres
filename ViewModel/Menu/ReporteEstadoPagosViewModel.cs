using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Services.Clases;
using ControlTalleresMVP.Services.Generaciones;
using ControlTalleresMVP.Services.Talleres;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class ReporteEstadoPagosViewModel : ObservableObject
    {
        private readonly IClaseService _claseService;
        private readonly ITallerService _tallerService;
        private readonly IGeneracionService _generacionService;

        [ObservableProperty] private ObservableCollection<EstadoPagoAlumnoDTO> estadosPago = new();
        [ObservableProperty] private ObservableCollection<TallerDTO> talleres = new();
        [ObservableProperty] private TallerDTO? tallerSeleccionado;
        [ObservableProperty] private bool cargando = false;
        [ObservableProperty] private string? mensajeEstado;

        public string TituloEncabezado { get; set; } = "Reporte de Estado de Pagos";

        public ReporteEstadoPagosViewModel(
            IClaseService claseService,
            ITallerService tallerService,
            IGeneracionService generacionService)
        {
            _claseService = claseService;
            _tallerService = tallerService;
            _generacionService = generacionService;
        }

        [RelayCommand]
        private async Task CargarDatosAsync()
        {
            try
            {
                Cargando = true;
                MensajeEstado = "Cargando datos...";

                // Cargar talleres
                var talleresList = await _tallerService.ObtenerTalleresParaGridAsync();
                Talleres = new ObservableCollection<TallerDTO>(talleresList);

                // Cargar estados de pago
                await CargarEstadosPagoAsync();
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al cargar datos: {ex.Message}";
            }
            finally
            {
                Cargando = false;
            }
        }

        [RelayCommand]
        private async Task FiltrarPorTallerAsync()
        {
            if (TallerSeleccionado == null) return;

            try
            {
                Cargando = true;
                MensajeEstado = $"Filtrando por taller: {TallerSeleccionado.Nombre}";

                var estados = await _claseService.ObtenerEstadoPagoAlumnosAsync(TallerSeleccionado.Id);
                EstadosPago = new ObservableCollection<EstadoPagoAlumnoDTO>(estados);

                MensajeEstado = $"Mostrando {EstadosPago.Count} registros para {TallerSeleccionado.Nombre}";
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al filtrar: {ex.Message}";
            }
            finally
            {
                Cargando = false;
            }
        }

        [RelayCommand]
        private async Task MostrarTodosAsync()
        {
            await CargarEstadosPagoAsync();
        }

        [RelayCommand]
        private void LimpiarFiltros()
        {
            TallerSeleccionado = null;
            EstadosPago.Clear();
            MensajeEstado = "Filtros limpiados";
        }

        private async Task CargarEstadosPagoAsync()
        {
            var estados = await _claseService.ObtenerEstadoPagoAlumnosAsync();
            EstadosPago = new ObservableCollection<EstadoPagoAlumnoDTO>(estados);
            MensajeEstado = $"Mostrando {EstadosPago.Count} registros";
        }

        // Propiedades calculadas para estadísticas
        public int TotalAlumnos => EstadosPago.Count;
        public int AlumnosCompletos => EstadosPago.Count(e => e.TodasLasClasesPagadas);
        public int AlumnosParciales => EstadosPago.Count(e => e.ClasesPagadas > 0 && !e.TodasLasClasesPagadas);
        public int AlumnosSinPagos => EstadosPago.Count(e => e.ClasesPagadas == 0);
        public decimal MontoTotalEsperado => EstadosPago.Sum(e => e.MontoTotal);
        public decimal MontoTotalRecaudado => EstadosPago.Sum(e => e.MontoPagado);
        public decimal MontoPendiente => EstadosPago.Sum(e => e.MontoPendiente);
        public decimal PorcentajeRecaudacion => MontoTotalEsperado > 0 ? (MontoTotalRecaudado / MontoTotalEsperado) * 100 : 0;

        // Método para actualizar estadísticas cuando cambia la colección
        partial void OnEstadosPagoChanged(ObservableCollection<EstadoPagoAlumnoDTO> value)
        {
            OnPropertyChanged(nameof(TotalAlumnos));
            OnPropertyChanged(nameof(AlumnosCompletos));
            OnPropertyChanged(nameof(AlumnosParciales));
            OnPropertyChanged(nameof(AlumnosSinPagos));
            OnPropertyChanged(nameof(MontoTotalEsperado));
            OnPropertyChanged(nameof(MontoTotalRecaudado));
            OnPropertyChanged(nameof(MontoPendiente));
            OnPropertyChanged(nameof(PorcentajeRecaudacion));
        }
    }
}
