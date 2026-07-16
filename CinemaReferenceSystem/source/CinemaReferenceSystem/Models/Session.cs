using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaReferenceSystem.Models;

public class Session : BaseEntity
{
    public int HallId { get; set; }
    public int MovieId { get; set; }
    public DateTime StartTime { get; set; }
    public decimal Price { get; set; }

    public override string ToString() => $"Сеанс {StartTime:dd.MM.yyyy HH:mm} — цена {Price} руб.";
}