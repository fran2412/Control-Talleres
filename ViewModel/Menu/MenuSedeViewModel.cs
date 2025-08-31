using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Promotores;
using ControlTalleresMVP.Services.Sedes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuSedeViewModel : BaseMenuViewModel<SedeDTO, Sede, ISedeService>
    {
        public string TituloEncabezado { get; set; } = "Gestión de sedes";
        public override ObservableCollection<SedeDTO> Registros
            => _itemService.RegistrosSedes;

        public MenuSedeViewModel(ISedeService itemService, IDialogService dialogService)
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
                _dialogService.Alerta("El nombre de la sede es obligatorio");
                return;
            }

            if (_dialogService.Confirmar(
                $"¿Confirma que desea registrar la sede: {CampoTextoNombre}?") != true)
            {
                return;
            }

            try
            {
                await _itemService.GuardarAsync(new Sede
                {
                    Nombre = CampoTextoNombre.Trim(),
                });

                LimpiarCampos();
                _dialogService.Info("Sede registrada correctamente");
            }
            catch (Exception ex)
            {
                _dialogService.Error("Error al registrar la sede.\n" + ex.Message);
            }
        }

        protected override async Task ActualizarAsync(SedeDTO? sedeSeleccionada)
        {
            await Task.CompletedTask;
            _dialogService.Info(sedeSeleccionada!.Nombre);
        }


        protected override void LimpiarCampos()
        {
            CampoTextoNombre = "";

        }

        public override bool Filtro(object o)
        {
            if (o is not SedeDTO a) return false;
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
