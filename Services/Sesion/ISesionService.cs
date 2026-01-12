using ControlTalleresMVP.Persistence.ModelDTO;

namespace ControlTalleresMVP.Services.Sesion
{
    public interface ISesionService
    {
        SesionDto SesionActual { get; }
        void EstablecerSede(int sedeId, string sedeNombre);
        string ObtenerNombreSede();
        int ObtenerIdSede();
    }
}
