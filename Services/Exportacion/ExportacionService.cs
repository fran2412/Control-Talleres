using ClosedXML.Excel;
using ControlTalleresMVP.Persistence.ModelDTO;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.IO;

namespace ControlTalleresMVP.Services.Exportacion
{
    public class ExportacionService : IExportacionService
    {
        public async Task<string> ExportarEstadoPagosAsync(IEnumerable<EstadoPagoAlumnoDTO> datos, string formato = "csv")
        {
            var rutaDescargas = ObtenerRutaDescargas();
            var nombreArchivo = $"Reporte_Estado_Pagos_{DateTime.Now:yyyyMMdd_HHmmss}";
            var rutaCompleta = Path.Combine(rutaDescargas, $"{nombreArchivo}.{formato.ToLower()}");

            if (formato.ToLower() == "csv")
            {
                await ExportarCsvEstadoPagosAsync(datos, rutaCompleta);
            }
            else if (formato.ToLower() == "xlsx")
            {
                await ExportarExcelEstadoPagosAsync(datos, rutaCompleta);
            }

            return rutaCompleta;
        }

        public async Task<string> ExportarInscripcionesAsync(IEnumerable<InscripcionReporteDTO> datos, string formato = "csv")
        {
            var rutaDescargas = ObtenerRutaDescargas();
            var nombreArchivo = $"Reporte_Inscripciones_{DateTime.Now:yyyyMMdd_HHmmss}";
            var rutaCompleta = Path.Combine(rutaDescargas, $"{nombreArchivo}.{formato.ToLower()}");

            if (formato.ToLower() == "csv")
            {
                await ExportarCsvInscripcionesAsync(datos, rutaCompleta);
            }
            else if (formato.ToLower() == "xlsx")
            {
                await ExportarExcelInscripcionesAsync(datos, rutaCompleta);
            }

            return rutaCompleta;
        }

        private async Task ExportarCsvEstadoPagosAsync(IEnumerable<EstadoPagoAlumnoDTO> datos, string rutaArchivo)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ","
            };

            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, config);

            // Escribir encabezados
            csv.WriteField("Alumno");
            csv.WriteField("Taller");
            csv.WriteField("Fecha Inicio");
            csv.WriteField("Fecha Fin");
            csv.WriteField("Clases Pagadas");
            csv.WriteField("Clases Totales");
            csv.WriteField("Monto Pagado");
            csv.WriteField("Monto Total");
            csv.WriteField("Estado Pago");
            csv.WriteField("Progreso %");
            csv.NextRecord();

            // Escribir datos
            foreach (var item in datos)
            {
                csv.WriteField(item.NombreAlumno);
                csv.WriteField(item.NombreTaller);
                csv.WriteField(item.FechaInicio.ToString("dd/MM/yyyy"));
                csv.WriteField(item.FechaFin?.ToString("dd/MM/yyyy") ?? "N/A");
                csv.WriteField(item.ClasesPagadas);
                csv.WriteField(item.ClasesTotales);
                csv.WriteField(item.MontoPagado.ToString("C2"));
                csv.WriteField(item.MontoTotal.ToString("C2"));
                csv.WriteField(item.EstadoPago);
                csv.WriteField($"{CalcularProgreso(item.ClasesPagadas, item.ClasesTotales):F1}%");
                csv.NextRecord();
            }

            await File.WriteAllTextAsync(rutaArchivo, writer.ToString());
        }

        private async Task ExportarCsvInscripcionesAsync(IEnumerable<InscripcionReporteDTO> datos, string rutaArchivo)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ","
            };

            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, config);

            // Escribir encabezados
            csv.WriteField("Alumno");
            csv.WriteField("Taller");
            csv.WriteField("Sede");
            csv.WriteField("Promotor");
            csv.WriteField("Generación");
            csv.WriteField("Fecha Inscripción");
            csv.WriteField("Costo");
            csv.WriteField("Saldo Actual");
            csv.WriteField("Estado");
            csv.WriteField("Día Semana");
            csv.WriteField("Fecha Inicio Taller");
            csv.WriteField("Fecha Fin Taller");
            csv.WriteField("Días Transcurridos");
            csv.WriteField("Días Restantes");
            csv.WriteField("Progreso %");
            csv.NextRecord();

            // Escribir datos
            foreach (var item in datos)
            {
                csv.WriteField(item.NombreAlumno);
                csv.WriteField(item.NombreTaller);
                csv.WriteField(item.NombreSede);
                csv.WriteField(item.NombrePromotor);
                csv.WriteField(item.NombreGeneracion);
                csv.WriteField(item.FechaInscripcion.ToString("dd/MM/yyyy"));
                csv.WriteField(item.Costo.ToString("C2"));
                csv.WriteField(item.SaldoActual.ToString("C2"));
                csv.WriteField(item.Estado);
                csv.WriteField(item.DiaSemana);
                csv.WriteField(item.FechaInicioTaller.ToString("dd/MM/yyyy"));
                csv.WriteField(item.FechaFinTaller?.ToString("dd/MM/yyyy") ?? "N/A");
                csv.WriteField(item.DiasTranscurridos);
                csv.WriteField(item.DiasRestantes?.ToString() ?? "N/A");
                csv.WriteField($"{item.ProgresoPorcentaje:F1}%");
                csv.NextRecord();
            }

            await File.WriteAllTextAsync(rutaArchivo, writer.ToString());
        }

        private async Task ExportarExcelEstadoPagosAsync(IEnumerable<EstadoPagoAlumnoDTO> datos, string rutaArchivo)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Estado de Pagos");

            // Escribir encabezados
            var headers = new[] { "Alumno", "Taller", "Fecha Inicio", "Fecha Fin", "Clases Pagadas",
                                "Clases Totales", "Monto Pagado", "Monto Total", "Estado Pago", "Progreso %" };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            }

            // Escribir datos
            int row = 2;
            foreach (var item in datos)
            {
                worksheet.Cell(row, 1).Value = item.NombreAlumno;
                worksheet.Cell(row, 2).Value = item.NombreTaller;
                worksheet.Cell(row, 3).Value = item.FechaInicio.ToString("dd/MM/yyyy");
                worksheet.Cell(row, 4).Value = item.FechaFin?.ToString("dd/MM/yyyy") ?? "N/A";
                worksheet.Cell(row, 5).Value = item.ClasesPagadas;
                worksheet.Cell(row, 6).Value = item.ClasesTotales;
                worksheet.Cell(row, 7).Value = item.MontoPagado;
                worksheet.Cell(row, 8).Value = item.MontoTotal;
                worksheet.Cell(row, 9).Value = item.EstadoPago;
                worksheet.Cell(row, 10).Value = $"{CalcularProgreso(item.ClasesPagadas, item.ClasesTotales):F1}%";
                row++;
            }

            // Ajustar ancho de columnas
            worksheet.Columns().AdjustToContents();

            await Task.Run(() => workbook.SaveAs(rutaArchivo));
        }

        private async Task ExportarExcelInscripcionesAsync(IEnumerable<InscripcionReporteDTO> datos, string rutaArchivo)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Inscripciones");

            // Escribir encabezados
            var headers = new[] { "Alumno", "Taller", "Sede", "Promotor", "Generación", "Fecha Inscripción",
                                "Costo", "Saldo Actual", "Estado", "Día Semana", "Fecha Inicio Taller",
                                "Fecha Fin Taller", "Días Transcurridos", "Días Restantes", "Progreso %" };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            }

            // Escribir datos
            int row = 2;
            foreach (var item in datos)
            {
                worksheet.Cell(row, 1).Value = item.NombreAlumno;
                worksheet.Cell(row, 2).Value = item.NombreTaller;
                worksheet.Cell(row, 3).Value = item.NombreSede;
                worksheet.Cell(row, 4).Value = item.NombrePromotor;
                worksheet.Cell(row, 5).Value = item.NombreGeneracion;
                worksheet.Cell(row, 6).Value = item.FechaInscripcion.ToString("dd/MM/yyyy");
                worksheet.Cell(row, 7).Value = item.Costo;
                worksheet.Cell(row, 8).Value = item.SaldoActual;
                worksheet.Cell(row, 9).Value = item.Estado;
                worksheet.Cell(row, 10).Value = item.DiaSemana;
                worksheet.Cell(row, 11).Value = item.FechaInicioTaller.ToString("dd/MM/yyyy");
                worksheet.Cell(row, 12).Value = item.FechaFinTaller?.ToString("dd/MM/yyyy") ?? "N/A";
                worksheet.Cell(row, 13).Value = item.DiasTranscurridos;
                worksheet.Cell(row, 14).Value = item.DiasRestantes?.ToString() ?? "N/A";
                worksheet.Cell(row, 15).Value = $"{item.ProgresoPorcentaje:F1}%";
                row++;
            }

            // Ajustar ancho de columnas
            worksheet.Columns().AdjustToContents();

            await Task.Run(() => workbook.SaveAs(rutaArchivo));
        }

        private static decimal CalcularProgreso(int clasesPagadas, int clasesTotales)
        {
            if (clasesTotales == 0) return 0;
            return (decimal)clasesPagadas / clasesTotales * 100;
        }

        private string ObtenerRutaDescargas()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
        }
    }
}
