using HotelBooking.Enums;
using HotelBooking.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HotelBooking.Services
{
    public class OrderGenerator
    {
        private Random random = new Random();

        public List<Order> GenerateOrders(List<Room> rooms, DateTime today)
        {
            List<Order> orders = new List<Order>();

            var freeCounts = new Dictionary<RoomType, int>();
            foreach (RoomType type in Enum.GetValues(typeof(RoomType)))
            {
                int free = rooms.Count(r =>
                    r.Type == type &&
                    !r.Reservations.Any(res =>
                        res.CheckIn <= today && res.CheckOut > today));
                freeCounts[type] = free;
            }

            foreach (RoomType type in Enum.GetValues(typeof(RoomType)))
            {
                int free = freeCounts[type];
                int excess = free == 0 ? 1 : random.Next(1, 3);
                int orderCount = free + excess;

                for (int i = 0; i < orderCount; i++)
                {
                    StayDuration duration;
                    int val = random.Next(3);
                    switch (val)
                    {
                        case 0: duration = StayDuration.Day; break;
                        case 1: duration = StayDuration.Week; break;
                        default: duration = StayDuration.Month; break;
                    }

                    orders.Add(new Order
                    {
                        Type = type,
                        Duration = duration,
                        ArrivalDate = today
                    });
                }
            }

            return orders;
        }
    }
}