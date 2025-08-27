using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Services.Navigation;
using ControlTalleresMVP.UI.Windows;
using ControlTalleresMVP.ViewModel.Menu;
using ControlTalleresMVP.ViewModel.Navigation;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Windows;

namespace ControlTalleresMVP
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();

            base.OnStartup(e);

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        private void ConfigureServices(IServiceCollection services)
        {
            //Ventanas
            services.AddSingleton<MainWindow>();
            services.AddSingleton<MenuWindow>();

            //ViewModels
            services.AddTransient<MenuInicioViewModel>();
            services.AddTransient<MenuAlumnosViewModel>();
            services.AddTransient<MenuTalleresViewModel>();
            services.AddTransient<MenuInscripcionesViewModel>();
            services.AddTransient<MenuPagosViewModel>();
            services.AddTransient<ShellViewModel>();
            services.AddTransient<INavigatorService, NavigatorService>();

        }
    }
}