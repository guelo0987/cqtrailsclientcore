using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using cqtrailsclientcore.DTO;
using iTextSharp.text;
using iTextSharp.text.pdf;
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
        // Se intentarán las dos formas de generar PDF
        try 
        {
            // Intentar con PDF desde cero primero
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
            logger.LogError($"Error al generar PDF desde cero: {ex.Message}");
            
            // Si falla, intentar con la plantilla
            string localFilePath = GenerarPdfDesdeTemplate(dto, webRootPath);
            
            // Subir a Google Drive
            string fileName = $"PreFactura_Template_{dto.IdPreFactura}.pdf";
            logger.LogInformation($"Subiendo archivo alternativo {fileName} a Google Drive...");
            
            // Subir a Google Drive y obtener URL
            string googleDriveUrl = await googleDriveService.UploadFile(localFilePath, fileName);
            logger.LogInformation($"Archivo alternativo subido exitosamente a Google Drive: {googleDriveUrl}");
            
            return googleDriveUrl;
        }
    }
    
    // Método para compatibilidad con código existente
    public static string GenerarPrefacturaPDF(PreFacturaDTO dto, string webRootPath)
    {
        // Generar PDF localmente (sin subirlo a Google Drive)
        try 
        {
            return GenerarPdfDesdeZero(dto, webRootPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al generar PDF desde cero: {ex.Message}");
            return GenerarPdfDesdeTemplate(dto, webRootPath);
        }
    }

    private static string GenerarPdfDesdeTemplate(PreFacturaDTO dto, string webRootPath)
    {
        // Ruta de la plantilla PDF existente
        string templatePath = Path.Combine(webRootPath, "PDF", "plantilla_prefactura.pdf");
        // Carpeta de salida (wwwroot/prefacturas)
        string outputDir = Path.Combine(webRootPath, "prefacturas");
        Directory.CreateDirectory(outputDir); // Crea la carpeta si no existe

        // Nombre de archivo único, usando el ID de prefactura para evitar colisiones
        string fileName = $"PreFactura_Template_{dto.IdPreFactura}.pdf";
        string outputPath = Path.Combine(outputDir, fileName);

        try
        {
            // Abrir la plantilla PDF
            using (PdfReader pdfReader = new PdfReader(templatePath))
            using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (PdfStamper pdfStamper = new PdfStamper(pdfReader, fs))
            {
                AcroFields formFields = pdfStamper.AcroFields;
                
                // Listar todos los campos disponibles para diagnóstico
                StringBuilder campos = new StringBuilder();
                foreach (string fieldName in formFields.Fields.Keys)
                {
                    campos.AppendLine($"Campo: {fieldName}");
                }
                Console.WriteLine("Campos disponibles: " + campos.ToString());
                
                // Intentar llenar todos los campos conocidos de la plantilla
                TrySetField(formFields, "No", dto.IdPreFactura.ToString());
                TrySetField(formFields, "PrefacturaID", dto.IdPreFactura.ToString());
                TrySetField(formFields, "Numero", dto.IdPreFactura.ToString());
                
                // Información del cliente
                if (dto.Reservacion?.Usuario != null)
                {
                    TrySetField(formFields, "ClienteNombre", $"{dto.Reservacion.Usuario.Nombre} {dto.Reservacion.Usuario.Apellido}".Trim());
                    TrySetField(formFields, "ClienteEmail", dto.Reservacion.Usuario.Email);
                    TrySetField(formFields, "ClienteDireccion", "Dirección del cliente");
                    TrySetField(formFields, "ClienteTelefono", "809-000-0000");
                }
                
                // Información de la empresa
                TrySetField(formFields, "EmpresaNombre", "CQTRAILS S.A.");
                TrySetField(formFields, "EmpresaDireccion", "Av. Principal #123, Santo Domingo");
                TrySetField(formFields, "EmpresaContacto", "info@cqtrails.com / 809-123-4567");
                
                // Detalles de reservación
                if (dto.Reservacion != null)
                {
                    string fechaInicioStr = dto.Reservacion.FechaInicio.ToString("dd/MM/yyyy");
                    string fechaFinStr = dto.Reservacion.FechaFin.ToString("dd/MM/yyyy");
                    
                    TrySetField(formFields, "FechaReservacion", dto.Reservacion.FechaReservacion?.ToString("dd/MM/yyyy") ?? "");
                    TrySetField(formFields, "FechaInicio", fechaInicioStr);
                    TrySetField(formFields, "FechaFin", fechaFinStr);
                    TrySetField(formFields, "Periodo", $"Del {fechaInicioStr} al {fechaFinStr}");
                }
                
                // Detalles de vehículos
                if (dto.Reservacion?.Vehiculos != null)
                {
                    for (int i = 0; i < dto.Reservacion.Vehiculos.Count && i < 5; i++)
                    {
                        var vehiculo = dto.Reservacion.Vehiculos[i];
                        string prefix = $"Vehiculo{i + 1}";
                        
                        TrySetField(formFields, $"{prefix}_Detalle", $"{vehiculo.Modelo} ({vehiculo.Placa})");
                        TrySetField(formFields, $"{prefix}_Placa", vehiculo.Placa);
                        TrySetField(formFields, $"{prefix}_Modelo", vehiculo.Modelo);
                        TrySetField(formFields, $"{prefix}_Tipo", vehiculo.TipoVehiculo);
                        TrySetField(formFields, $"{prefix}_Capacidad", vehiculo.Capacidad.ToString());
                        TrySetField(formFields, $"{prefix}_Estado", vehiculo.EstadoAsignacion);
                        
                        // Intentar diferentes formatos de campos para vehículos
                        TrySetField(formFields, $"Detalle{i + 1}", $"{vehiculo.Modelo} - {vehiculo.Placa}");
                        TrySetField(formFields, $"Cantidad{i + 1}", "1");
                        TrySetField(formFields, $"Precio{i + 1}", (dto.CostoVehiculo / dto.Reservacion.Vehiculos.Count).ToString("N2"));
                        TrySetField(formFields, $"Total{i + 1}", (dto.CostoVehiculo / dto.Reservacion.Vehiculos.Count).ToString("N2"));
                    }
                }
                
                // Información de costos
                decimal subtotal = dto.CostoVehiculo;
                decimal costoAdicional = dto.CostoAdicional ?? 0;
                decimal subtotalConAdicionales = subtotal + costoAdicional;
                decimal iva = subtotalConAdicionales * 0.18m;
                decimal total = dto.CostoTotal;
                
                TrySetField(formFields, "CostoVehiculo", subtotal.ToString("N2"));
                TrySetField(formFields, "CostoAdicional", costoAdicional.ToString("N2"));
                TrySetField(formFields, "Subtotal", subtotalConAdicionales.ToString("N2"));
                TrySetField(formFields, "IVA", iva.ToString("N2"));
                TrySetField(formFields, "Total", total.ToString("N2"));
                TrySetField(formFields, "PorcentajeIVA", "18%");
                
                // Fechas adicionales
                DateTime fechaActual = DateTime.Now;
                TrySetField(formFields, "FechaEmision", fechaActual.ToString("dd/MM/yyyy"));
                TrySetField(formFields, "FechaVencimiento", fechaActual.AddDays(30).ToString("dd/MM/yyyy"));
                
                // "Flatten" del formulario para que los campos rellenados queden fijos (no editables)
                pdfStamper.FormFlattening = true;
            }

            // Devolver la ruta completa del archivo
            return outputPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al generar PDF con template: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"InnerException: {ex.InnerException.Message}");
            }
            
            throw;
        }
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
            // Crear documento
            Document documento = new Document(PageSize.A4, 36, 36, 54, 36);
            
            using (FileStream fs = new FileStream(outputPath, FileMode.Create))
            using (PdfWriter writer = PdfWriter.GetInstance(documento, fs))
            {
                documento.Open();
                
                // Cargar logo
                try 
                {
                    // Intentar diferentes formatos de logo
                    string[] possibleLogoExtensions = { "png", "jpg", "jpeg", "gif" };
                    string logoPath = null;
                    Image logo = null;
                    
                    foreach (var ext in possibleLogoExtensions)
                    {
                        try
                        {
                            string testPath = Path.Combine(webRootPath, "images", $"logo.{ext}");
                            if (System.IO.File.Exists(testPath))
                            {
                                logoPath = testPath;
                                logo = Image.GetInstance(testPath);
                                break;
                            }
                        }
                        catch (Exception) 
                        {
                            // Continuar con la siguiente extensión
                        }
                    }
                    
                    // Si se encontró un logo válido, usarlo
                    if (logo != null)
                    {
                        logo.ScaleToFit(150, 75);
                        logo.Alignment = Image.ALIGN_RIGHT;
                        documento.Add(logo);
                        Console.WriteLine($"Logo cargado exitosamente: {logoPath}");
                    }
                    else 
                    {
                        // Crear logo de texto como respaldo
                        Paragraph logoText = new Paragraph("CQ TRAILS", 
                            new Font(Font.FontFamily.HELVETICA, 18, Font.BOLD, new BaseColor(0, 150, 0)));
                        logoText.Alignment = Element.ALIGN_RIGHT;
                        documento.Add(logoText);
                        
                        Console.WriteLine("No se encontró un logo válido, usando texto alternativo");
                    }
                }
                catch (Exception ex)
                {
                    // Si hay error al cargar el logo, crear un texto como reemplazo
                    Paragraph logoText = new Paragraph("CQ TRAILS", 
                        new Font(Font.FontFamily.HELVETICA, 18, Font.BOLD, new BaseColor(0, 150, 0)));
                    logoText.Alignment = Element.ALIGN_RIGHT;
                    documento.Add(logoText);
                    
                    Console.WriteLine($"Error al cargar logo: {ex.Message}");
                }
                
                // Título
                Paragraph titulo = new Paragraph("PRE-FACTURA", 
                    new Font(Font.FontFamily.HELVETICA, 24, Font.BOLD, BaseColor.BLACK));
                titulo.Alignment = Element.ALIGN_LEFT;
                documento.Add(titulo);
                
                // Número de prefactura
                Paragraph numeroPrefactura = new Paragraph($"N°: {dto.IdPreFactura}", 
                    new Font(Font.FontFamily.HELVETICA, 12, Font.NORMAL, BaseColor.BLACK));
                numeroPrefactura.Alignment = Element.ALIGN_LEFT;
                documento.Add(numeroPrefactura);
                
                // Espacio
                documento.Add(new Paragraph(" "));
                
                // Crear tabla para la información
                PdfPTable infoTabla = new PdfPTable(2);
                infoTabla.WidthPercentage = 100;
                infoTabla.SetWidths(new float[] { 1f, 1f });
                
                // Datos del cliente
                PdfPCell celdaCliente = new PdfPCell();
                celdaCliente.Border = Rectangle.BOX;
                celdaCliente.BackgroundColor = new BaseColor(240, 240, 240);
                celdaCliente.Padding = 10;
                
                Paragraph tituloCliente = new Paragraph("DATOS DEL CLIENTE", 
                    new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD));
                celdaCliente.AddElement(tituloCliente);
                
                if (dto.Reservacion?.Usuario != null)
                {
                    string nombreCompleto = $"{dto.Reservacion.Usuario.Nombre} {dto.Reservacion.Usuario.Apellido}".Trim();
                    Paragraph nombreClienteP = new Paragraph(nombreCompleto, 
                        new Font(Font.FontFamily.HELVETICA, 10));
                    celdaCliente.AddElement(nombreClienteP);
                    
                    Paragraph emailClienteP = new Paragraph(dto.Reservacion.Usuario.Email, 
                        new Font(Font.FontFamily.HELVETICA, 10));
                    celdaCliente.AddElement(emailClienteP);
                    
                    Paragraph telefonoClienteP = new Paragraph("809-000-0000", 
                        new Font(Font.FontFamily.HELVETICA, 10));
                    celdaCliente.AddElement(telefonoClienteP);
                }
                
                // Datos de la empresa
                PdfPCell celdaEmpresa = new PdfPCell();
                celdaEmpresa.Border = Rectangle.BOX;
                celdaEmpresa.BackgroundColor = new BaseColor(240, 240, 240);
                celdaEmpresa.Padding = 10;
                
                Paragraph tituloEmpresa = new Paragraph("DATOS DE LA EMPRESA", 
                    new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD));
                tituloEmpresa.Alignment = Element.ALIGN_RIGHT;
                celdaEmpresa.AddElement(tituloEmpresa);
                
                Paragraph nombreEmpresaP = new Paragraph("CQTRAILS S.A.", 
                    new Font(Font.FontFamily.HELVETICA, 10));
                nombreEmpresaP.Alignment = Element.ALIGN_RIGHT;
                celdaEmpresa.AddElement(nombreEmpresaP);
                
                Paragraph emailEmpresaP = new Paragraph("info@cqtrails.com", 
                    new Font(Font.FontFamily.HELVETICA, 10));
                emailEmpresaP.Alignment = Element.ALIGN_RIGHT;
                celdaEmpresa.AddElement(emailEmpresaP);
                
                Paragraph telefonoEmpresaP = new Paragraph("809-123-4567", 
                    new Font(Font.FontFamily.HELVETICA, 10));
                telefonoEmpresaP.Alignment = Element.ALIGN_RIGHT;
                celdaEmpresa.AddElement(telefonoEmpresaP);
                
                Paragraph direccionEmpresaP = new Paragraph("Av. Principal #123, Santo Domingo", 
                    new Font(Font.FontFamily.HELVETICA, 10));
                direccionEmpresaP.Alignment = Element.ALIGN_RIGHT;
                celdaEmpresa.AddElement(direccionEmpresaP);
                
                infoTabla.AddCell(celdaCliente);
                infoTabla.AddCell(celdaEmpresa);
                documento.Add(infoTabla);
                
                // Espacio
                documento.Add(new Paragraph(" "));
                
                // Detalles de la reservación
                if (dto.Reservacion != null)
                {
                    PdfPTable reservacionTabla = new PdfPTable(2);
                    reservacionTabla.WidthPercentage = 100;
                    reservacionTabla.SetWidths(new float[] { 1f, 3f });
                    
                    AddTableCell(reservacionTabla, "Reservación:", dto.Reservacion.IdReservacion.ToString());
                    
                    string fechaInicio = dto.Reservacion.FechaInicio.ToString("dd/MM/yyyy");
                    string fechaFin = dto.Reservacion.FechaFin.ToString("dd/MM/yyyy");
                    AddTableCell(reservacionTabla, "Periodo:", $"Del {fechaInicio} al {fechaFin}");
                    
                    if (!string.IsNullOrEmpty(dto.Reservacion.RutaPersonalizada))
                    {
                        AddTableCell(reservacionTabla, "Ruta:", dto.Reservacion.RutaPersonalizada);
                    }
                    
                    if (!string.IsNullOrEmpty(dto.Reservacion.RequerimientosAdicionales))
                    {
                        AddTableCell(reservacionTabla, "Requerimientos:", dto.Reservacion.RequerimientosAdicionales);
                    }
                    
                    documento.Add(reservacionTabla);
                    documento.Add(new Paragraph(" "));
                }
                
                // Tabla de vehículos
                if (dto.Reservacion?.Vehiculos != null && dto.Reservacion.Vehiculos.Any())
                {
                    // Cabecera para los vehículos
                    Paragraph vehiculosTitulo = new Paragraph("DETALLE DE VEHÍCULOS", 
                        new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD));
                    vehiculosTitulo.SpacingAfter = 10;
                    documento.Add(vehiculosTitulo);
                    
                    // Tabla de vehículos
                    PdfPTable vehiculosTabla = new PdfPTable(5);
                    vehiculosTabla.WidthPercentage = 100;
                    vehiculosTabla.SetWidths(new float[] { 2f, 1f, 1f, 0.7f, 1.3f });
                    
                    // Encabezados
                    AddHeaderCell(vehiculosTabla, "Vehículo");
                    AddHeaderCell(vehiculosTabla, "Placa");
                    AddHeaderCell(vehiculosTabla, "Tipo");
                    AddHeaderCell(vehiculosTabla, "Cap.");
                    AddHeaderCell(vehiculosTabla, "Estado");
                    
                    // Filas de vehículos
                    foreach (var vehiculo in dto.Reservacion.Vehiculos)
                    {
                        vehiculosTabla.AddCell(CreateCell($"{vehiculo.Modelo} ({vehiculo.Ano})", Element.ALIGN_LEFT));
                        vehiculosTabla.AddCell(CreateCell(vehiculo.Placa, Element.ALIGN_CENTER));
                        vehiculosTabla.AddCell(CreateCell(vehiculo.TipoVehiculo, Element.ALIGN_CENTER));
                        vehiculosTabla.AddCell(CreateCell(vehiculo.Capacidad.ToString(), Element.ALIGN_CENTER));
                        vehiculosTabla.AddCell(CreateCell(vehiculo.EstadoAsignacion, Element.ALIGN_CENTER));
                    }
                    
                    documento.Add(vehiculosTabla);
                    documento.Add(new Paragraph(" "));
                }
                
                // Tabla de totales
                PdfPTable totalesTabla = new PdfPTable(2);
                totalesTabla.WidthPercentage = 50;
                totalesTabla.HorizontalAlignment = Element.ALIGN_RIGHT;
                totalesTabla.SetWidths(new float[] { 1f, 1f });
                
                decimal subtotal = dto.CostoVehiculo;
                decimal costoAdicional = dto.CostoAdicional ?? 0;
                decimal subtotalConAdicionales = subtotal + costoAdicional;
                decimal iva = subtotalConAdicionales * 0.18m;
                decimal total = dto.CostoTotal;
                
                if (costoAdicional > 0)
                {
                    AddTableCellBold(totalesTabla, "Costo Vehículos:", subtotal.ToString("C"));
                    AddTableCellBold(totalesTabla, "Costo Adicional:", costoAdicional.ToString("C"));
                }
                
                AddTableCellBold(totalesTabla, "Subtotal:", subtotalConAdicionales.ToString("C"));
                AddTableCellBold(totalesTabla, "IVA (18%):", iva.ToString("C"));
                
                // Celda de total con fondo verde
                PdfPCell celdaTotalLabel = new PdfPCell(new Phrase("TOTAL:", 
                    new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.WHITE)));
                celdaTotalLabel.BackgroundColor = new BaseColor(0, 150, 0);
                celdaTotalLabel.HorizontalAlignment = Element.ALIGN_LEFT;
                celdaTotalLabel.Padding = 5;
                totalesTabla.AddCell(celdaTotalLabel);
                
                PdfPCell celdaTotalValor = new PdfPCell(new Phrase(total.ToString("C"), 
                    new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD, BaseColor.WHITE)));
                celdaTotalValor.BackgroundColor = new BaseColor(0, 150, 0);
                celdaTotalValor.HorizontalAlignment = Element.ALIGN_RIGHT;
                celdaTotalValor.Padding = 5;
                totalesTabla.AddCell(celdaTotalValor);
                
                documento.Add(totalesTabla);
                documento.Add(new Paragraph(" "));
                
                // Términos y condiciones
                Paragraph terminosTitulo = new Paragraph("Términos y Condiciones", 
                    new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD));
                documento.Add(terminosTitulo);
                
                Paragraph terminos = new Paragraph(
                    "1. Esta prefactura tiene una validez de 30 días desde su emisión.\n" +
                    "2. El pago debe realizarse antes de la fecha de vencimiento.\n" +
                    "3. Los precios incluyen 18% de ITBIS.\n" +
                    "4. Para más información, contacte a nuestro servicio al cliente.",
                    new Font(Font.FontFamily.HELVETICA, 8));
                documento.Add(terminos);
                
                // Fecha de emisión
                Paragraph fechaEmision = new Paragraph($"Fecha de emisión: {DateTime.Now:dd/MM/yyyy}", 
                    new Font(Font.FontFamily.HELVETICA, 8));
                fechaEmision.Alignment = Element.ALIGN_RIGHT;
                documento.Add(fechaEmision);
                
                documento.Close();
            }
            
            // Devolver la ruta completa del archivo
            return outputPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al generar PDF desde cero: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }
    }
    
    // Métodos auxiliares para la creación de PDF desde cero
    private static void AddTableCell(PdfPTable table, string label, string value)
    {
        Font labelFont = new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD);
        Font valueFont = new Font(Font.FontFamily.HELVETICA, 10);
        
        PdfPCell labelCell = new PdfPCell(new Phrase(label, labelFont));
        labelCell.Border = Rectangle.BOTTOM_BORDER;
        labelCell.PaddingBottom = 5;
        table.AddCell(labelCell);
        
        PdfPCell valueCell = new PdfPCell(new Phrase(value, valueFont));
        valueCell.Border = Rectangle.BOTTOM_BORDER;
        valueCell.PaddingBottom = 5;
        table.AddCell(valueCell);
    }
    
    private static void AddTableCellBold(PdfPTable table, string label, string value)
    {
        Font labelFont = new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD);
        Font valueFont = new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD);
        
        PdfPCell labelCell = new PdfPCell(new Phrase(label, labelFont));
        labelCell.Border = Rectangle.NO_BORDER;
        labelCell.PaddingBottom = 5;
        table.AddCell(labelCell);
        
        PdfPCell valueCell = new PdfPCell(new Phrase(value, valueFont));
        valueCell.Border = Rectangle.NO_BORDER;
        valueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
        valueCell.PaddingBottom = 5;
        table.AddCell(valueCell);
    }
    
    private static void AddHeaderCell(PdfPTable table, string text)
    {
        Font headerFont = new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD, BaseColor.WHITE);
        PdfPCell cell = new PdfPCell(new Phrase(text, headerFont));
        cell.BackgroundColor = new BaseColor(0, 100, 0);
        cell.HorizontalAlignment = Element.ALIGN_CENTER;
        cell.Padding = 5;
        table.AddCell(cell);
    }
    
    private static PdfPCell CreateCell(string text, int alignment)
    {
        Font cellFont = new Font(Font.FontFamily.HELVETICA, 9);
        PdfPCell cell = new PdfPCell(new Phrase(text, cellFont));
        cell.HorizontalAlignment = alignment;
        cell.Padding = 5;
        return cell;
    }
    
    // Método auxiliar para manejar el caso de campos inexistentes en la plantilla
    private static void TrySetField(AcroFields fields, string fieldName, string value)
    {
        try
        {
            fields.SetField(fieldName, value);
        }
        catch (Exception)
        {
            // Si el campo no existe, simplemente continuamos
        }
    }
}
