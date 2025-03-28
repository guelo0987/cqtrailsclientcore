using System;
using System.Collections.Generic;

namespace cqtrailsclientcore.Models;

public partial class Reservacione
{
    public int IdReservacion { get; set; }

    public int? IdUsuario { get; set; }

    public int? IdEmpleado { get; set; }

    public int? IdEmpresa { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public string? RutaPersonalizada { get; set; }

    public string? RequerimientosAdicionales { get; set; }

    public string? Estado { get; set; }

    public DateTime? FechaReservacion { get; set; }

    public DateTime? FechaConfirmacion { get; set; }
    
    public int Total { get; set; }
    
    public int SubTotal { get; set; }

    public virtual Empleado? IdEmpleadoNavigation { get; set; }

    public virtual Empresa? IdEmpresaNavigation { get; set; }

    public virtual Usuario? IdUsuarioNavigation { get; set; }

    public virtual ICollection<Notificacione> Notificaciones { get; set; } = new List<Notificacione>();

    public virtual ICollection<PreFactura> PreFacturas { get; set; } = new List<PreFactura>();

    public virtual ICollection<VehiculosReservacione> VehiculosReservaciones { get; set; } = new List<VehiculosReservacione>();
}
