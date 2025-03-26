using System;
using System.Collections.Generic;

namespace cqtrailsclientcore.Models;

public partial class VehiculosReservacione
{
    public int IdVehiculo { get; set; }

    public int IdReservacion { get; set; }

    public DateTime? FechaAsignacion { get; set; }

    public string? EstadoAsignacion { get; set; }

    public virtual Reservacione IdReservacionNavigation { get; set; } = null!;

    public virtual Vehiculo IdVehiculoNavigation { get; set; } = null!;
}
