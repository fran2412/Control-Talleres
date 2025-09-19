using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Sedes;
using ControlTalleresMVP.Services.Talleres;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

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

        // Propiedades para manejar sedes
        [ObservableProperty]
        private ObservableCollection<Sede> sedesDisponibles = new();

        [ObservableProperty]
        private Sede? sedeSeleccionada;

        private readonly ISedeService _sedeService;

        public MenuTalleresViewModel(ITallerService itemService, ISedeService sedeService, IDialogService dialogService)
            : base(itemService, dialogService)
        {
            _sedeService = sedeService;
            itemService.InicializarRegistros();
            InicializarVista();
            CargarSedesDisponibles();
        }

        private void CargarSedesDisponibles()
        {
            try
            {
                var sedes = _sedeService.ObtenerTodos();
                SedesDisponibles.Clear();
                foreach (var sede in sedes)
                {
                    SedesDisponibles.Add(sede);
                }
                
                // Seleccionar la primera sede por defecto
                if (SedesDisponibles.Any())
                {
                    SedeSeleccionada = SedesDisponibles.First();
                }
            }
            catch (Exception ex)
            {
                // Manejar error de carga de sedes
                System.Windows.MessageBox.Show($"Error al cargar sedes: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        protected override async Task RegistrarItemAsync()
        {
            if (string.IsNullOrWhiteSpace(CampoTextoNombre))
            {
                _dialogService.Alerta("El nombre del taller es obligatorio");
                return;
            }

            if (SedeSeleccionada == null)
            {
                _dialogService.Alerta("Debe seleccionar una sede para el taller");
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
                    SedeId = SedeSeleccionada.SedeId
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
            SedeSeleccionada = SedesDisponibles.FirstOrDefault(); // Resetear a la primera sede
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
