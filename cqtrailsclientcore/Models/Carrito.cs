namespace cqtrailsclientcore.Models;

public class Carrito
{
    public int id { get; set; }
    public DateTime fecha_creacion { get; set; }
    public int usuario_id { get; set; }
    
    // Add navigation property
    public virtual Usuario Usuario { get; set; }
}