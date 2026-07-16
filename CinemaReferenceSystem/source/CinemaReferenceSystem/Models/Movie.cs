using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaReferenceSystem.Models;

public class Movie : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Genre { get; set; }
    public int DurationMinutes { get; set; }
    public string? Director { get; set; }

    public override string ToString() => $"Фильм: {Title} ({DurationMinutes} мин)";
}