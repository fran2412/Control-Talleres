namespace ControlTalleresMVP.Services.Configuracion
{
    public interface IConfiguracionService
    {
        string GetValor(string clave, string valorPorDefecto = "");
        public T GetValor<T>(string clave, T valorPorDefecto = default!);
        bool SetValor(string clave, string valor);

        // Métodos para configuraciones específicas por sede
        T GetValorSede<T>(string clave, T valorPorDefecto = default!);
        bool SetValorSede(string clave, string valor);
    }
}
