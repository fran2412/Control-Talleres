using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Services.Exportacion;
using ControlTalleresMVP.Services.Generaciones;
using ControlTalleresMVP.Services.Inscripciones;
using ControlTalleresMVP.Services.Promotores;
using ControlTalleresMVP.Services.Sedes;
using ControlTalleresMVP.Services.Talleres;
using System.Collections.ObjectModel;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuReporteInscripcionesViewModel : ObservableObject
    {
        private readonly IInscripcionReporteService _inscripcionReporteService;
        private readonly ITallerService _tallerService;
        private readonly ISedeService _sedeService;
        private readonly IPromotorService _promotorService;
        private readonly IGeneracionService _generacionService;
        private readonly IExportacionService _exportacionService;

        [ObservableProperty] private ObservableCollection<InscripcionReporteDTO> inscripciones = new();
        [ObservableProperty] private ObservableCollection<TallerDTO> talleres = new();
        [ObservableProperty] private ObservableCollection<PromotorDTO> promotores = new();
        [ObservableProperty] private ObservableCollection<GeneracionDTO> generaciones = new();
        [ObservableProperty] private ObservableCollection<SedeDTO> sedes = new();

        [ObservableProperty] private TallerDTO? tallerSeleccionado;
        [ObservableProperty] private PromotorDTO? promotorSeleccionado;
        [ObservableProperty] private GeneracionDTO? generacionSeleccionada;
        [ObservableProperty] private SedeDTO? sedeSeleccionada;
        [ObservableProperty] private DateTime fechaDesde = DateTime.Today.AddMonths(-1);
        [ObservableProperty] private DateTime fechaHasta = DateTime.Today; // Incluye todo el día actual

        [ObservableProperty] private bool cargando = false;
        [ObservableProperty] private string? mensajeEstado;
        [ObservableProperty] private InscripcionEstadisticasDTO estadisticas = new();
        [ObservableProperty] private bool incluirTalleresEliminados = false;

        // Totales separados por tipo de taller
        [ObservableProperty] private decimal totalMontoTalleresActivos;
        [ObservableProperty] private decimal totalPagadoTalleresActivos;
        [ObservableProperty] private decimal totalSaldoTalleresActivos;

        [ObservableProperty] private decimal totalMontoTalleresEliminados;
        [ObservableProperty] private decimal totalPagadoTalleresEliminados;
        [ObservableProperty] private decimal totalSaldoTalleresEliminados;

        [ObservableProperty] private decimal totalMontoGeneral;
        [ObservableProperty] private decimal totalPagadoGeneral;
        [ObservableProperty] private decimal totalSaldoGeneral;

        public string TituloEncabezado { get; set; } = "Reporte de Inscripciones";

        public MenuReporteInscripcionesViewModel(
            IInscripcionReporteService inscripcionReporteService,
            ITallerService tallerService,
            ISedeService sedeService,
            IPromotorService promotorService,
            IGeneracionService generacionService,
            IExportacionService exportacionService)
        {
            _inscripcionReporteService = inscripcionReporteService;
            _tallerService = tallerService;
            _sedeService = sedeService;
            _promotorService = promotorService;
            _generacionService = generacionService;
            _exportacionService = exportacionService;

            // Cargar datos automáticamente al inicializar
            _ = Task.Run(async () => await CargarDatosAsync());
        }

        private async Task CargarDatosAsync()
        {
            try
            {
                Cargando = true;
                MensajeEstado = "Cargando datos...";

                // Cargar listas de filtros
                var talleresList = await _tallerService.ObtenerTalleresParaGridAsync(IncluirTalleresEliminados);
                Talleres = new ObservableCollection<TallerDTO>(talleresList);

                var promotoresList = await _promotorService.ObtenerPromotoresParaGridAsync();
                Promotores = new ObservableCollection<PromotorDTO>(promotoresList);

                // Cargar sedes
                var sedesList = await _sedeService.ObtenerSedesParaGridAsync();
                Sedes = new ObservableCollection<SedeDTO>(sedesList);

                // Cargar generaciones
                var generacionesList = await _generacionService.ObtenerGeneracionesParaGridAsync();
                Generaciones = new ObservableCollection<GeneracionDTO>(generacionesList);

                // Establecer la generación actual como predeterminada
                var generacionActual = _generacionService.ObtenerGeneracionActual();
                if (generacionActual != null)
                {
                    GeneracionSeleccionada = Generaciones.FirstOrDefault(g => g.Id == generacionActual.GeneracionId);
                }

                // Cargar inscripciones y estadísticas
                await CargarInscripcionesAsync();
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
        public async Task FiltrarAsync()
        {
            await CargarInscripcionesAsync();
        }

        [RelayCommand]
        public async Task LimpiarFiltrosAsync()
        {
            TallerSeleccionado = null;
            PromotorSeleccionado = null;
            GeneracionSeleccionada = null;
            SedeSeleccionada = null;
            FechaDesde = DateTime.Today.AddMonths(-1);
            FechaHasta = DateTime.Today; // Siempre hasta la fecha actual

            await CargarInscripcionesAsync();
        }

        [RelayCommand]
        public async Task ExportarAsync()
        {
            try
            {
                Cargando = true;
                MensajeEstado = "Exportando datos...";

                if (!Inscripciones.Any())
                {
                    MensajeEstado = "No hay datos para exportar";
                    return;
                }

                // Exportar como CSV por defecto
                var rutaArchivo = await _exportacionService.ExportarInscripcionesAsync(Inscripciones, "csv");
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

                if (!Inscripciones.Any())
                {
                    MensajeEstado = "No hay datos para exportar";
                    return;
                }

                var rutaArchivo = await _exportacionService.ExportarInscripcionesAsync(Inscripciones, "xlsx");
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

        private async Task CargarInscripcionesAsync()
        {
            try
            {
                Cargando = true;
                MensajeEstado = "Cargando inscripciones...";

                var inscripcionesList = await _inscripcionReporteService.ObtenerInscripcionesReporteAsync(
                    TallerSeleccionado?.Id,
                    PromotorSeleccionado?.Id,
                    GeneracionSeleccionada?.Id,
                    FechaDesde,
                    FechaHasta,
                    IncluirTalleresEliminados); // Ya está limitado a fecha actual en el servicio

                // Aplicar filtro por sede en memoria
                var inscripcionesFiltradas = inscripcionesList.AsEnumerable();
                if (SedeSeleccionada != null)
                {
                    inscripcionesFiltradas = inscripcionesFiltradas.Where(i => i.NombreSede == SedeSeleccionada.Nombre);
                }

                // Ordenar por estado de pago: pagadas primero, luego pendientes
                var inscripcionesOrdenadas = OrdenarPorEstadoPago(inscripcionesFiltradas);
                Inscripciones = new ObservableCollection<InscripcionReporteDTO>(inscripcionesOrdenadas);

                // Cargar estadísticas
                var estadisticasData = await _inscripcionReporteService.ObtenerEstadisticasInscripcionesAsync(
                    TallerSeleccionado?.Id,
                    GeneracionSeleccionada?.Id,
                    FechaDesde,
                    FechaHasta,
                    IncluirTalleresEliminados);

                Estadisticas = estadisticasData;

                MensajeEstado = $"Mostrando {Inscripciones.Count} inscripciones";
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al cargar inscripciones: {ex.Message}";
            }
            finally
            {
                Cargando = false;
            }
        }

        // Propiedades calculadas para estadísticas rápidas
        public int TotalInscripciones => Inscripciones.Count;
        public int InscripcionesActivas => Inscripciones.Count(i => i.Estado == "Pendiente");
        public int InscripcionesCanceladas => Inscripciones.Count(i => i.Estado == "Cancelada");
        public int InscripcionesPagadas => Inscripciones.Count(i => i.Estado == "Pagada");
        public decimal MontoTotalInscripciones => Inscripciones.Sum(i => i.Costo);
        public decimal MontoTotalRecaudado => Inscripciones.Sum(i => i.Costo - i.SaldoActual);
        public decimal MontoTotalPendiente => Inscripciones.Where(i => !i.TallerEliminado).Sum(i => i.SaldoActual);

        // Método para actualizar estadísticas cuando cambia la colección
        partial void OnInscripcionesChanged(ObservableCollection<InscripcionReporteDTO> value)
        {
            OnPropertyChanged(nameof(TotalInscripciones));
            OnPropertyChanged(nameof(InscripcionesActivas));
            OnPropertyChanged(nameof(InscripcionesCanceladas));
            OnPropertyChanged(nameof(InscripcionesPagadas));
            OnPropertyChanged(nameof(MontoTotalInscripciones));
            OnPropertyChanged(nameof(MontoTotalRecaudado));
            OnPropertyChanged(nameof(MontoTotalPendiente));

            // Recalcular totales separados
            RecalcularTotalesSeparados();
        }

        private void RecalcularTotalesSeparados()
        {
            decimal montoActivos = 0, pagadoActivos = 0, saldoActivos = 0;
            decimal montoEliminados = 0, pagadoEliminados = 0, saldoEliminados = 0;
            decimal montoGeneral = 0, pagadoGeneral = 0, saldoGeneral = 0;

            foreach (var inscripcion in Inscripciones)
            {
                // Totales generales (todo lo que se muestra)
                montoGeneral += inscripcion.Costo;
                pagadoGeneral += inscripcion.Costo - inscripcion.SaldoActual;
                saldoGeneral += inscripcion.SaldoActual;

                // Totales separados por tipo de taller
                if (inscripcion.TallerEliminado)
                {
                    montoEliminados += inscripcion.Costo;
                    pagadoEliminados += inscripcion.Costo - inscripcion.SaldoActual;
                    saldoEliminados += inscripcion.SaldoActual;
                }
                else
                {
                    montoActivos += inscripcion.Costo;
                    pagadoActivos += inscripcion.Costo - inscripcion.SaldoActual;
                    saldoActivos += inscripcion.SaldoActual;
                }
            }

            // Actualizar propiedades
            TotalMontoTalleresActivos = montoActivos;
            TotalPagadoTalleresActivos = pagadoActivos;
            TotalSaldoTalleresActivos = saldoActivos;

            TotalMontoTalleresEliminados = montoEliminados;
            TotalPagadoTalleresEliminados = pagadoEliminados;
            TotalSaldoTalleresEliminados = saldoEliminados;

            TotalMontoGeneral = montoGeneral;
            TotalPagadoGeneral = pagadoGeneral;
            TotalSaldoGeneral = saldoGeneral;
        }

        // Métodos para actualizar automáticamente cuando cambian las fechas
        partial void OnFechaDesdeChanged(DateTime value)
        {
            // Si la fecha desde es mayor que la fecha hasta, ajustar la fecha hasta
            if (value > FechaHasta)
            {
                FechaHasta = value;
            }

            // Recargar datos automáticamente
            _ = Task.Run(async () => await CargarInscripcionesAsync());
        }

        partial void OnFechaHastaChanged(DateTime value)
        {
            // Si la fecha hasta es menor que la fecha desde, ajustar la fecha desde
            if (value < FechaDesde)
            {
                FechaDesde = value;
            }

            // Recargar datos automáticamente
            _ = Task.Run(async () => await CargarInscripcionesAsync());
        }

        // Método para manejar el cambio del checkbox de talleres eliminados
        partial void OnIncluirTalleresEliminadosChanged(bool value)
        {
            // Recargar talleres y datos cuando cambie el filtro
            _ = Task.Run(async () => await RecargarTalleresYInscripcionesAsync());
        }

        // Método para manejar el cambio de sede
        partial void OnSedeSeleccionadaChanged(SedeDTO? value)
        {
            // Recargar datos cuando cambie la sede
            _ = Task.Run(async () => await CargarInscripcionesAsync());
        }

        private async Task RecargarTalleresYInscripcionesAsync()
        {
            try
            {
                Cargando = true;
                MensajeEstado = "Actualizando filtros...";

                // Recargar talleres con el nuevo filtro
                var talleresList = await _tallerService.ObtenerTalleresParaGridAsync(IncluirTalleresEliminados);
                Talleres = new ObservableCollection<TallerDTO>(talleresList);

                // Limpiar selección de taller si estaba seleccionado un taller eliminado
                if (TallerSeleccionado != null && TallerSeleccionado.Eliminado && !IncluirTalleresEliminados)
                {
                    TallerSeleccionado = null;
                }

                // Recargar inscripciones
                await CargarInscripcionesAsync();
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al actualizar filtros: {ex.Message}";
            }
            finally
            {
                Cargando = false;
            }
        }

        /// <summary>
        /// Ordena las inscripciones por estado de pago: Pagadas → Parciales (por progreso) → Sin Pagos → Canceladas (al final)
        /// </summary>
        private IEnumerable<InscripcionReporteDTO> OrdenarPorEstadoPago(IEnumerable<InscripcionReporteDTO> inscripciones)
        {
            return inscripciones.OrderBy(i => i.Estado == "Cancelada" ? 1 : 0) // Canceladas al final (1 va después de 0)
                               .ThenByDescending(i => i.SaldoActual == 0) // Pagadas primero (SaldoActual = 0)
                               .ThenByDescending(i => i.MontoPagado > 0) // Luego parciales (MontoPagado > 0 pero SaldoActual > 0)
                               .ThenByDescending(i => i.ProgresoPorcentaje) // Parciales por progreso (mayor a menor)
                               .ThenBy(i => i.NombreAlumno); // Finalmente por nombre de alumno
        }

        [RelayCommand]
        public void ReordenarPorEstadoPago()
        {
            if (Inscripciones.Any())
            {
                var inscripcionesOrdenadas = OrdenarPorEstadoPago(Inscripciones);
                Inscripciones = new ObservableCollection<InscripcionReporteDTO>(inscripcionesOrdenadas);
                MensajeEstado = "Inscripciones reordenadas por estado de pago (Pagadas → Parciales por progreso → Sin Pagos → Canceladas al final)";
            }
        }
    }
}
