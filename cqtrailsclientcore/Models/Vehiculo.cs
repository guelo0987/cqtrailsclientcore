using System;
using System.Collections.Generic;

namespace cqtrailsclientcore.Models;

public partial class Vehiculo
{
    public int IdVehiculo { get; set; }

    public string Placa { get; set; } = null!;

    public string Modelo { get; set; } = null!;

    public string TipoVehiculo { get; set; } = null!;

    public int Capacidad { get; set; }

    public int Ano { get; set; }

    public bool? Disponible { get; set; }
    
    public int Price { get; set; }
    
    public string? Image_url { get; set; }
    
    public string? Transmision { get; set; }
    
    public string? Combustible { get; set; }
    

    public virtual ICollection<VehiculosReservacione> VehiculosReservaciones { get; set; } = new List<VehiculosReservacione>();
}
