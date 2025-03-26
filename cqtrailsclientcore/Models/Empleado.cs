using System;
using System.Collections.Generic;

namespace cqtrailsclientcore.Models;

public partial class Empleado
{
    public int IdEmpleado { get; set; }

    public int IdEmpresa { get; set; }

    public int IdUsuario { get; set; }

    public virtual Empresa IdEmpresaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<Reservacione> Reservaciones { get; set; } = new List<Reservacione>();
}
