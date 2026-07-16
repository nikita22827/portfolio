using HotelBooking.Enums;
using System.Collections.Generic;

namespace HotelBooking.Models
{
    public class Room
    {
        public int Id { get; set; }

        public RoomType Type { get; set; }

        public decimal PricePerDay { get; set; }

        public List<Reservation> Reservations { get; set; }
            = new List<Reservation>();

        public override string ToString()
        {
            return $"{Id} ({Type})";
        }
    }
}