namespace cqtrailsclientcore.DTO;

public class CarritoDTO
{
    public int Id { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int UsuarioId { get; set; }
    public UsuarioBasicoDTO Usuario { get; set; }
}

public class DetalleCarritoResponseDTO
{
    public int Id { get; set; }
    public int CarritoId { get; set; }
    public int VehiculoId { get; set; }
    public int Price { get; set; }
    public int Cantidad { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public int SubTotal { get; set; }
    public int Total { get; set; }
    
    // Información básica del vehículo
    public VehiculoBasicoDTO Vehiculo { get; set; }
}

public class VehiculoBasicoDTO
{
    public int IdVehiculo { get; set; }
    public string Placa { get; set; }
    public string Modelo { get; set; }
    public string TipoVehiculo { get; set; }
    public int Capacidad { get; set; }
    public int Ano { get; set; }
    public int Price { get; set; }
} 