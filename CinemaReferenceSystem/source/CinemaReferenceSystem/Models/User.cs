using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaReferenceSystem.Models;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;  // сюда будем отправлять plain-пароль — триггер в БД сам захэширует
    public string Role { get; set; } = "user"; // "user" или "admin"

    public override string ToString() => $"Пользователь: {Username} (роль: {Role})";
}