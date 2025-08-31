using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Navigation;
using ControlTalleresMVP.Services.Promotores;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuPromotorViewModel: BaseMenuViewModel<PromotorDTO, Promotor, IPromotorService>
    {
        public string TituloEncabezado { get; set; } = "Gestión de promotores";
        public override string TextGuardarItemButton => "Guardar promotor";
        public override string TituloFormulario => "Registrar promotor";
        public override ObservableCollection<PromotorDTO> Registros
            => _itemService.RegistrosPromotores;

        public MenuPromotorViewModel(IPromotorService itemService, IDialogService dialogService)
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
                _dialogService.Alerta("El nombre del promotor es obligatorio");
                return;
            }

            if (_dialogService.Confirmar(
                $"¿Confirma que desea registrar al promotor: {CampoTextoNombre}?") != true)
            {
                return;
            }

            try
            {
                await _itemService.GuardarAsync(new Promotor
                {
                    Nombre = CampoTextoNombre.Trim(),
                });

                LimpiarCampos();
                _dialogService.Info("Promotor registrado correctamente");
            }
            catch (Exception ex)
            {
                _dialogService.Error("Error al registrar el promotor.\n" + ex.Message);
            }
        }

        protected override async Task ActualizarAsync(PromotorDTO? promotorSeleccionado)
        {
            await Task.CompletedTask;
            _dialogService.Info(promotorSeleccionado!.Nombre);
        }


        protected override void LimpiarCampos()
        {
            CampoTextoNombre = "";

        }

        public override bool Filtro(object o)
        {
            if (o is not PromotorDTO a) return false;
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
