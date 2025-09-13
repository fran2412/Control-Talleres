using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Services.Clases;
using ControlTalleresMVP.Services.Generaciones;
using ControlTalleresMVP.Services.Talleres;
using ControlTalleresMVP.Services.Exportacion;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuReporteEstadoPagosViewModel : ObservableObject
    {
        private readonly IClaseService _claseService;
        private readonly ITallerService _tallerService;
        private readonly IGeneracionService _generacionService;
        private readonly IExportacionService _exportacionService;

        [ObservableProperty] private ObservableCollection<EstadoPagoAlumnoDTO> estadosPago = new();
        [ObservableProperty] private ObservableCollection<TallerDTO> talleres = new();
        [ObservableProperty] private TallerDTO? tallerSeleccionado;
        [ObservableProperty] private ObservableCollection<GeneracionDTO> generaciones = new();
        [ObservableProperty] private GeneracionDTO? generacionSeleccionada;
        [ObservableProperty] private bool cargando = false;
        [ObservableProperty] private string? mensajeEstado;

        public string TituloEncabezado { get; set; } = "Reporte de Estado de Pagos";

        public MenuReporteEstadoPagosViewModel(
            IClaseService claseService,
            ITallerService tallerService,
            IGeneracionService generacionService,
            IExportacionService exportacionService)
        {
            _claseService = claseService;
            _tallerService = tallerService;
            _generacionService = generacionService;
            _exportacionService = exportacionService;

            // Cargar datos automáticamente al inicializar
            _ = CargarDatosAsync();
        }

        private async Task CargarDatosAsync()
        {
            try
            {
                Cargando = true;
                MensajeEstado = "Cargando datos...";

                // Verificar generación actual
                var generacion = _generacionService.ObtenerGeneracionActual();
                if (generacion == null)
                {
                    MensajeEstado = "No hay generación activa. Por favor, configure una generación actual.";
                    return;
                }

                MensajeEstado = $"Generación actual: {generacion.Nombre}";

                // Cargar generaciones
                var generacionesList = await _generacionService.ObtenerGeneracionesParaGridAsync();
                Generaciones = new ObservableCollection<GeneracionDTO>(generacionesList);
                // Seleccionar la generación actual por defecto
                GeneracionSeleccionada = generacionesList.FirstOrDefault(g => g.Id == generacion.GeneracionId);

                // Cargar talleres
                var talleresList = await _tallerService.ObtenerTalleresParaGridAsync();
                Talleres = new ObservableCollection<TallerDTO>(talleresList);
                MensajeEstado = $"Talleres cargados: {talleresList.Count}";


                // Cargar estados de pago
                await CargarEstadosPagoAsync();
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al cargar datos: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error en CargarDatosAsync: {ex}");
            }
            finally
            {
                Cargando = false;
            }
        }

        [RelayCommand]
        public async Task FiltrarAsync()
        {
            try
            {
                Cargando = true;
                MensajeEstado = "Aplicando filtros...";

                // Obtener IDs de los filtros seleccionados
                var tallerId = TallerSeleccionado?.Id;
                var generacionId = GeneracionSeleccionada?.Id;

                // Construir mensaje de filtros aplicados
                var filtrosAplicados = new List<string>();
                if (tallerId.HasValue) filtrosAplicados.Add($"Taller: {TallerSeleccionado!.Nombre}");
                if (generacionId.HasValue) filtrosAplicados.Add($"Generación: {GeneracionSeleccionada!.Nombre}");

                var mensajeFiltros = filtrosAplicados.Count > 0 
                    ? string.Join(", ", filtrosAplicados)
                    : "Todos los registros";

                MensajeEstado = $"Filtrando por: {mensajeFiltros}";

                // Llamar al servicio con los filtros
                var estados = await _claseService.ObtenerEstadoPagoAlumnosAsync(tallerId, null, generacionId);
                
                // Aplicar filtros adicionales en memoria si es necesario
                var estadosFiltrados = estados.AsEnumerable();

                // Filtrar por generación si está seleccionada
                if (generacionId.HasValue)
                {
                    // Nota: Este filtro se aplicaría en el servicio, pero por ahora lo hacemos aquí
                    // En una implementación completa, se pasaría al servicio
                }

                // Ordenar por estado antes de asignar
                var estadosOrdenados = OrdenarPorEstado(estadosFiltrados);
                EstadosPago = new ObservableCollection<EstadoPagoAlumnoDTO>(estadosOrdenados);
                MensajeEstado = $"Mostrando {EstadosPago.Count} registros - {mensajeFiltros}";
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al filtrar: {ex.Message}";
                EstadosPago.Clear();
            }
            finally
            {
                Cargando = false;
            }
        }


        [RelayCommand]
        public void LimpiarFiltros()
        {
            // Solo limpiar la selección de filtros, no cambiar los datos
            TallerSeleccionado = null;
            GeneracionSeleccionada = null;
            MensajeEstado = "Filtros limpiados";
        }

        [RelayCommand]
        public void ReordenarPorEstado()
        {
            if (EstadosPago.Any())
            {
                var estadosOrdenados = OrdenarPorEstado(EstadosPago);
                EstadosPago = new ObservableCollection<EstadoPagoAlumnoDTO>(estadosOrdenados);
                MensajeEstado = "Datos reordenados por estado (Completos → Parciales por progreso → Sin Pagos)";
            }
        }

        [RelayCommand]
        public async Task MostrarTodos()
        {
            try
            {
                Cargando = true;
                MensajeEstado = "Cargando todos los registros...";
                
                // Limpiar filtros primero
                TallerSeleccionado = null;
                GeneracionSeleccionada = null;
                
                // Cargar todos los datos sin filtros (null = sin filtro)
                var estados = await _claseService.ObtenerEstadoPagoAlumnosAsync(null, null, null);
                
                EstadosPago = new ObservableCollection<EstadoPagoAlumnoDTO>(estados);
                
                MensajeEstado = $"Mostrando todos los registros: {EstadosPago.Count}";
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al cargar todos los registros: {ex.Message}";
                EstadosPago.Clear();
            }
            finally
            {
                Cargando = false;
            }
        }

        private async Task CargarEstadosPagoAsync()
        {
            try
            {
                // Usar siempre la generación actual por defecto
                var generacionId = _generacionService.ObtenerGeneracionActual()?.GeneracionId;
                var estados = await _claseService.ObtenerEstadoPagoAlumnosAsync(null, null, generacionId);
                var estadosOrdenados = OrdenarPorEstado(estados);
                EstadosPago = new ObservableCollection<EstadoPagoAlumnoDTO>(estadosOrdenados);
                MensajeEstado = $"Mostrando {EstadosPago.Count} registros";
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al cargar estados de pago: {ex.Message}";
                EstadosPago.Clear();
                throw; // Re-lanzar para que el método llamador pueda manejar el error
            }
        }

        /// <summary>
        /// Ordena los estados de pago por prioridad: Completos → Parciales (por progreso) → Sin Pagos
        /// </summary>
        private IEnumerable<EstadoPagoAlumnoDTO> OrdenarPorEstado(IEnumerable<EstadoPagoAlumnoDTO> estados)
        {
            return estados.OrderByDescending(e => e.TodasLasClasesPagadas) // Completos primero (TodasLasClasesPagadas = true)
                         .ThenByDescending(e => e.ClasesPagadas > 0) // Luego parciales (ClasesPagadas > 0 pero no todas)
                         .ThenByDescending(e => CalcularProgresoPago(e.MontoTotal, e.MontoPendiente)) // Parciales por progreso (mayor a menor)
                         .ThenBy(e => e.NombreAlumno); // Finalmente por nombre de alumno
        }

        /// <summary>
        /// Calcula el progreso de pago basado en el monto total y el saldo pendiente
        /// </summary>
        private static decimal CalcularProgresoPago(decimal montoTotal, decimal montoPendiente)
        {
            if (montoTotal <= 0) return 0;
            
            var montoPagado = montoTotal - montoPendiente;
            var progreso = (montoPagado / montoTotal) * 100;
            
            return Math.Max(0, Math.Min(100, progreso));
        }

        // Propiedades calculadas para estadísticas
        public int TotalAlumnos => EstadosPago.Count;
        public int AlumnosCompletos => EstadosPago.Count(e => e.TodasLasClasesPagadas);
        public int AlumnosParciales => EstadosPago.Count(e => e.ClasesPagadas > 0 && !e.TodasLasClasesPagadas);
        public int AlumnosSinPagos => EstadosPago.Count(e => e.ClasesPagadas == 0);
        
        // Métricas financieras
        public decimal MontoTotalEsperado => EstadosPago.Sum(e => e.MontoTotal);
        public decimal MontoTotalRecaudado => EstadosPago.Sum(e => e.MontoPagado);
        public decimal MontoPendiente => EstadosPago.Sum(e => e.MontoPendiente);
        public decimal PorcentajeRecaudacion => MontoTotalEsperado > 0 ? (MontoTotalRecaudado / MontoTotalEsperado) * 100 : 0;
        
        // Métricas de clases
        public int TotalClasesEsperadas => EstadosPago.Sum(e => e.ClasesTotales);
        public int TotalClasesPagadas => EstadosPago.Sum(e => e.ClasesPagadas);
        public int TotalClasesPendientes => EstadosPago.Sum(e => e.ClasesPendientes);
        public decimal PorcentajeClasesPagadas => TotalClasesEsperadas > 0 ? (decimal)TotalClasesPagadas / TotalClasesEsperadas * 100 : 0;
        
        // Métricas por taller (si hay filtro)
        public int TalleresActivos => EstadosPago.Select(e => e.TallerId).Distinct().Count();
        public decimal PromedioPagoPorAlumno => TotalAlumnos > 0 ? MontoTotalRecaudado / TotalAlumnos : 0;
        public decimal PromedioClasesPorAlumno => TotalAlumnos > 0 ? (decimal)TotalClasesEsperadas / TotalAlumnos : 0;
        
        // Formateo de montos para mostrar
        public string MontoTotalEsperadoFormateado => $"${MontoTotalEsperado:N2}";
        public string MontoTotalRecaudadoFormateado => $"${MontoTotalRecaudado:N2}";
        public string MontoPendienteFormateado => $"${MontoPendiente:N2}";
        public string PorcentajeRecaudacionFormateado => $"{PorcentajeRecaudacion:F1}%";
        public string PromedioPagoPorAlumnoFormateado => $"${PromedioPagoPorAlumno:N2}";

        // Método para actualizar estadísticas cuando cambia la colección
        partial void OnEstadosPagoChanged(ObservableCollection<EstadoPagoAlumnoDTO> value)
        {
            // Estadísticas básicas
            OnPropertyChanged(nameof(TotalAlumnos));
            OnPropertyChanged(nameof(AlumnosCompletos));
            OnPropertyChanged(nameof(AlumnosParciales));
            OnPropertyChanged(nameof(AlumnosSinPagos));
            
            // Métricas financieras
            OnPropertyChanged(nameof(MontoTotalEsperado));
            OnPropertyChanged(nameof(MontoTotalRecaudado));
            OnPropertyChanged(nameof(MontoPendiente));
            OnPropertyChanged(nameof(PorcentajeRecaudacion));
            
            // Métricas de clases
            OnPropertyChanged(nameof(TotalClasesEsperadas));
            OnPropertyChanged(nameof(TotalClasesPagadas));
            OnPropertyChanged(nameof(TotalClasesPendientes));
            OnPropertyChanged(nameof(PorcentajeClasesPagadas));
            
            // Métricas adicionales
            OnPropertyChanged(nameof(TalleresActivos));
            OnPropertyChanged(nameof(PromedioPagoPorAlumno));
            OnPropertyChanged(nameof(PromedioClasesPorAlumno));
            
            // Formateo
            OnPropertyChanged(nameof(MontoTotalEsperadoFormateado));
            OnPropertyChanged(nameof(MontoTotalRecaudadoFormateado));
            OnPropertyChanged(nameof(MontoPendienteFormateado));
            OnPropertyChanged(nameof(PorcentajeRecaudacionFormateado));
            OnPropertyChanged(nameof(PromedioPagoPorAlumnoFormateado));
        }

        [RelayCommand]
        public async Task ExportarAsync()
        {
            try
            {
                Cargando = true;
                MensajeEstado = "Exportando datos...";

                if (!EstadosPago.Any())
                {
                    MensajeEstado = "No hay datos para exportar";
                    return;
                }

                // Exportar como CSV por defecto
                var rutaArchivo = await _exportacionService.ExportarEstadoPagosAsync(EstadosPago, "csv");
                MensajeEstado = $"Archivo exportado exitosamente: {System.IO.Path.GetFileName(rutaArchivo)}";
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al exportar: {ex.Message}";
            }
            finally
            {
                Cargando = false;
            }
        }

        [RelayCommand]
        public async Task ExportarExcelAsync()
        {
            try
            {
                Cargando = true;
                MensajeEstado = "Exportando a Excel...";

                if (!EstadosPago.Any())
                {
                    MensajeEstado = "No hay datos para exportar";
                    return;
                }

                var rutaArchivo = await _exportacionService.ExportarEstadoPagosAsync(EstadosPago, "xlsx");
                MensajeEstado = $"Archivo Excel exportado exitosamente: {System.IO.Path.GetFileName(rutaArchivo)}";
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al exportar: {ex.Message}";
            }
            finally
            {
                Cargando = false;
            }
        }

        // Propiedades para mostrar información del filtro actual
        public string FiltroActual
        {
            get
            {
                var filtros = new List<string>();
                if (TallerSeleccionado != null) filtros.Add($"Taller: {TallerSeleccionado.Nombre}");
                if (GeneracionSeleccionada != null) filtros.Add($"Generación: {GeneracionSeleccionada.Nombre}");
                
                return filtros.Count > 0 
                    ? $"Filtrado por: {string.Join(", ", filtros)}"
                    : "Mostrando todos los registros";
            }
        }

        public bool TieneFiltroActivo => TallerSeleccionado != null || GeneracionSeleccionada != null;

        // Métodos para actualizar información de filtro cuando cambian las selecciones
        partial void OnTallerSeleccionadoChanged(TallerDTO? value)
        {
            OnPropertyChanged(nameof(FiltroActual));
            OnPropertyChanged(nameof(TieneFiltroActivo));
        }

        partial void OnGeneracionSeleccionadaChanged(GeneracionDTO? value)
        {
            OnPropertyChanged(nameof(FiltroActual));
            OnPropertyChanged(nameof(TieneFiltroActivo));
        }

    }
}
