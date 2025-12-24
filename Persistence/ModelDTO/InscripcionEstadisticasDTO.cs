namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public class InscripcionEstadisticasDTO
    {
        // Estadísticas básicas
        public int TotalInscripciones { get; set; }
        public int InscripcionesActivas { get; set; }
        public int InscripcionesCanceladas { get; set; }
        public int InscripcionesPagadas { get; set; }

        // Estadísticas financieras
        public decimal MontoTotalInscripciones { get; set; }
        public decimal MontoTotalRecaudado { get; set; }
        public decimal MontoTotalPendiente { get; set; }
        public decimal PromedioCostoInscripcion { get; set; }

        // Estadísticas por período (removidas las de crecimiento)

        // Estadísticas por taller
        public int TalleresConInscripciones { get; set; }
        public string TallerMasPopular { get; set; } = "";
        public int InscripcionesTallerMasPopular { get; set; }

        // Estadísticas por día de la semana
        public int LunesInscripciones { get; set; }
        public int MartesInscripciones { get; set; }
        public int MiercolesInscripciones { get; set; }
        public int JuevesInscripciones { get; set; }
        public int ViernesInscripciones { get; set; }
        public int SabadoInscripciones { get; set; }
        public int DomingoInscripciones { get; set; }

        // Estadísticas de retención
        public decimal TasaRetencion { get; set; }
        public int AlumnosNuevos { get; set; }
        public int AlumnosRecurrentes { get; set; }

        // Formateo para mostrar
        public string MontoTotalInscripcionesFormateado => $"${MontoTotalInscripciones:N2}";
        public string MontoTotalRecaudadoFormateado => $"${MontoTotalRecaudado:N2}";
        public string MontoTotalPendienteFormateado => $"${MontoTotalPendiente:N2}";
        public string PromedioCostoInscripcionFormateado => $"${PromedioCostoInscripcion:N2}";
        public string TasaRetencionFormateado => $"{TasaRetencion:F1}%";
    }
}
