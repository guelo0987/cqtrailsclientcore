using System;
using System.Collections.Generic;

namespace cqtrailsclientcore.Models;

public partial class PreFactura
{
    public int IdPreFactura { get; set; }

    public int IdReservacion { get; set; }

    public decimal CostoVehiculo { get; set; }

    public decimal? CostoAdicional { get; set; }

    public decimal CostoTotal { get; set; }

    public DateTime? FechaGeneracion { get; set; }

    public string? ArchivoPdf { get; set; }

    public virtual Reservacione IdReservacionNavigation { get; set; } = null!;
}
