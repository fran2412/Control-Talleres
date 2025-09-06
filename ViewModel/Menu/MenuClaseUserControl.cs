using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Clases;
using ControlTalleresMVP.Services.Configuracion;
using ControlTalleresMVP.Services.Inscripciones;
using ControlTalleresMVP.Services.Picker;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data; // ICollectionView
using System.ComponentModel; // para ICollectionView events

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuClaseUserControl : ObservableObject
    {
        private readonly IClaseService _claseService;
        private readonly IInscripcionService _inscripcionService;
        private readonly IDialogService _dialogService;
        private readonly IConfiguracionService _configuracionService;
        private readonly IAlumnoPickerService _alumnoPicker;

        // Límites y control de recursión
        private const int MaxClases = 4;
        private const decimal MinCostoClase = 1m;
        private const decimal MaxCostoClase = 10000m;

        private bool _ajustandoCantidad;
        private bool _ajustandoCosto;
        private bool _ajustandoMonto;

        public MenuClaseUserControl(
            IClaseService claseService,
            IInscripcionService inscripcionService,
            IDialogService dialogService,
            IConfiguracionService configuracionService,
            IAlumnoPickerService alumnoPicker)
        {
            _claseService = claseService;
            _dialogService = dialogService;
            _alumnoPicker = alumnoPicker;
            _configuracionService = configuracionService;
            _inscripcionService = inscripcionService;

            FechaDeHoy = DateTime.Now.ToString("dd/MM/yyyy");

            // Inicializa costo y monta el sugerido + ingresado
            var costoCfg = _configuracionService.GetValor<int>("costo_clase", 150);
            CostoClase = ClampCosto(R2(costoCfg));
            RecalcularSugeridoYQuizasMonto();

            // ====== Inicialización del REGISTRO (grid financiero) ======
            FechaHastaRegistros = DateTime.Today;
            FechaDesdeRegistros = DateTime.Today.AddMonths(-1);
            InicializarVistaRegistros();
            _ = CargarRegistrosClasesAsync(); // primera carga
        }

        // ====================
        // PROPIEDADES BINDING (COBRO)
        // ====================

        [ObservableProperty]
        private string fechaDeHoy;

        [ObservableProperty]
        private string? alumnoNombre;

        [ObservableProperty]
        private Alumno? alumnoSeleccionado;

        [ObservableProperty]
        private ObservableCollection<Taller> talleresDelAlumno = new();

        [ObservableProperty]
        private Taller? tallerSeleccionado;

        [ObservableProperty]
        private int cantidadSeleccionada = 1;
        partial void OnCantidadSeleccionadaChanged(int value)
        {
            // Clamp real: 1..MaxClases
            var clamped = value < 1 ? 1 : (value > MaxClases ? MaxClases : value);
            if (clamped != value)
            {
                if (_ajustandoCantidad) return;
                _ajustandoCantidad = true;
                CantidadSeleccionada = clamped;
                _ajustandoCantidad = false;
                return;
            }

            RecalcularSugeridoYQuizasMonto();
        }

        [ObservableProperty]
        private decimal costoClase;
        partial void OnCostoClaseChanged(decimal value)
        {
            var clamped = ClampCosto(R2(value));
            if (clamped != value)
            {
                if (_ajustandoCosto) return;
                _ajustandoCosto = true;
                CostoClase = clamped;
                _ajustandoCosto = false;
                return;
            }

            RecalcularSugeridoYQuizasMonto();
        }

        [ObservableProperty]
        private decimal? montoIngresado;
        partial void OnMontoIngresadoChanged(decimal? value)
        {
            if (_ajustandoMonto) return;
            if (value is null) return;

            var v = R2(value.Value);
            // Clamp: 0..MontoSugerido
            if (v < 0m) v = 0m;
            if (MontoSugerido > 0m && v > MontoSugerido) v = MontoSugerido;

            if (MontoIngresado != v)
            {
                _ajustandoMonto = true;
                MontoIngresado = v;
                _ajustandoMonto = false;
            }
        }

        [ObservableProperty]
        private decimal montoSugerido;

        [ObservableProperty]
        private string? mensajeValidacion;

        // ====================
        // REGISTRO FINANCIERO (GRID)
        // ====================

        // Fuente para el DataGrid
        public ObservableCollection<ClaseFinancieraDTO> RegistrosClases { get; } = new();
        public ICollectionView? RegistrosClasesView { get; private set; }

        // Filtros
        [ObservableProperty] private DateTime? fechaDesdeRegistros;
        [ObservableProperty] private DateTime? fechaHastaRegistros;

        private string _filtroRegistrosClases = string.Empty;
        public string FiltroRegistrosClases
        {
            get => _filtroRegistrosClases;
            set
            {
                if (SetProperty(ref _filtroRegistrosClases, value))
                    RegistrosClasesView?.Refresh();
            }
        }

        // Totales del grid
        [ObservableProperty] private decimal totalMontoClases;
        [ObservableProperty] private decimal totalPagadoClases;
        [ObservableProperty] private decimal totalSaldoClases;

        // ====================
        // COMANDOS (COBRO)
        // ====================

        [RelayCommand]
        private async Task BuscarAlumno()
        {
            var alumno = _alumnoPicker.Pick();
            if (alumno is null) return;

            var inscripciones = await _inscripcionService.ObtenerInscripcionesAsync(alumno.AlumnoId);

            var talleresDisponibles = new ObservableCollection<Taller>(
                inscripciones
                    .Where(i => i.Taller != null)
                    .Select(i => i.Taller!)
                    .GroupBy(t => t.TallerId)
                    .Select(g => g.First())
                    .ToList()
            );

            if (talleresDisponibles.Count == 0)
            {
                _dialogService.Alerta("Este alumno no está inscrito en ningún taller.\n" +
                    "Inscríbalo en alguno para poder registrar sus pagos a clases.");
                return;
            }

            // Set bindings básicos
            TalleresDelAlumno = talleresDisponibles;
            AlumnoSeleccionado = alumno;
            AlumnoNombre = alumno.Nombre;

            // Verificar pagos de HOY
            var ids = talleresDisponibles.Select(t => t.TallerId).ToArray();
            var estados = await _claseService.ObtenerEstadoPagoHoyAsync(
                alumno.AlumnoId, ids, DateTime.Today);

            // ¿Todas pagadas?
            var todosPagados = estados.Length > 0 && estados.All(e => e.EstaPagada);

            if (todosPagados)
            {
                var lista = string.Join("\n • ",
                    talleresDisponibles.Select(t => t.Nombre));

                _dialogService.Alerta(
                    "El alumno ya tiene pagada la clase de HOY en todos sus talleres:\n" +
                    $"• {lista}");

                // Aun así, cargamos el registro para que vea su historial
                await CargarRegistrosClasesAsync();
                return;
            }

            // Si hay al menos un taller donde se puede pagar hoy, NO mostrar alerta.
            // Preselecciona el primero disponible para pagar.
            var disponibles = estados.Where(e => e.PuedePagar)
                                     .Select(e => e.TallerId)
                                     .ToHashSet();

            var primerDisponible = TalleresDelAlumno.FirstOrDefault(t => disponibles.Contains(t.TallerId))
                                   ?? TalleresDelAlumno.First();

            TallerSeleccionado = primerDisponible;

            // Actualiza el grid según el alumno elegido
            await CargarRegistrosClasesAsync();
        }

        [RelayCommand]
        private void LimpiarSeleccion()
        {
            AlumnoSeleccionado = null;
            AlumnoNombre = string.Empty;
            TallerSeleccionado = null;
            TalleresDelAlumno = new();

            // Reinicia cantidad y montos
            if (!_ajustandoCantidad)
            {
                _ajustandoCantidad = true;
                CantidadSeleccionada = 1;
                _ajustandoCantidad = false;
            }

            // Recalcula y fija al sugerido
            RecalcularSugeridoYQuizasMonto(forceSetMonto: true);
            MensajeValidacion = null;

            // También limpia filtros del grid y recarga
            _ = LimpiarFiltrosRegistrosAsync();
        }

        [RelayCommand]
        private void UsarMontoSugerido()
        {
            _ajustandoMonto = true;
            MontoIngresado = MontoSugerido;
            _ajustandoMonto = false;
        }

        [RelayCommand]
        private async Task GuardarPagoClases(CancellationToken ct)
        {
            try
            {
                MensajeValidacion = null;

                if (AlumnoSeleccionado is null)
                {
                    MensajeValidacion = "Debes seleccionar un alumno.";
                    return;
                }

                if (TallerSeleccionado is null)
                {
                    MensajeValidacion = "Debes seleccionar un taller.";
                    return;
                }

                if (MontoIngresado is null || MontoIngresado <= 0)
                {
                    MensajeValidacion = "El monto debe ser mayor a 0.";
                    return;
                }

                if (CantidadSeleccionada < 1)
                {
                    MensajeValidacion = "La cantidad de clases debe ser al menos 1.";
                    return;
                }

                // --- Cálculos ---
                var costo = Math.Round(CostoClase, 2, MidpointRounding.AwayFromZero);
                var totalIngresado = Math.Round(MontoIngresado.Value, 2, MidpointRounding.AwayFromZero);

                var clasesCompletas = (int)Math.Min(
                    CantidadSeleccionada,
                    Math.Truncate(totalIngresado / (costo == 0 ? 1 : costo))
                );

                var sobrante = totalIngresado - (clasesCompletas * costo);

                // Generar las fechas necesarias (hoy + semanas)
                var cantidadFechas = Math.Max(
                    clasesCompletas + (clasesCompletas > 0 && sobrante > 0m && clasesCompletas < CantidadSeleccionada ? 1 : 0),
                    1
                );

                var fechas = GenerarFechasSemanas(DateTime.Today, cantidadFechas);

                // --- Tracking ---
                var resultados = new System.Collections.Generic.List<RegistrarClaseResult>();

                // 1) Clases completas
                for (int i = 0; i < clasesCompletas; i++)
                {
                    var r = await _claseService.RegistrarClaseAsync(
                        AlumnoSeleccionado.AlumnoId,
                        TallerSeleccionado.TallerId,
                        fechas[i],
                        costo,
                        ct);
                    resultados.Add(r);
                }

                // 2) Parcial (si aplica y hubo al menos una completa)
                if (clasesCompletas > 0 && sobrante > 0m && clasesCompletas < CantidadSeleccionada)
                {
                    var abono = Math.Round(sobrante, 2, MidpointRounding.AwayFromZero);
                    var r = await _claseService.RegistrarClaseAsync(
                        AlumnoSeleccionado.AlumnoId,
                        TallerSeleccionado.TallerId,
                        fechas[clasesCompletas],
                        abono,
                        ct);
                    resultados.Add(r);
                }
                // 3) Solo abono (cuando no alcanza ni para una completa)
                else if (clasesCompletas == 0 && totalIngresado > 0m)
                {
                    var r = await _claseService.RegistrarClaseAsync(
                        AlumnoSeleccionado.AlumnoId,
                        TallerSeleccionado.TallerId,
                        fechas[0],
                        totalIngresado,
                        ct);
                    resultados.Add(r);
                }

                // --- Mensajes a partir de resultados reales ---
                string F(DateTime d) => d.ToString("dd/MM/yyyy");

                bool huboCambios = resultados.Any(r => r.PagoCreado || r.CargoCreado || r.ClaseCreada);
                if (!huboCambios)
                {
                    if (resultados.Count > 0 && resultados.All(r => r.CargoYaPagado))
                        _dialogService.Alerta("No se registró ningún movimiento: la(s) clase(s) ya estaban pagadas.");
                    else
                        _dialogService.Alerta("No se registró ningún movimiento.");
                    return;
                }

                var pagadasFull = resultados
                    .Where(r => r.PagoCreado && r.MontoAplicado >= costo)
                    .Select(r => r.Fecha)
                    .OrderBy(d => d)
                    .ToList();

                var parciales = resultados
                    .Where(r => r.PagoCreado && r.MontoAplicado > 0m && r.MontoAplicado < costo)
                    .Select(r => new { r.Fecha, r.MontoAplicado })
                    .OrderBy(x => x.Fecha)
                    .ToList();

                if (pagadasFull.Count > 0 && parciales.Count > 0)
                {
                    var listado = string.Join(", ", pagadasFull.Select(F));
                    var p = parciales.Last();
                    _dialogService.Info(
                        $"Se registraron {pagadasFull.Count} clase(s) pagada(s) ({listado}) " +
                        $"y un abono de {p.MontoAplicado:0.00} para la clase de {F(p.Fecha)}.",
                        "Clases");
                }
                else if (pagadasFull.Count == 1 && parciales.Count == 0)
                {
                    _dialogService.Info($"Se registró la clase de {F(pagadasFull[0])} pagada por completo.", "Clases");
                }
                else if (pagadasFull.Count > 1 && parciales.Count == 0)
                {
                    var listado = string.Join(", ", pagadasFull.Select(F));
                    _dialogService.Info($"Se registraron {pagadasFull.Count} clase(s) pagada(s): {listado}.", "Clases");
                }
                else if (pagadasFull.Count == 0 && parciales.Count > 0)
                {
                    var p = parciales.Last();
                    _dialogService.Info($"Se registró un abono de {p.MontoAplicado:0.00} para la clase de {F(p.Fecha)}.", "Clases");
                }

                // Recarga el grid con los últimos cambios
                await CargarRegistrosClasesAsync();

                LimpiarSeleccion();
            }
            catch (Exception ex)
            {
                _dialogService.Error($"Error al registrar el pago: {ex.Message}", "Clases");
            }
        }

        [RelayCommand]
        private void CancelarPagoClases()
        {
            if (_dialogService.Confirmar("¿Está seguro de cancelar el registro del pago?", "Clases"))
            {
                LimpiarSeleccion();
                _dialogService.Info("Registro cancelado.", "Clases");
            }
        }

        // ====================
        // COMANDOS (REGISTRO / GRID)
        // ====================

        [RelayCommand]
        private async Task BuscarAlumnoRegistrosAsync()
        {
            var alumno = _alumnoPicker.Pick();
            if (alumno is null) return;

            AlumnoSeleccionado = alumno; // reutilizamos la misma selección
            AlumnoNombre = alumno.Nombre;
            await CargarRegistrosClasesAsync();
        }

        [RelayCommand]
        private async Task LimpiarFiltrosRegistrosAsync()
        {
            // No tocamos la selección del alumno del flujo de cobro
            FechaHastaRegistros = DateTime.Today;
            FechaDesdeRegistros = DateTime.Today.AddMonths(-1);
            FiltroRegistrosClases = string.Empty;

            await CargarRegistrosClasesAsync();
        }

        [RelayCommand]
        private async Task CargarRegistrosClasesAsync(CancellationToken ct = default)
        {
            try
            {
                var datos = await _claseService.ObtenerClasesFinancierasAsync(
                    alumnoId: AlumnoSeleccionado?.AlumnoId,   // si hay alumno seleccionado, filtra
                    tallerId: null,
                    desde: FechaDesdeRegistros,
                    hasta: FechaHastaRegistros,
                    ct: ct);

                RegistrosClases.Clear();
                foreach (var r in datos)
                    RegistrosClases.Add(r);

                RecalcularTotalesRegistrosDesdeVista();
            }
            catch (Exception ex)
            {
                _dialogService.Error("No fue posible cargar el registro de clases.\n" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task CancelarClaseAsync(int claseId)
        {
            if (!_dialogService.Confirmar("¿Seguro que deseas cancelar esta clase?")) return;

            try
            {
                await _claseService.CancelarAsync(claseId);
                await CargarRegistrosClasesAsync();
                _dialogService.Info("Clase cancelada correctamente.");
            }
            catch (Exception ex)
            {
                _dialogService.Error("No se pudo cancelar la clase.\n" + ex.Message);
            }
        }

        // ====================
        // Helpers (validación, grid, cálculo)
        // ====================

        private (bool ok, string? msg) Validar()
        {
            if (AlumnoSeleccionado is null)
                return (false, "Debes seleccionar un alumno.");

            if (TallerSeleccionado is null)
                return (false, "Debes seleccionar un taller.");

            if (CantidadSeleccionada < 1 || CantidadSeleccionada > MaxClases)
                return (false, $"La cantidad debe estar entre 1 y {MaxClases}.");

            if (CostoClase < MinCostoClase || CostoClase > MaxCostoClase)
                return (false, $"El costo de la clase debe estar entre {MinCostoClase:0.##} y {MaxCostoClase:0.##}.");

            if (MontoSugerido <= 0)
                return (false, "El monto sugerido no es válido.");

            if (!MontoIngresado.HasValue || MontoIngresado <= 0)
                return (false, "El monto ingresado debe ser mayor a 0.");

            if (MontoIngresado > MontoSugerido)
                return (false, "El monto ingresado no puede exceder el monto sugerido.");

            return (true, null);
        }

        private void RecalcularSugeridoYQuizasMonto(bool forceSetMonto = false)
        {
            var nuevoSugerido = R2(CantidadSeleccionada * CostoClase);

            if (MontoSugerido != nuevoSugerido)
                MontoSugerido = nuevoSugerido;

            // Si no hay monto, si nos pasamos, o si se fuerza, alinear al sugerido
            if (forceSetMonto || !MontoIngresado.HasValue || MontoIngresado > MontoSugerido)
            {
                if (!_ajustandoMonto)
                {
                    _ajustandoMonto = true;
                    MontoIngresado = MontoSugerido;
                    _ajustandoMonto = false;
                }
            }
            else
            {
                // Normaliza
                if (MontoIngresado.HasValue)
                {
                    var norm = MontoIngresado.Value;
                    if (norm < 0m) norm = 0m;
                    norm = R2(norm);

                    if (!_ajustandoMonto && norm != MontoIngresado.Value)
                    {
                        _ajustandoMonto = true;
                        MontoIngresado = norm;
                        _ajustandoMonto = false;
                    }
                }
            }
        }

        // ==== Vista del grid ====
        private void InicializarVistaRegistros()
        {
            RegistrosClasesView = CollectionViewSource.GetDefaultView(RegistrosClases);
            RegistrosClasesView.Filter = FiltroRegistrosPredicate;
            RegistrosClasesView.CurrentChanged += (_, __) => RecalcularTotalesRegistrosDesdeVista();
        }

        private bool FiltroRegistrosPredicate(object o)
        {
            if (o is not ClaseFinancieraDTO dto) return false;

            // Filtro de texto (Alumno, Taller, Estado)
            if (!string.IsNullOrWhiteSpace(FiltroRegistrosClases))
            {
                var f = FiltroRegistrosClases.Trim();
                if (!((dto.AlumnoNombre?.Contains(f, StringComparison.OrdinalIgnoreCase) == true)
                    || (dto.TallerNombre?.Contains(f, StringComparison.OrdinalIgnoreCase) == true)
                    || dto.EstadoTexto.Contains(f, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            // Rango de fechas (opcional)
            if (FechaDesdeRegistros.HasValue && dto.FechaClase.Date < FechaDesdeRegistros.Value.Date)
                return false;
            if (FechaHastaRegistros.HasValue && dto.FechaClase.Date > FechaHastaRegistros.Value.Date)
                return false;

            // Si hay AlumnoSeleccionado, normalmente la carga ya viene filtrada por servicio,
            // pero si quieres filtrar en cliente, descomenta:
            // if (AlumnoSeleccionado != null && dto.AlumnoId != AlumnoSeleccionado.AlumnoId) return false;

            return true;
        }

        private void RecalcularTotalesRegistrosDesdeVista()
        {
            if (RegistrosClasesView is null) return;

            decimal monto = 0, pagado = 0, saldo = 0;
            foreach (var item in RegistrosClasesView)
            {
                if (item is ClaseFinancieraDTO r)
                {
                    monto += r.Monto;
                    pagado += r.MontoPagado;
                    saldo += r.SaldoActual;
                }
            }

            TotalMontoClases = monto;
            TotalPagadoClases = pagado;
            TotalSaldoClases = saldo;
        }

        // ==== Utilidades ====
        private static DateTime[] GenerarFechasSemanas(DateTime inicio, int cantidad)
        {
            if (cantidad < 1) return Array.Empty<DateTime>();
            var arr = new DateTime[cantidad];
            for (int i = 0; i < cantidad; i++)
                arr[i] = inicio.AddDays(7 * i);
            return arr;
        }

        private static decimal R2(decimal v) =>
            Math.Round(v, 2, MidpointRounding.AwayFromZero);

        private static decimal ClampCosto(decimal v)
        {
            if (v < MinCostoClase) return MinCostoClase;
            if (v > MaxCostoClase) return MaxCostoClase;
            return v;
        }
    }
}
