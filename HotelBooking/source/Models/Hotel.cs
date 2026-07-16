using HotelBooking.Enums;
using System;
using System.Collections.Generic;

namespace HotelBooking.Models
{
    public class Hotel
    {
        public List<Room> Rooms { get; set; }
        public List<Order> Orders { get; set; }
        public List<Order> NewOrders { get; set; }
        public Statistics Statistics { get; set; }
        public int CurrentDay { get; set; }
        public DateTime LastProcessedDate { get; set; }

        public Hotel()
        {
            Rooms = new List<Room>();
            Orders = new List<Order>();
            NewOrders = new List<Order>();
            Statistics = new Statistics();
            CurrentDay = 1;
            LastProcessedDate = DateTime.Today.AddDays(-1);
        }

        public void InitializeRooms()
        {
            int id = 1;
            for (int i = 0; i < 30; i++)
                Rooms.Add(new Room { Id = id++, Type = RoomType.Single, PricePerDay = 3000 });
            for (int i = 0; i < 20; i++)
                Rooms.Add(new Room { Id = id++, Type = RoomType.Double, PricePerDay = 5000 });
            for (int i = 0; i < 10; i++)
                Rooms.Add(new Room { Id = id++, Type = RoomType.Triple, PricePerDay = 7000 });
            for (int i = 0; i < 5; i++)
                Rooms.Add(new Room { Id = id++, Type = RoomType.Vip, PricePerDay = 15000 });
        }

        public void InitializeReservations()
        {
            DateTime today = DateTime.Today;
            for (int i = 0; i < 3; i++)
                Rooms[i].Reservations.Add(new Reservation
                {
                    OrderId = 0,
                    CheckIn = today,
                    CheckOut = today.AddDays(2)
                });
            for (int i = 30; i < 32; i++)
                Rooms[i].Reservations.Add(new Reservation
                {
                    OrderId = 0,
                    CheckIn = today,
                    CheckOut = today.AddDays(7)
                });
            Rooms[60].Reservations.Add(new Reservation
            {
                OrderId = 0,
                CheckIn = today,
                CheckOut = today.AddDays(30)
            });
        }
    }
}