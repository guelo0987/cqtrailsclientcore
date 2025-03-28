namespace cqtrailsclientcore.DTO;

public class DetalleCarritoDTO
{
    public int UsuarioId { get; set; }
    public int VehiculoId { get; set; }
    public int Cantidad { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
} 