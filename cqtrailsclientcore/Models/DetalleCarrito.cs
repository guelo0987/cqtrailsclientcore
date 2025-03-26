namespace cqtrailsclientcore.Models;

public class DetalleCarrito
{
    public int id { get; set; }
    public int CarritoId { get; set; }
    public int Price { get; set; }
    public int Cantidad { get; set; }
    
    // Navigation property
    public virtual Carrito Carrito { get; set; }
}