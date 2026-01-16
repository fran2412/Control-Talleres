using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Helpers.Commands;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Alumnos;
using ControlTalleresMVP.Services.Configuracion;
using ControlTalleresMVP.Services.Inscripciones;
using ControlTalleresMVP.Services.Promotores;
using ControlTalleresMVP.Services.Sesion;
using ControlTalleresMVP.Services.Talleres;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuAlumnosViewModel : BaseMenuViewModel<AlumnoDTO, Alumno, IAlumnoService>
    {
        public string TituloEncabezado { get; set; } = "Gestión de alumnos";

        public override ObservableCollection<AlumnoDTO> Registros
            => _itemService.RegistrosAlumnos;

        public ObservableCollection<TallerInscripcionDTO> TalleresDisponibles { get; }

        [ObservableProperty] private string campoTextoNombre = "";
        [ObservableProperty] private string campoTextTelefono = "";

        [ObservableProperty] private int? promotorSeleccionadoId;
        [ObservableProperty] private bool inscribirEnTaller;
        [ObservableProperty] private decimal descuentoPorClase;

        private readonly IPromotorService _promotorService;
        private readonly ISesionService _sesionService;
        private readonly IConfiguracionService _configuracionService;
        private readonly ITallerService _tallerService;

        public MenuAlumnosViewModel(IAlumnoService itemService, IDialogService dialogService, IPromotorService promotorService, ISesionService sesionService, ITallerService tallerService, IConfiguracionService configuracionService)
            : base(itemService, dialogService)
        {
            _configuracionService = configuracionService;
            _promotorService = promotorService;
            _sesionService = sesionService;
            _tallerService = tallerService;

            OpcionesPromotor = new ObservableCollection<Promotor>(_promotorService.ObtenerTodos());

            TalleresDisponibles = new ObservableCollection<TallerInscripcionDTO>();


            itemService.InicializarRegistros();
            InicializarVista();

            CargarTalleresDesdeBd();

        }

        public decimal TotalCostos =>
            TalleresDisponibles?.Where(t => t.EstaSeleccionado).Sum(t => t.Costo) ?? 0;

        public decimal TotalAbonado =>
            TalleresDisponibles?.Where(t => t.EstaSeleccionado).Sum(t => t.Abono) ?? 0;

        public decimal SaldoPendienteTotal =>
            TalleresDisponibles?.Where(t => t.EstaSeleccionado).Sum(t => t.SaldoPendiente) ?? 0;



        public ObservableCollection<Promotor> OpcionesPromotor { get; }

        private bool _ocultarBecadosEnPicker;
        private decimal _costoClaseActual;

        public bool OcultarBecadosEnPicker
        {
            get => _ocultarBecadosEnPicker;
            set
            {
                if (SetProperty(ref _ocultarBecadosEnPicker, value))
                {
                    // Actualizar el costo de clase cuando se activa el filtro
                    if (value)
                    {
                        _costoClaseActual = _configuracionService.GetValorSede<int>("costo_clase", 150);
                    }
                    RegistrosView?.Refresh();
                }
            }
        }

        protected override async Task EliminarAsync(AlumnoDTO? alumnoSeleccionado)
        {
            if (alumnoSeleccionado == null) return;

            if (!_dialogService.Confirmar($"¿Está seguro de eliminar el alumno {alumnoSeleccionado.Nombre}?")) return;

            try
            {
                await _itemService.EliminarAsync(alumnoSeleccionado.Id);
                _dialogService.Info("Alumno eliminado correctamente");
            }
            catch (Exception ex)
            {
                _dialogService.Error("Error al eliminar el alumno.\n" + ex.Message);
            }
        }

        [RelayCommand]
        protected override async Task RegistrarItemAsync()
        {
            if (!ValidarYConfirmarRegistro()) return;

            try
            {
                // capturamos el alumno guardado para obtener AlumnoId
                var alumnoGuardado = await _itemService.GuardarAsync(new Alumno
                {
                    Nombre = CampoTextoNombre.Trim(),
                    Telefono = CampoTextTelefono?.Trim(),
                    SedeId = _sesionService.ObtenerIdSede(),
                    PromotorId = PromotorSeleccionadoId,
                    DescuentoPorClase = DescuentoPorClase
                });

                // Procesar inscripciones si está marcado
                if (InscribirEnTaller)
                {
                    var inscripcionesExitosas = await ProcesarInscripcionesTalleres(alumnoGuardado.AlumnoId);
                    if (!inscripcionesExitosas) return;

                    // Preguntar si desea registrar el pago de las clases del alumno
                    if (_dialogService.Confirmar(
                        "¿Desea registrar el pago de las clases del alumno?\n\n" +
                        "Si selecciona 'Sí', se abrirá el formulario de pagos de clases con este alumno preseleccionado.\n" +
                        "Si selecciona 'No', solo se registrará la inscripción.",
                        "¿Registrar pago de clases?"))
                    {
                        await AbrirFormularioPagoClasesConAlumno(alumnoGuardado.AlumnoId, alumnoGuardado.Nombre);

                        return;
                    }
                }

                LimpiarCampos();
                _dialogService.Info("Alumno registrado correctamente");
            }
            catch (Exception ex)
            {
                _dialogService.Error("Error al registrar el alumno.\n" + ex.Message);
            }
        }

        private async Task<bool> ProcesarInscripcionesTalleres(int alumnoId)
        {
            var seleccionados = TalleresDisponibles?.Where(t => t.EstaSeleccionado).ToList();
            if (seleccionados == null || seleccionados.Count == 0)
            {
                _dialogService.Alerta("Marcaste 'Inscribir en taller', pero no seleccionaste ningún taller.");
                return false;
            }

            var errores = new List<string>();
            int inscripcionesOk = 0;

            foreach (var taller in seleccionados)
            {
                try
                {
                    var abono = ObtenerAbonoValido(taller.Abono);

                    using var scope = App.ServiceProvider!.CreateScope();
                    var inscripcionService = scope.ServiceProvider.GetRequiredService<IInscripcionService>();

                    await inscripcionService.InscribirAsync(
                        alumnoId: alumnoId,
                        tallerId: taller.TallerId,
                        abonoInicial: abono
                    );

                    inscripcionesOk += 1;
                }

                catch (Exception ex)
                {
                    errores.Add($"Taller {taller.Nombre ?? taller.TallerId.ToString()}: {ex.Message}");
                }
            }

            if (inscripcionesOk > 0)
            {
                _dialogService.Info($"Se registraron {inscripcionesOk} inscripción(es).");
            }

            if (errores.Count > 0)
            {
                _dialogService.Alerta("Algunas inscripciones no se pudieron procesar:\n" + string.Join("\n", errores));
            }

            return inscripcionesOk > 0;
        }

        public override bool Filtro(object o)
        {
            if (o is not AlumnoDTO a)
                return false;

            // Ocultar alumnos "becados" (descuento >= costo de clase) cuando está activo el filtro
            if (OcultarBecadosEnPicker && _costoClaseActual > 0 && a.DescuentoPorClase >= _costoClaseActual)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(FiltroRegistros))
            {
                return true;
            }

            var nombreCompleto = a.Nombre ?? "";
            var telefono = a.Telefono ?? "";
            var telSoloDigitos = new string(telefono.Where(char.IsDigit).ToArray());

            var haystack = Normalizar($"{nombreCompleto} {telefono} {telSoloDigitos}");

            var tokens = FiltroRegistros
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Normalizar);

            foreach (var token in tokens)
            {
                if (!haystack.Contains(token))
                    return false;
            }

            return true;
        }

        protected override void LimpiarCampos()
        {
            CampoTextoNombre = "";
            CampoTextTelefono = "";
            PromotorSeleccionadoId = null;
            DescuentoPorClase = 0m;

            if (TalleresDisponibles != null)
            {
                foreach (var taller in TalleresDisponibles)
                {
                    taller.EstaSeleccionado = false;
                    taller.Abono = 0;
                }
            }

        }

        protected override async Task ActualizarAsync(AlumnoDTO? alumnoSeleccionado)
        {
            await Task.CompletedTask;
            _dialogService.Info(alumnoSeleccionado!.Nombre);
        }

        private void CargarTalleresDesdeBd()
        {
            try
            {
                // Desuscribir eventos ANTES de limpiar para evitar memory leak
                DesuscribirEventosTalleres();
                TalleresDisponibles.Clear();

                var costoDefault = _configuracionService.GetValorSede<int>("costo_inscripcion", 600);
                var talleres = _tallerService.ObtenerTalleresParaInscripcion(costoDefault);

                foreach (var item in talleres)
                {
                    item.PropertyChanged += OnTallerPropertyChanged!;
                    TalleresDisponibles.Add(item);
                }

                ActualizarTotales();
            }
            catch (Exception ex)
            {
                _dialogService.Error("No se pudieron cargar los talleres.\n" + ex.Message);
            }
        }

        private void OnTallerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TallerInscripcionDTO.Abono) ||
                e.PropertyName == nameof(TallerInscripcionDTO.SaldoPendiente) ||
                e.PropertyName == nameof(TallerInscripcionDTO.EstaSeleccionado))
            {
                ActualizarTotales();
            }
        }

        private async Task AbrirFormularioPagoClasesConAlumno(int alumnoId, string nombreAlumno)
        {
            try
            {
                var navigatorService = App.ServiceProvider?.GetRequiredService<INavigatorService>();
                if (navigatorService == null) return;

                navigatorService.NavigateTo<MenuClaseUserControl>();

                await Task.Delay(100);

                var claseUserControl = navigatorService.CurrentViewModel as MenuClaseUserControl;
                if (claseUserControl?.MenuClaseCobroVM != null)
                {
                    var alumno = new Alumno { AlumnoId = alumnoId, Nombre = nombreAlumno };
                    await claseUserControl.MenuClaseCobroVM.BuscarAlumnoConAlumno(alumno);
                }
            }
            catch (Exception ex)
            {
                _dialogService.Error($"Error al abrir el formulario de pagos de clases: {ex.Message}");
            }
        }

        private bool ValidarYConfirmarRegistro()
        {
            if (string.IsNullOrWhiteSpace(CampoTextoNombre))
            {
                _dialogService.Alerta("El nombre del alumno es obligatorio");
                return false;
            }

            if (DescuentoPorClase < 0)
            {
                _dialogService.Alerta("El descuento por clase no puede ser negativo.");
                return false;
            }

            var costoClase = Math.Max(1, _configuracionService.GetValorSede<int>("costo_clase", 150));

            if (DescuentoPorClase > costoClase)
            {
                _dialogService.Alerta($"El descuento por clase no puede ser mayor a {costoClase:C} (costo de la clase).");
                return false;
            }

            return _dialogService.Confirmar(
                $"¿Confirma que desea registrar al alumno: {CampoTextoNombre}?");
        }


        private static decimal ObtenerAbonoValido(decimal abono)
        {
            return abono is decimal valorAbono && valorAbono > 0 ? valorAbono : 0m;
        }

        private void ActualizarTotales()
        {
            OnPropertyChanged(nameof(TotalCostos));
            OnPropertyChanged(nameof(TotalAbonado));
            OnPropertyChanged(nameof(SaldoPendienteTotal));
        }

        private void DesuscribirEventosTalleres()
        {
            foreach (var taller in TalleresDisponibles)
            {
                taller.PropertyChanged -= OnTallerPropertyChanged!;
            }
        }
    }
}
