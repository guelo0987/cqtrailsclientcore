namespace cqtrailsclientcore.Models;

public class DetalleCarrito
{
    public int id { get; set; }
    public int CarritoId { get; set; }

    public int VehiculoId { get; set; }
    public int Price { get; set; }
    public int Cantidad { get; set; }

    public DateOnly FechaInicio{ get; set; }
    public DateOnly FechaFin{ get; set; }
    public int SubTotal { get; set; }
    public int Total { get; set; }
    
    // Navigation property
    public virtual Carrito Carrito { get; set; }
    public virtual Vehiculo Vehiculo { get; set; }
}