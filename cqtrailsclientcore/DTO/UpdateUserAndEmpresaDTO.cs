namespace cqtrailsclientcore.DTO;

public class UpdateUserAndEmpresaDTO
{
    public string CurrentEmail { get; set; }
    public string NewEmail { get; set; }
    public string NewNombre { get; set; }
    public string NewApellido { get; set; }
    public string? NewEmpresaNombre { get; set; } // Nuevo nombre de la empresa (opcional)
    public string? NewEmpresaTelefono { get; set; } // Nuevo teléfono de la empresa (opcional)
}