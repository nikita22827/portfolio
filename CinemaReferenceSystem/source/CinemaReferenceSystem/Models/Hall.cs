using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaReferenceSystem.Models;

public class Hall : BaseEntity
{
    public int CinemaId { get; set; }
    public int HallNumber { get; set; }
    public int SeatsCount { get; set; }
    public int RowsCount { get; set; }
    public int SeatsPerRow { get; set; }

    public override string ToString() => $"Зал №{HallNumber} (мест: {SeatsCount})";
}