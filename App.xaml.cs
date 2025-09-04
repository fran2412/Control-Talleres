using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Configuraciones;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Services.Alumnos;
using ControlTalleresMVP.Services.Cargos;
using ControlTalleresMVP.Services.Configuracion;
using ControlTalleresMVP.Services.Generaciones;
using ControlTalleresMVP.Services.Inscripciones;
using ControlTalleresMVP.Services.Navigation;
using ControlTalleresMVP.Services.Pagos;
using ControlTalleresMVP.Services.Picker;
using ControlTalleresMVP.Services.Promotores;
using ControlTalleresMVP.Services.Sedes;
using ControlTalleresMVP.Services.Talleres;
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
                opt.UseSqlite($"Data Source={AppPaths.DbPath}")
                .UseSnakeCaseNamingConvention(), ServiceLifetime.Scoped);

            //Ventanas
            services.AddSingleton<MainWindow>();
            services.AddSingleton<MenuWindow>();

            //ViewModels
            services.AddTransient<MenuInicioViewModel>();
            services.AddTransient<MenuAlumnosViewModel>();
            services.AddTransient<MenuTalleresViewModel>();
            services.AddTransient<MenuInscripcionesViewModel>();
            services.AddTransient<MenuPagosViewModel>();
            services.AddTransient<MenuPromotorViewModel>();
            services.AddTransient<MenuSedeViewModel>();
            services.AddTransient<MenuAdministracionViewModel>();
            services.AddTransient<ShellViewModel>();

            //Services
            services.AddTransient<INavigatorService, NavigatorService>();
            services.AddTransient<IDialogService, DialogService>();
            services.AddScoped<IAlumnoService, AlumnoService>();
            services.AddScoped<ISedeService, SedeService>();
            services.AddScoped<IGeneracionService, GeneracionService>();
            services.AddScoped<ITallerService, TallerService>();
            services.AddScoped<IInscripcionService, InscripcionService>();
            services.AddScoped<IConfiguracionService, ConfiguracionService>();
            services.AddScoped<IPromotorService, PromotorService>();
            services.AddScoped<ICargosService, CargoService>();
            services.AddScoped<IPagoService, PagoService>();
            services.AddTransient<IAlumnoPickerService, AlumnoPickerService>();
        }
    }
}