public class RoleKey
{
    public int Id { get; set; }
    public string HashedKey { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Secretaria" o "Medico"
}