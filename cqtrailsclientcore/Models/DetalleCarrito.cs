namespace cqtrailsclientcore.Models;

public class DetalleCarrito
{
    public int id { get; set; }
    public int CarritoId { get; set; }

    public int VehiculoId { get; set; }
    public int Price { get; set; }
    public int Cantidad { get; set; }

    public DateTime FechaInicio{ get; set; }
    public DateTime FechaFin{ get; set; }
    public int SubTotal { get; set; }
    public int Total { get; set; }
    
    // New columns added to match DB schema
    public int CiudadInicioId { get; set; }
    public int CiudadFinId { get; set; }
    
    // Navigation property
    public virtual Carrito Carrito { get; set; }
    public virtual Vehiculo Vehiculo { get; set; }
    public virtual Ciudade CiudadInicio { get; set; }
    public virtual Ciudade CiudadFin { get; set; }
}