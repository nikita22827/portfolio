using System;

namespace HotelBooking.Models
{
    public class Reservation
    {
        public int OrderId { get; set; }

        public DateTime CheckIn { get; set; }

        public DateTime CheckOut { get; set; }
    }
}