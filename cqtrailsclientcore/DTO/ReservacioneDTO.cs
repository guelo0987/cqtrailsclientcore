namespace cqtrailsclientcore.DTO;

public class ReservacioneDTO
{
    public int IdReservacion { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string? RutaPersonalizada { get; set; }
    public string? RequerimientosAdicionales { get; set; }
    public string? Estado { get; set; }
    public DateTime? FechaReservacion { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public int Total { get; set; }
    public int SubTotal { get; set; }
    public int? CiudadInicioId { get; set; }
    public int? CiudadFinId { get; set; }
    
    // Información básica del usuario
    public UsuarioBasicoDTO? Usuario { get; set; }
    
    // Lista de vehículos en la reservación
    public List<VehiculoReservacionDTO>? Vehiculos { get; set; }
}

public class UsuarioBasicoDTO
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = null!;
    public string Apellido { get; set; } = null!;
    public string Email { get; set; } = null!;
}

public class VehiculoReservacionDTO
{
    public int IdVehiculo { get; set; }
    public string Placa { get; set; } = null!;
    public string Modelo { get; set; } = null!;
    public string TipoVehiculo { get; set; } = null!;
    public int Capacidad { get; set; }
    public int Ano { get; set; }
    public string? EstadoAsignacion { get; set; }
    public string? ImageUrl { get; set; }
}