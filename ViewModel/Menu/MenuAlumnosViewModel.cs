using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Helpers.Commands;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Alumnos;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Security.AccessControl;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public partial class MenuAlumnosViewModel : BaseMenuViewModel<AlumnoDTO, Alumno, IAlumnoService>
    {
        public string TituloEncabezado { get; set; } = "Gestión de alumnos";

        public override ObservableCollection<AlumnoDTO> Registros
            => _itemService.RegistrosAlumnos;

        public ObservableCollection<TallerInscripcion> TalleresDisponibles { get; }

        [ObservableProperty] private string campoTextoNombre = "";
        [ObservableProperty] private string campoTextTelefono = "";
        [ObservableProperty] private int? sedeSeleccionadaId;
        [ObservableProperty] private int? promotorSeleccionadoId;
        [ObservableProperty] private bool inscribirEnTaller;

        public MenuAlumnosViewModel(IAlumnoService itemService, IDialogService dialogService)
            : base(itemService, dialogService)
        {
            TalleresDisponibles = new ObservableCollection<TallerInscripcion>
            {
                new TallerInscripcion { Nombre = "Uñas", Costo = 1200 },
                new TallerInscripcion { Nombre = "Repostería", Costo = 1500 }
            };

            foreach (var taller in TalleresDisponibles)
            {
                taller.PropertyChanged += OnTallerPropertyChanged!;
            }

            itemService.InicializarRegistros();
            InicializarVista();
        }

        protected override async Task ActualizarAsync(AlumnoDTO? alumnoSeleccionado)
        {
            await Task.CompletedTask;
            _dialogService.Info(alumnoSeleccionado!.Nombre);
        }



        private void OnTallerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TallerInscripcion.Abono) ||
                e.PropertyName == nameof(TallerInscripcion.SaldoPendiente) ||
                e.PropertyName == nameof(TallerInscripcion.EstaSeleccionado))
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
                await _itemService.GuardarAsync(new Alumno
                {
                    Nombre = CampoTextoNombre.Trim(),
                    Telefono = CampoTextTelefono?.Trim(),
                    IdSede = SedeSeleccionadaId,
                    IdPromotor = PromotorSeleccionadoId,
                });

                // Procesar inscripciones si está marcado
                if (InscribirEnTaller)
                {
                    await ProcesarInscripcionesTalleres();
                }

                LimpiarCampos();
                _dialogService.Info("Alumno registrado correctamente");
            }
            catch (Exception ex)
            {
                _dialogService.Error("Error al registrar el alumno.\n" + ex.Message);
            }
        }

        private async Task ProcesarInscripcionesTalleres()
        {
            var talleresSeleccionados = TalleresDisponibles?.Where(t => t.EstaSeleccionado).ToList();
            if (talleresSeleccionados?.Any() != true) return;

            // TODO: Implementar lógica de inscripción a talleres
            // foreach (var taller in talleresSeleccionados)
            // {
            //     await _tallerService.InscribirAlumnoAsync(ultimoAlumnoId, taller, taller.Abono);
            // }
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
            if (o is not AlumnoDTO a) return false;
            if (string.IsNullOrWhiteSpace(FiltroRegistros)) return true;

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








public class TallerInscripcion : INotifyPropertyChanged
{
    public string Nombre { get; set; } = "";

    private bool _estaSeleccionado;
    public bool EstaSeleccionado
    {
        get => _estaSeleccionado;
        set
        {
            if (_estaSeleccionado != value)
            {
                _estaSeleccionado = value;
                OnPropertyChanged(nameof(EstaSeleccionado));
            }
        }
    }

    public decimal Costo { get; set; } = 1200;

    private decimal _abono;
    public decimal Abono
    {
        get => _abono;
        set
        {

            decimal nuevoValor = value;
            // ✅ Evitar que el abono sea mayor al costo
            if (nuevoValor > Costo)
                nuevoValor = Costo;

            if (_abono != nuevoValor)
            {
                _abono = nuevoValor;
                OnPropertyChanged(nameof(Abono));
                OnPropertyChanged(nameof(SaldoPendiente));
            }
        }
    }

    public decimal SaldoPendiente => Math.Max(0, Costo - Abono);

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}