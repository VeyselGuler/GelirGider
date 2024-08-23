using System.ComponentModel.DataAnnotations;

namespace GelirGider.Models;

public class Register
{
    public int Id { get; set; }
    [Required] public string Name { get; set; }
    [Required] public string Pw { get; set; }
    [Required] public string Pwconfirmend { get; set; }
    [Required] public string Mail { get; set; }
    public DateTime Created { get; set; }
}

public class Login
{
    public int Id { get; set; }
    [Required] public string Mail { get; set; }

    public string? Name { get; set; }
    [Required] public string Pw { get; set; }
}

public class ResetPwToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; }
    public DateTime Created { get; set; }
    public bool Used { get; set; }
}

public class PwReset
{
    [Required] public string Token { get; set; }
    [Required] public string Pw { get; set; }
    [Required] public string PwConfirmend { get; set; }
}