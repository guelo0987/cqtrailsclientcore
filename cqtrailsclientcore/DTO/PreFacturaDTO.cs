namespace cqtrailsclientcore.DTO;

public class PreFacturaDTO
{
    public int IdPreFactura { get; set; }
    public int IdReservacion { get; set; }
    public decimal CostoVehiculo { get; set; }
    public decimal? CostoAdicional { get; set; }
    public decimal CostoTotal { get; set; }
    public DateTime? FechaGeneracion { get; set; }
    public string? ArchivoPdf { get; set; }
    
    // Información básica de la reservación
    public ReservacioneDTO? Reservacion { get; set; }
} 