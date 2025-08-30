using ControlTalleresMVP.Helpers.Commands;
using ControlTalleresMVP.Persistence.Models;
using ControlTalleresMVP.Services.Alumnos;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public class MenuAlumnosViewModel : INotifyPropertyChanged
    {
        public string TituloEncabezado { get; set; } = "Gestión de Alumnos";
        public ObservableCollection<TallerInscripcion> TalleresDisponibles { get; set; }
        public ICommand RegistrarAlumnoCommand { get; }

        private readonly IAlumnoService _alumnoService;


        public MenuAlumnosViewModel(IAlumnoService alumnoService)
        {
            TalleresDisponibles = new ObservableCollection<TallerInscripcion>
            {
                new TallerInscripcion
                {
                    Nombre = "Uñas",
                    Costo = 1200,
                },
                new TallerInscripcion
                {
                    Nombre = "Repostería",
                    Costo = 1500,
                }
            };

            _alumnoService = alumnoService;
            RegistrarAlumnoCommand = new RelayCommand(RegistrarAlumno);

            foreach (var taller in TalleresDisponibles)
            {
                taller.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(TallerInscripcion.Abono) ||
                        e.PropertyName == nameof(TallerInscripcion.SaldoPendiente) ||
                        e.PropertyName == nameof(TallerInscripcion.EstaSeleccionado))
                    {
                        OnPropertyChanged(nameof(TotalCostos));
                        OnPropertyChanged(nameof(TotalAbonado));
                        OnPropertyChanged(nameof(SaldoPendienteTotal));
                    }
                };
            }
        }

        public decimal TotalCostos =>
    TalleresDisponibles.Where(t => t.EstaSeleccionado).Sum(t => t.Costo);

        public decimal TotalAbonado =>
            TalleresDisponibles.Where(t => t.EstaSeleccionado).Sum(t => t.Abono);

        public decimal SaldoPendienteTotal =>
            TalleresDisponibles.Where(t => t.EstaSeleccionado).Sum(t => t.SaldoPendiente);

        private string _campoTextoNombre = "";
        public string CampoTextoNombre
        {
            get => _campoTextoNombre;
            set
            {
                if (_campoTextoNombre != value)
                {
                    _campoTextoNombre = value;
                    OnPropertyChanged(nameof(CampoTextoNombre));
                }
            }
        }

        private string _campoTextTelefono = "";
        public string CampoTextTelefono
        {
            get => _campoTextTelefono;
            set
            {
                if (_campoTextTelefono != value)
                {
                    _campoTextTelefono = value;
                    OnPropertyChanged(nameof(CampoTextTelefono));
                }
            }
        }

        public ObservableCollection<Sede>? OpcionesSede { get; set; }

        private int? _sedeSeleccionadaId;
        public int? SedeSeleccionadaId
        {
            get => _sedeSeleccionadaId;
            set
            {
                _sedeSeleccionadaId = value;
                OnPropertyChanged(nameof(SedeSeleccionadaId));
            }
        }

        public ObservableCollection<Promotor>? OpcionesPromotor { get; set; }

        private int? _promotorSeleccionadoId;
        public int? PromotorSeleccionadoId
        {
            get => _promotorSeleccionadoId;
            set
            {
                _promotorSeleccionadoId = value;
                OnPropertyChanged(nameof(PromotorSeleccionadoId));
            }
        }

        private bool _inscribirEnTaller;
        public bool InscribirEnTaller
        {
            get => _inscribirEnTaller;
            set
            {
                if (_inscribirEnTaller != value)
                {
                    _inscribirEnTaller = value;
                    OnPropertyChanged(nameof(InscribirEnTaller));
                }
            }
        }


        private void RegistrarAlumno()
        {
            if (string.IsNullOrWhiteSpace(CampoTextoNombre))
            {
                MessageBox.Show("El nombre del alumno es obligatorio"); return;
            }

            if (InscribirEnTaller)
            {
                MessageBox.Show("Inscripción en talleres no implementada aún.");
                // Lógica para inscribir en talleres
            }
            try
            {
                _alumnoService.AgregarAlumno(new Alumno
                {
                    Nombre = CampoTextoNombre,
                    Telefono = CampoTextTelefono,
                    IdSede = SedeSeleccionadaId,
                    IdPromotor = PromotorSeleccionadoId,
                });
                MessageBox.Show("Alumno registrado exitosamente.");
            }
            catch (Exception ex) 
            {
                MessageBox.Show("Error al registrar el alumno.\n" + ex.Message);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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