namespace cqtrailsclientcore.DTO;

public class UsuarioDTO
{
    
  
    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;
    
    public string NombreEmpresa { get; set; } = null!; 
    public string ContactoEmail { get; set; } = null!;
    public string ContactoTelefono { get; set; } = null!;
}
    


