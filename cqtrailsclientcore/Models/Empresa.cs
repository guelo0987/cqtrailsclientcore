using System;
using System.Collections.Generic;

namespace cqtrailsclientcore.Models;

public partial class Empresa
{
    public int IdEmpresa { get; set; }

    public string Nombre { get; set; } = null!;

    public string ContactoEmail { get; set; } = null!;

    public string ContactoTelefono { get; set; } = null!;

    public DateTime? FechaRegistro { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();

    public virtual ICollection<Reservacione> Reservaciones { get; set; } = new List<Reservacione>();
}
