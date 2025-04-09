namespace cqtrailsclientcore.DTO;

public class DetalleCarritoDTO
{
    public int UsuarioId { get; set; }
    public int VehiculoId { get; set; }
    public int Cantidad { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int CiudadInicioId { get; set; }
    public int CiudadFinId { get; set; }
} 