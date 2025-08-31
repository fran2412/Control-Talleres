using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ControlTalleresMVP.Helpers.Dialogs
{
    public interface IDialogService
    {
        public void Info(string mensaje, string titulo = "Información");
        public void Alerta(string mensaje, string titulo = "Advertencia");
        public void Error(string mensaje, string titulo = "Error");
        public bool Confirmar(string mensaje, string titulo = "Confirmación");
        public MessageBoxResult Decidir(string mensaje, string titulo = "Decisión");
        public bool ConfirmarOkCancel(string mensaje, string titulo = "Confirmación");

    }
}
