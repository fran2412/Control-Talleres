using ControlTalleresMVP.Persistence.ModelDTO;

namespace ControlTalleresMVP.Services.Sesion
{
    public class SesionService : ISesionService
    {
        public SesionDto SesionActual { get; } = new SesionDto();

        public void EstablecerSede(int sedeId, string sedeNombre)
        {
            SesionActual.SedeId = sedeId;
            SesionActual.SedeNombre = sedeNombre;
        }
        public string ObtenerNombreSede()
        {
            return SesionActual.SedeNombre;
        }
        public int ObtenerIdSede()
        {
            return SesionActual.SedeId;
        }
    }
}
