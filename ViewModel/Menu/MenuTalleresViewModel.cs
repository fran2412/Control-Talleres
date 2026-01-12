using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Sesion;
using ControlTalleresMVP.Services.Talleres;
using System.Collections.ObjectModel;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuTalleresViewModel : BaseMenuViewModel<TallerDTO, Taller, ITallerService>
    {
        public string TituloEncabezado { get; set; } = "Gestión de talleres";
        public override ObservableCollection<TallerDTO> Registros
            => _itemService.RegistrosTalleres;

        [ObservableProperty]
        private TimeSpan horarioInicio = new TimeSpan(9, 0, 0); // 9:00 AM por defecto

        [ObservableProperty]
        private TimeSpan horarioFin = new TimeSpan(11, 0, 0); // 11:00 AM por defecto

        // NUEVO: lista para bindear el ComboBox si lo necesitas desde el VM
        public IReadOnlyList<DayOfWeek> DiasSemana { get; } = Enum.GetValues<DayOfWeek>();

        // NUEVO: día de la semana seleccionado para el registro/edición
        [ObservableProperty]
        private DayOfWeek diaSemanaSeleccionado = DayOfWeek.Monday;

        [ObservableProperty]
        private DateTime fechaInicio = DateTime.Today;

        [ObservableProperty]
        private DateTime? fechaFin;

        private readonly ISesionService _sesionService;

        public MenuTalleresViewModel(ITallerService itemService, ISesionService sesionService, IDialogService dialogService)
            : base(itemService, dialogService)
        {
            _sesionService = sesionService;
            itemService.InicializarRegistros();
            InicializarVista();
        }



        [RelayCommand]
        protected override async Task RegistrarItemAsync()
        {
            if (string.IsNullOrWhiteSpace(CampoTextoNombre))
            {
                _dialogService.Alerta("El nombre del taller es obligatorio");
                return;
            }



            if (HorarioFin <= HorarioInicio)
            {
                _dialogService.Alerta("El horario de fin debe ser posterior al horario de inicio");
                return;
            }

            if (FechaFin.HasValue && FechaFin.Value < FechaInicio)
            {
                _dialogService.Alerta("La fecha de fin no puede ser anterior a la fecha de inicio");
                return;
            }

            if (_dialogService.Confirmar(
                $"¿Confirma que desea registrar el taller: {CampoTextoNombre.Trim()}?") != true)
            {
                return;
            }

            try
            {
                await _itemService.GuardarAsync(new Taller
                {
                    Nombre = CampoTextoNombre.Trim(),
                    HorarioInicio = HorarioInicio,
                    HorarioFin = HorarioFin,
                    DiaSemana = DiaSemanaSeleccionado,
                    FechaInicio = FechaInicio,
                    FechaFin = FechaFin,
                    SedeId = _sesionService.ObtenerIdSede()
                });

                LimpiarCampos();
                _dialogService.Info("Taller registrado correctamente");
            }
            catch (Exception ex)
            {
                _dialogService.Error("Error al registrar el taller.\n" + ex.Message);
            }
        }

        protected override async Task ActualizarAsync(TallerDTO? tallerSeleccionado)
        {
            await Task.CompletedTask;
            _dialogService.Info(tallerSeleccionado!.Nombre);
        }

        protected override void LimpiarCampos()
        {
            CampoTextoNombre = "";
            HorarioInicio = new TimeSpan(9, 0, 0);
            HorarioFin = new TimeSpan(11, 0, 0);
            DiaSemanaSeleccionado = DayOfWeek.Monday; // ← NUEVO (reset)
            FechaInicio = DateTime.Today;
            FechaFin = null;
        }

        public override bool Filtro(object o)
        {
            if (o is not TallerDTO a) return false;
            if (string.IsNullOrWhiteSpace(FiltroRegistros)) return true;

            var nombreCompleto = a.Nombre ?? "";
            var haystack = Normalizar($"{nombreCompleto}");

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
