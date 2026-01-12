using ControlTalleresMVP.Abstractions;
using ControlTalleresMVP.Configuraciones;
using ControlTalleresMVP.Helpers.Dialogs;
using ControlTalleresMVP.Persistence.DataContext;
using ControlTalleresMVP.Services.Alumnos;
using ControlTalleresMVP.Services.Cargos;
using ControlTalleresMVP.Services.Clases;
using ControlTalleresMVP.Services.Configuracion;
using ControlTalleresMVP.Services.Generaciones;
using ControlTalleresMVP.Services.Inscripciones;
using ControlTalleresMVP.Services.Navigation;
using ControlTalleresMVP.Services.Pagos;
using ControlTalleresMVP.Services.Picker;
using ControlTalleresMVP.Services.Promotores;
using ControlTalleresMVP.Services.Sedes;
using ControlTalleresMVP.Services.Talleres;
using ControlTalleresMVP.Services.Backup;
using ControlTalleresMVP.Services.Exportacion;
using ControlTalleresMVP.Services.Sesion;
using ControlTalleresMVP.ViewModel.Sesion;
using ControlTalleresMVP.UI.Windows;
using ControlTalleresMVP.ViewModel.Menu;
using ControlTalleresMVP.ViewModel.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Windows;
using ControlTalleresMVP.Validators;

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

            // Verificación de licencia antes de abrir la ventana principal
            using (var scope = ServiceProvider.CreateScope())
            {
                var sp = scope.ServiceProvider;
                var configService = sp.GetRequiredService<IConfiguracionService>();
                var dialogService = sp.GetRequiredService<IDialogService>();

                var verificada = configService.GetValor<bool>("licencia_verificada", false);
                if (!verificada)
                {
                    var hoy = DateTime.Now.Day;
                    var ayer = DateTime.Now.Day - 1;
                    var codigoEsperado = $"STCV{hoy}{ayer}";
                    var ingresado = dialogService.PedirTexto("Ingrese el código de verificación para este dispositivo:\n", "Verificación de licencia");

                    if (!string.IsNullOrWhiteSpace(ingresado) && string.Equals(ingresado.Trim(), codigoEsperado, StringComparison.OrdinalIgnoreCase))
                    {
                        configService.SetValor("licencia_verificada", bool.TrueString);
                    }
                    else
                    {
                        dialogService.Error("Código inválido. La aplicación se cerrará.", "Verificación fallida");
                        Shutdown();
                        return;
                    }
                }
            }

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            using (var scope = ServiceProvider.CreateScope())
            {
                var serviceProviderScope = scope.ServiceProvider;
                var escuelaContext = serviceProviderScope.GetRequiredService<EscuelaContext>();

                var backupService = serviceProviderScope.GetRequiredService<IBackupService>();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await backupService.CreateAutomaticBackupAsync();
                    }
                    catch
                    {
                    }
                });

            }
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
            services.AddTransient<SeleccionSedeWindow>();

            //ViewModels
            services.AddTransient<MenuInicioViewModel>();
            services.AddTransient<MenuAlumnosViewModel>();
            services.AddTransient<MenuTalleresViewModel>();
            services.AddTransient<MenuInscripcionesViewModel>();
            services.AddTransient<MenuInscripcionRegistrosViewModel>();
            services.AddTransient<MenuClaseUserControl>();
            services.AddTransient<MenuClaseCobroViewModel>();
            services.AddTransient<MenuClaseRegistrosViewModel>();
            services.AddTransient<MenuPagosViewModel>();
            services.AddTransient<MenuPromotorViewModel>();
            services.AddTransient<MenuSedeViewModel>();
            services.AddTransient<MenuAdministracionViewModel>();
            services.AddTransient<MenuBackupViewModel>();
            services.AddTransient<ReporteEstadoPagosViewModel>();
            services.AddTransient<MenuReporteEstadoPagosViewModel>();
            services.AddTransient<MenuReporteInscripcionesViewModel>();
            services.AddTransient<ShellViewModel>();
            services.AddTransient<SeleccionSedeViewModel>();

            //Services
            services.AddScoped<INavigatorService, NavigatorService>();
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
            services.AddScoped<IClaseService, ClaseService>();
            services.AddScoped<IInscripcionReporteService, InscripcionReporteService>();
            services.AddTransient<IAlumnoPickerService, AlumnoPickerService>();
            services.AddScoped<IBackupService, BackupService>();
            services.AddTransient<IExportacionService, ExportacionService>();
            services.AddSingleton<ISesionService, SesionService>();

            //Validators
            services.AddScoped<IAlumnoValidator, AlumnoValidator>();
        }
    }
}