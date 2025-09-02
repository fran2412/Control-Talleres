using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public class TallerInscripcionDTO : INotifyPropertyChanged
    {
        public int TallerId { get; set; }

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
                var nuevoValor = value;
                if (nuevoValor < 0) nuevoValor = 0;
                if (nuevoValor > Costo) nuevoValor = Costo;

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
