using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaReferenceSystem.Models;

public class Ticket : BaseEntity
{
    public int SessionId { get; set; }
    public int RowNum { get; set; }
    public int SeatNum { get; set; }
    public bool IsSold { get; set; } = false;

    public override string ToString() => $"Билет: ряд {RowNum}, место {SeatNum} (продано: {IsSold})";
}