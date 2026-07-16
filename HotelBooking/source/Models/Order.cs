using HotelBooking.Enums;
using System;
using System.Collections.Generic;

namespace HotelBooking.Models
{
    public class Order
    {
        private static int nextId = 1;

        public int Id { get; }
        public RoomType Type { get; set; }
        public StayDuration Duration { get; set; }
        public DateTime ArrivalDate { get; set; }

        public DateTime Deadline => ArrivalDate.AddDays(3);

        public bool IsAssigned { get; set; }
        public DateTime? AssignedDate { get; set; }
        public List<int> AssignedRoomIds { get; set; } = new List<int>();

        public Order()
        {
            Id = nextId++;
        }
    }
}