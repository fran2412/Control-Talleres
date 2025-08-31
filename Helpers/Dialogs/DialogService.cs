using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ControlTalleresMVP.Helpers.Dialogs
{
    public class DialogService: IDialogService
    {
        /// <summary>
        /// Muestra un mensaje informativo (solo OK).
        /// </summary>
        public void Info(string mensaje, string titulo = "Información")
        {
            MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Muestra una advertencia (solo OK).
        /// </summary>
        public void Alerta(string mensaje, string titulo = "Advertencia")
        {
            MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Muestra un error (solo OK).
        /// </summary>
        public void Error(string mensaje, string titulo = "Error")
        {
            MessageBox.Show(mensaje, titulo, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Pide confirmación Sí/No.
        /// </summary>
        public bool Confirmar(string mensaje, string titulo = "Confirmación")
        {
            var r = MessageBox.Show(mensaje, titulo, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return r == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Pide decisión Sí/No/Cancelar.
        /// </summary>
        public MessageBoxResult Decidir(string mensaje, string titulo = "Decisión")
        {
            return MessageBox.Show(mensaje, titulo, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        }

        /// <summary>
        /// Pide confirmación OK/Cancelar.
        /// </summary>
        public bool ConfirmarOkCancel(string mensaje, string titulo = "Confirmación")
        {
            var r = MessageBox.Show(mensaje, titulo, MessageBoxButton.OKCancel, MessageBoxImage.Question);
            return r == MessageBoxResult.OK;
        }
    }
}
