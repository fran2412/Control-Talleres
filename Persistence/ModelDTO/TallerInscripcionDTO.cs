using ControlTalleresMVP.Persistence.Models;
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
            set { if (_estaSeleccionado != value) { _estaSeleccionado = value; OnPropertyChanged(nameof(EstaSeleccionado)); } }
        }

        // Si es null, el servicio resolverá la generación automáticamente
        public int? GeneracionId { get; set; }

        private decimal _costo = 1200m;
        public decimal Costo
        {
            get => _costo;
            set { var v = value < 0 ? 0 : value; if (_costo != v) { _costo = v; if (_abono > _costo) { _abono = _costo; OnPropertyChanged(nameof(Abono)); } OnPropertyChanged(nameof(Costo)); OnPropertyChanged(nameof(SaldoPendiente)); } }
        }

        private decimal _abono;
        public decimal Abono
        {
            get => _abono;
            set { var v = value; if (v < 0) v = 0; if (v > Costo) v = Costo; if (_abono != v) { _abono = v; OnPropertyChanged(nameof(Abono)); OnPropertyChanged(nameof(SaldoPendiente)); } }
        }

        public decimal SaldoPendiente => Math.Max(0, Costo - Abono);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
