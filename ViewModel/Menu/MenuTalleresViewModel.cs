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
        private string campoTextoHorario = "";

        // NUEVO: lista para bindear el ComboBox si lo necesitas desde el VM
        public IReadOnlyList<DayOfWeek> DiasSemana { get; } = Enum.GetValues<DayOfWeek>();

        // NUEVO: día de la semana seleccionado para el registro/edición
        [ObservableProperty]
        private DayOfWeek diaSemanaSeleccionado = DayOfWeek.Monday;

        public MenuTalleresViewModel(ITallerService itemService, IDialogService dialogService)
            : base(itemService, dialogService)
        {
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

            if (string.IsNullOrWhiteSpace(CampoTextoHorario))
            {
                _dialogService.Alerta("El horario del taller es obligatorio");
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
                    Horario = CampoTextoHorario.Trim(),
                    DiaSemana = DiaSemanaSeleccionado
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
            CampoTextoHorario = "";
            DiaSemanaSeleccionado = DayOfWeek.Monday; // ← NUEVO (reset)
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
