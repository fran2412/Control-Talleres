using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ControlTalleresMVP.Persistence.Converters
{
    public class DiaSemanaConverter : ValueConverter<DayOfWeek, string>
    {
        private static readonly Dictionary<DayOfWeek, string> DiaSemanaToSpanish = new()
        {
            [DayOfWeek.Monday] = "Lunes",
            [DayOfWeek.Tuesday] = "Martes",
            [DayOfWeek.Wednesday] = "Miércoles",
            [DayOfWeek.Thursday] = "Jueves",
            [DayOfWeek.Friday] = "Viernes",
            [DayOfWeek.Saturday] = "Sábado",
            [DayOfWeek.Sunday] = "Domingo",
        };

        private static readonly Dictionary<string, DayOfWeek> SpanishToDiaSemana = new()
        {
            ["Lunes"] = DayOfWeek.Monday,
            ["Martes"] = DayOfWeek.Tuesday,
            ["Miércoles"] = DayOfWeek.Wednesday,
            ["Jueves"] = DayOfWeek.Thursday,
            ["Viernes"] = DayOfWeek.Friday,
            ["Sábado"] = DayOfWeek.Saturday,
            ["Domingo"] = DayOfWeek.Sunday,
        };

        // También manejar valores en inglés por compatibilidad
        private static readonly Dictionary<string, DayOfWeek> EnglishToDiaSemana = new()
        {
            ["Monday"] = DayOfWeek.Monday,
            ["Tuesday"] = DayOfWeek.Tuesday,
            ["Wednesday"] = DayOfWeek.Wednesday,
            ["Thursday"] = DayOfWeek.Thursday,
            ["Friday"] = DayOfWeek.Friday,
            ["Saturday"] = DayOfWeek.Saturday,
            ["Sunday"] = DayOfWeek.Sunday,
        };

        public DiaSemanaConverter() : base(
            v => DiaSemanaToSpanish[v],
            v => ConvertFromString(v))
        {
        }

        private static DayOfWeek ConvertFromString(string value)
        {
            if (SpanishToDiaSemana.ContainsKey(value))
                return SpanishToDiaSemana[value];

            if (EnglishToDiaSemana.ContainsKey(value))
                return EnglishToDiaSemana[value];

            return DayOfWeek.Monday; // Valor por defecto
        }
    }
}
