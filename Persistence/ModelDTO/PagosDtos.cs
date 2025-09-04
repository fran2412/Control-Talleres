using ControlTalleresMVP.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlTalleresMVP.Persistence.ModelDTO
{
    public record DestinoCargoDTO(
        int CargoId,
        string Tipo,              // "Inscripción" | "Clase" | "Cargo"
        string Descripcion,       // Texto para UI
        decimal SaldoPendiente,
        int? InscripcionId,
        int? ClaseId
    );

    public record PagoAplicacionCapturaDTO(
        int CargoId,
        int? InscripcionId,
        int? ClaseId,
        decimal Monto
    );

    public record PagoCapturaDTO(
        int AlumnoId,
        decimal MontoTotal,
        PagoAplicacionCapturaDTO[] Aplicaciones,
        MetodoPago Metodo = MetodoPago.Efectivo
    );
}
