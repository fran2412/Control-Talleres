using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Configuraciones;
using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Services.Alumnos;
using ControlTalleresMVP.Services.Navigation;
using ControlTalleresMVP.UI.Windows;
using ControlTalleresMVP.ViewModel.Menu;
using ControlTalleresMVP.ViewModel.Navigation;
using Microsoft.EntityFrameworkCore;
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
            AppPaths.EnsureAppFolder();

            var services = new ServiceCollection();
            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();

            base.OnStartup(e);

            var _escuelaContext = ServiceProvider.GetRequiredService<EscuelaContext>();

            using (var scope = ServiceProvider.CreateScope())
            {
                var serviceProviderScope = scope.ServiceProvider;
                var escuelaContext = serviceProviderScope.GetRequiredService<EscuelaContext>();

                escuelaContext.Database.Migrate();
            }


            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        private void ConfigureServices(IServiceCollection services)
        {
            //DbContext
            services.AddDbContext<EscuelaContext>(opt =>
                opt.UseSqlite($"Data Source={AppPaths.DbPath}"));

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

            //Services
            services.AddTransient<IAlumnoService, AlumnoService>();


        }
    }
}