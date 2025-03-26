using System;
using System.Collections.Generic;

namespace cqtrailsclientcore.Models;

public partial class Notificacione
{
    public int IdNotificacion { get; set; }

    public int IdReservacion { get; set; }

    public string TipoNotificacion { get; set; } = null!;

    public DateTime? FechaEnvio { get; set; }

    public virtual Reservacione IdReservacionNavigation { get; set; } = null!;
}
