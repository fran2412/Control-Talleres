using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public class MenuAlumnosViewModel: INotifyPropertyChanged
    {
        public string TituloEncabezado { get; set; } = "Gestión de Alumnos";
        public ObservableCollection<TallerInscripcion> TalleresDisponibles { get; set; }

        public MenuAlumnosViewModel()
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


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
}
