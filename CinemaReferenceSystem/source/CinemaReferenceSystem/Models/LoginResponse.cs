using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaReferenceSystem.Models;

public class LoginResponse
{
    public bool Success { get; set; }
    public string? UserRole { get; set; }  // будет "admin" или "user"
}
