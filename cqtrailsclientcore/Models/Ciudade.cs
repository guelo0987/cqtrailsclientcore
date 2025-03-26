using System;
using System.Collections.Generic;

namespace cqtrailsclientcore.Models;

public partial class Ciudade
{
    public int IdCiudad { get; set; }

    public string Nombre { get; set; } = null!;

    public string Estado { get; set; } = null!;
}
