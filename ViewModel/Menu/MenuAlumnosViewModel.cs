using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Helpers.Commands;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Alumnos;
using ControlTalleresMVP.Services.Configuracion;
using ControlTalleresMVP.Services.Generaciones;
using ControlTalleresMVP.Services.Inscripciones;
using ControlTalleresMVP.Services.Promotores;
using ControlTalleresMVP.Services.Sedes;
using ControlTalleresMVP.Services.Talleres;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

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
        [ObservableProperty] private int? sedeSeleccionadaId;
        [ObservableProperty] private int? promotorSeleccionadoId;
        [ObservableProperty] private bool inscribirEnTaller;

        private readonly IPromotorService _promotorService;
        private readonly ISedeService _sedeService;
        private readonly IConfiguracionService _configuracionService;
        private readonly ITallerService _tallerService;
        private readonly IInscripcionService _inscripcionService;

        public MenuAlumnosViewModel(IAlumnoService itemService, IDialogService dialogService, IPromotorService promotorService, ISedeService sedeService, ITallerService tallerService, IConfiguracionService configuracionService, IInscripcionService inscripcionesService)
            : base(itemService, dialogService)
        {
            _promotorService = promotorService;
            _sedeService = sedeService;
            _configuracionService = configuracionService;
            _tallerService = tallerService;
            _inscripcionService = inscripcionesService;

            OpcionesPromotor = new ObservableCollection<Promotor>(_promotorService.ObtenerTodos());
            OpcionesSede = new ObservableCollection<Sede>(_sedeService.ObtenerTodos());

            TalleresDisponibles = new ObservableCollection<TallerInscripcionDTO>();


            itemService.InicializarRegistros();
            InicializarVista();

            Application.Current.Dispatcher.Invoke(() => CargarTalleresDesdeBd());

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
                TalleresDisponibles.Clear();

                int costoDefault = 600;
                try
                {
                    costoDefault = _configuracionService.GetValor<int>("costo_inscripcion", 600);
                }
                catch (Exception ex)
                {
                    _dialogService.Error("No se pudo obtener el costo por defecto desde configuración.\n" + ex.Message);
                }

                var talleres = _tallerService.ObtenerTodos();

                foreach (var taller in talleres)
                {
                    var item = new TallerInscripcionDTO
                    {
                        TallerId = taller.TallerId,
                        Nombre = taller.Nombre,
                        Costo  = costoDefault,
                        EstaSeleccionado = false,
                        Abono = 0
                    };

                    item.PropertyChanged += OnTallerPropertyChanged!;
                    TalleresDisponibles.Add(item);
                }

                // Refresca totales
                OnPropertyChanged(nameof(TotalCostos));
                OnPropertyChanged(nameof(TotalAbonado));
                OnPropertyChanged(nameof(SaldoPendienteTotal));
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
                OnPropertyChanged(nameof(TotalCostos));
                OnPropertyChanged(nameof(TotalAbonado));
                OnPropertyChanged(nameof(SaldoPendienteTotal));
            }
        }

        public decimal TotalCostos =>
            TalleresDisponibles?.Where(t => t.EstaSeleccionado).Sum(t => t.Costo) ?? 0;

        public decimal TotalAbonado =>
            TalleresDisponibles?.Where(t => t.EstaSeleccionado).Sum(t => t.Abono) ?? 0;

        public decimal SaldoPendienteTotal =>
            TalleresDisponibles?.Where(t => t.EstaSeleccionado).Sum(t => t.SaldoPendiente) ?? 0;

        public ObservableCollection<Sede> OpcionesSede { get; set; }

        public ObservableCollection<Promotor> OpcionesPromotor { get; }


        [RelayCommand]
        protected override async Task RegistrarItemAsync()
        {
            if (string.IsNullOrWhiteSpace(CampoTextoNombre))
            {
                _dialogService.Alerta("El nombre del alumno es obligatorio");
                return;
            }

            if (_dialogService.Confirmar(
                $"¿Confirma que desea registrar al alumno: {CampoTextoNombre}?") != true)
            {
                return;
            }

            try
            {
                // IMPORTANTE: capturamos el alumno guardado para obtener AlumnoId
                var alumnoGuardado = await _itemService.GuardarAsync(new Alumno
                {
                    Nombre = CampoTextoNombre.Trim(),
                    Telefono = CampoTextTelefono?.Trim(),
                    SedeId = SedeSeleccionadaId,
                    PromotorId = PromotorSeleccionadoId,
                });

                // Procesar inscripciones si está marcado
                if (InscribirEnTaller)
                {
                    await ProcesarInscripcionesTalleres(alumnoGuardado.AlumnoId);
                }

                LimpiarCampos();
                _dialogService.Info("Alumno registrado correctamente");
            }
            catch (Exception ex)
            {
                _dialogService.Error("Error al registrar el alumno.\n" + ex.Message);
            }
        }

        private async Task ProcesarInscripcionesTalleres(int alumnoId)
        {
            var seleccionados = TalleresDisponibles?.Where(t => t.EstaSeleccionado).ToList();
            if (seleccionados == null || seleccionados.Count == 0)
            {
                _dialogService.Alerta("Marcaste 'Inscribir en taller', pero no seleccionaste ningún taller.");
                return;
            }

            var errores = new List<string>();
            int inscripcionesOk = 0;

            foreach (var taller in seleccionados)
            {
                try
                {
                    var abono = taller.Abono is decimal dec && dec > 0 ? dec : 0m;

                    await _inscripcionService.InscribirAsync(
                        alumnoId: alumnoId,
                        tallerId: taller.TallerId,
                        abonoInicial: abono
                    );

                    inscripcionesOk++;
                }
                catch (Exception ex)
                {
                    errores.Add($"Taller {taller.Nombre ?? taller.TallerId.ToString()}: {ex.Message}");
                }
            }

            if (inscripcionesOk > 0)
                _dialogService.Info($"Se registraron {inscripcionesOk} inscripción(es).");

            if (errores.Count > 0)
                _dialogService.Alerta("Algunas inscripciones no se pudieron procesar:\n" + string.Join("\n", errores));
        }

        protected override void LimpiarCampos()
        {
            CampoTextoNombre = "";
            CampoTextTelefono = "";
            SedeSeleccionadaId = null;
            PromotorSeleccionadoId = null;

            if (TalleresDisponibles != null)
            {
                foreach (var taller in TalleresDisponibles)
                {
                    taller.EstaSeleccionado = false;
                    taller.Abono = 0;
                }
            }
        }

        public override bool Filtro(object o)
        {
            if (o is not AlumnoDTO a)
                return false;

            if (string.IsNullOrWhiteSpace(FiltroRegistros))
                return true;

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
    }
}