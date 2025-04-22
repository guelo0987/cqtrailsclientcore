using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using cqtrailsclientcore.DTO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Colors;
using iText.IO.Image;
using iText.Layout.Properties;
using iText.Layout.Borders;
using Microsoft.Extensions.Logging;

namespace cqtrailsclientcore.Utils;

public static class PdfGenerator
{
    public static async Task<string> GenerarPrefacturaPDFAsync(
        PreFacturaDTO dto, 
        string webRootPath, 
        GoogleDriveService googleDriveService,
        ILogger logger)
    {
        try 
        {
            // Generar PDF localmente
            string localFilePath = GenerarPdfDesdeZero(dto, webRootPath);
            
            // Subir a Google Drive
            string fileName = $"PreFactura_Custom_{dto.IdPreFactura}.pdf";
            logger.LogInformation($"Subiendo archivo {fileName} a Google Drive...");
            
            // Subir a Google Drive y obtener URL
            string googleDriveUrl = await googleDriveService.UploadFile(localFilePath, fileName);
            logger.LogInformation($"Archivo subido exitosamente a Google Drive: {googleDriveUrl}");
            
            return googleDriveUrl;
        }
        catch (Exception ex)
        {
            logger.LogError($"Error al generar PDF: {ex.Message}");
            throw;
        }
    }
    
    // Método para compatibilidad con código existente
    public static string GenerarPrefacturaPDF(PreFacturaDTO dto, string webRootPath)
    {
        return GenerarPdfDesdeZero(dto, webRootPath);
    }

    private static string GenerarPdfDesdeZero(PreFacturaDTO dto, string webRootPath)
    {
        // Carpeta de salida
        string outputDir = Path.Combine(webRootPath, "prefacturas");
        Directory.CreateDirectory(outputDir);
        
        // Nombre de archivo
        string fileName = $"PreFactura_Custom_{dto.IdPreFactura}.pdf";
        string outputPath = Path.Combine(outputDir, fileName);
        
        try
        {
            // Crear documento PDF con iText7
            using (PdfWriter writer = new PdfWriter(outputPath))
            using (PdfDocument pdf = new PdfDocument(writer))
            using (Document document = new Document(pdf))
            {
                // Título
                Paragraph titulo = new Paragraph("PRE-FACTURA")
                    .SetFontSize(24)
                    .SetBold()
                    .SetTextAlignment(TextAlignment.LEFT);
                document.Add(titulo);
                
                // Número de prefactura
                Paragraph numeroPrefactura = new Paragraph($"N°: {dto.IdPreFactura}")
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.LEFT);
                document.Add(numeroPrefactura);
                
                // Espacio
                document.Add(new Paragraph(" "));
                
                // Crear tabla de información
                Table infoTabla = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(100));
                
                // Datos del cliente
                Cell celdaCliente = new Cell()
                    .SetBackgroundColor(new DeviceRgb(240, 240, 240))
                    .SetPadding(10);
                
                celdaCliente.Add(new Paragraph("DATOS DEL CLIENTE")
                    .SetFontSize(12)
                    .SetBold());
                
                if (dto.Reservacion?.Usuario != null)
                {
                    string nombreCompleto = $"{dto.Reservacion.Usuario.Nombre} {dto.Reservacion.Usuario.Apellido}".Trim();
                    celdaCliente.Add(new Paragraph(nombreCompleto).SetFontSize(10));
                    celdaCliente.Add(new Paragraph(dto.Reservacion.Usuario.Email).SetFontSize(10));
                    celdaCliente.Add(new Paragraph("809-000-0000").SetFontSize(10));
                }
                
                // Datos de la empresa
                Cell celdaEmpresa = new Cell()
                    .SetBackgroundColor(new DeviceRgb(240, 240, 240))
                    .SetPadding(10);
                
                celdaEmpresa.Add(new Paragraph("DATOS DE LA EMPRESA")
                    .SetFontSize(12)
                    .SetBold()
                    .SetTextAlignment(TextAlignment.RIGHT));
                
                celdaEmpresa.Add(new Paragraph("CQTRAILS S.A.")
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.RIGHT));
                
                celdaEmpresa.Add(new Paragraph("info@cqtrails.com")
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.RIGHT));
                
                celdaEmpresa.Add(new Paragraph("809-123-4567")
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.RIGHT));
                
                celdaEmpresa.Add(new Paragraph("Av. Principal #123, Santo Domingo")
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.RIGHT));
                
                infoTabla.AddCell(celdaCliente);
                infoTabla.AddCell(celdaEmpresa);
                document.Add(infoTabla);
                
                // Espacio
                document.Add(new Paragraph(" "));
                
                // Datos de la reservación
                if (dto.Reservacion != null)
                {
                    Table reservacionTabla = new Table(2)
                        .SetWidth(UnitValue.CreatePercentValue(100));
                    
                    AddTableCell(reservacionTabla, "Reservación:", dto.Reservacion.IdReservacion.ToString());
                    
                    string fechaInicio = dto.Reservacion.FechaInicio.ToString("dd/MM/yyyy");
                    string fechaFin = dto.Reservacion.FechaFin.ToString("dd/MM/yyyy");
                    AddTableCell(reservacionTabla, "Periodo:", $"Del {fechaInicio} al {fechaFin}");
                    
                    document.Add(reservacionTabla);
                    document.Add(new Paragraph(" "));
                }
                
                // Información de costos
                decimal subtotal = dto.CostoVehiculo;
                decimal costoAdicional = dto.CostoAdicional ?? 0;
                decimal subtotalConAdicionales = subtotal + costoAdicional;
                decimal iva = subtotalConAdicionales * 0.18m;
                decimal total = dto.CostoTotal;
                
                // Tabla de totales
                Table totalesTabla = new Table(2)
                    .SetWidth(UnitValue.CreatePercentValue(50))
                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT);
                
                AddTableCellBold(totalesTabla, "Subtotal:", subtotalConAdicionales.ToString("C"));
                AddTableCellBold(totalesTabla, "IVA (18%):", iva.ToString("C"));
                
                // Celda de total
                Cell celdaTotalLabel = new Cell()
                    .Add(new Paragraph("TOTAL:").SetFontSize(12).SetBold().SetFontColor(ColorConstants.WHITE))
                    .SetBackgroundColor(new DeviceRgb(0, 150, 0))
                    .SetPadding(5)
                    .SetTextAlignment(TextAlignment.LEFT);
                
                Cell celdaTotalValor = new Cell()
                    .Add(new Paragraph(total.ToString("C")).SetFontSize(12).SetBold().SetFontColor(ColorConstants.WHITE))
                    .SetBackgroundColor(new DeviceRgb(0, 150, 0))
                    .SetPadding(5)
                    .SetTextAlignment(TextAlignment.RIGHT);
                
                totalesTabla.AddCell(celdaTotalLabel);
                totalesTabla.AddCell(celdaTotalValor);
                document.Add(totalesTabla);
                
                // Términos y condiciones
                document.Add(new Paragraph(" "));
                document.Add(new Paragraph("Términos y Condiciones").SetFontSize(10).SetBold());
                document.Add(new Paragraph(
                    "1. Esta prefactura tiene una validez de 30 días desde su emisión.\n" +
                    "2. El pago debe realizarse antes de la fecha de vencimiento.\n" +
                    "3. Los precios incluyen 18% de ITBIS.\n" +
                    "4. Para más información, contacte a nuestro servicio al cliente."
                ).SetFontSize(8));
                
                // Fecha de emisión
                document.Add(new Paragraph($"Fecha de emisión: {DateTime.Now:dd/MM/yyyy}")
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.RIGHT));
            }
            
            // Devolver la ruta completa del archivo
            return outputPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al generar PDF: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }
    
    // Métodos auxiliares para la creación de PDF
    private static void AddTableCell(Table table, string label, string value)
    {
        Cell labelCell = new Cell()
            .Add(new Paragraph(label).SetBold().SetFontSize(10))
            .SetBorderBottom(new SolidBorder(1))
            .SetPaddingBottom(5);
        
        Cell valueCell = new Cell()
            .Add(new Paragraph(value).SetFontSize(10))
            .SetBorderBottom(new SolidBorder(1))
            .SetPaddingBottom(5);
        
        table.AddCell(labelCell);
        table.AddCell(valueCell);
    }
    
    private static void AddTableCellBold(Table table, string label, string value)
    {
        Cell labelCell = new Cell()
            .Add(new Paragraph(label).SetBold().SetFontSize(10))
            .SetBorder(Border.NO_BORDER)
            .SetPaddingBottom(5);
        
        Cell valueCell = new Cell()
            .Add(new Paragraph(value).SetBold().SetFontSize(10))
            .SetBorder(Border.NO_BORDER)
            .SetTextAlignment(TextAlignment.RIGHT)
            .SetPaddingBottom(5);
        
        table.AddCell(labelCell);
        table.AddCell(valueCell);
    }
}
