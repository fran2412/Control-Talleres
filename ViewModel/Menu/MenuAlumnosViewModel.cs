using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.ViewModel.Menu
{
    public class MenuAlumnosViewModel
    {
        public string TituloEncabezado { get; set; } = "Gestión de Alumnos";
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
