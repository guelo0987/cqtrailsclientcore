using System;
using System.Collections.Generic;

namespace cqtrailsclientcore.Models;

public partial class Permiso
{
    public int IdPermiso { get; set; }

    public string NombrePermiso { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<Role> IdRols { get; set; } = new List<Role>();
}
