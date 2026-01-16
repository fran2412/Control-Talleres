using ControlTalleresMVP.Persistence.ModelDTO;
using ControlTalleresMVP.UI.Windows.FormContainer;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ControlTalleresMVP.UI.Component.Taller
{
    /// <summary>
    /// Lógica de interacción para RegistrosTallerUserControl.xaml
    /// </summary>
    public partial class RegistrosTallerUserControl : UserControl
    {
        public RegistrosTallerUserControl()
        {
            InitializeComponent();
        }


        private void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TallerDTO tallerDto)
            {
                var taller = new Persistence.Models.Taller
                {
                    TallerId = tallerDto.Id,
                    HorarioInicio = tallerDto.HorarioInicio,
                    HorarioFin = tallerDto.HorarioFin,
                    Nombre = tallerDto.Nombre,
                    DiaSemana = ConvertirStringADiaSemana(tallerDto.DiaSemana),
                };

                new ContenedorFormTallerWindow(taller).ShowDialog();

            }
        }

        private static DayOfWeek ConvertirStringADiaSemana(string diaSemana)
        {
            return diaSemana switch
            {
                "Lunes" => DayOfWeek.Monday,
                "Martes" => DayOfWeek.Tuesday,
                "Miércoles" => DayOfWeek.Wednesday,
                "Jueves" => DayOfWeek.Thursday,
                "Viernes" => DayOfWeek.Friday,
                "Sábado" => DayOfWeek.Saturday,
                "Domingo" => DayOfWeek.Sunday,
                _ => DayOfWeek.Monday
            };
        }
    }
}
