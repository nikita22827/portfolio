namespace CinemaReferenceSystem.Models;

public class HallDetails : BaseEntity
{
    public string CinemaName { get; set; } = string.Empty;
    public int HallNumber { get; set; }
    public int RowsCount { get; set; }
    public int MaxSeatsPerRow { get; set; }
}