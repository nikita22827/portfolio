using System;
using System.Collections.Generic;
using System.Linq;
using HotelBooking.Enums;
using HotelBooking.Models;

namespace HotelBooking.Algorithms
{
    public class GreedyBookingAlgorithm : IBookingAlgorithm
    {
        public void ProcessOrders(List<Order> orders, List<Room> rooms, DateTime today)
        {
            var sortedOrders = orders
                .Where(o => !o.IsAssigned)
                .OrderBy(o => (int)o.Duration)
                .ThenBy(o => o.Id)
                .ToList();

            foreach (var order in sortedOrders)
            {
                bool placed = false;

                for (DateTime start = today; start <= order.Deadline && !placed; start = start.AddDays(1))
                {
                    int duration = (int)order.Duration;

                    placed = TrySingleRoom(order, rooms, start, duration);
                    if (placed) break;

                    if (!placed && (order.Type == RoomType.Double || order.Type == RoomType.Triple))
                    {
                        placed = TryMultipleRooms(order, rooms, start, duration);
                        if (placed) break;
                    }
                }
            }
        }

        private bool TrySingleRoom(Order order, List<Room> rooms, DateTime start, int duration)
        {
            var allowedTypes = GetAllowedTypes(order.Type);
            foreach (var type in allowedTypes)
            {
                var freeRoom = rooms
                    .Where(r => r.Type == type)
                    .FirstOrDefault(r => IsRoomFree(r, start, duration));
                if (freeRoom != null)
                {
                    BookRoom(freeRoom, order, start, duration);
                    return true;
                }
            }
            return false;
        }

        private bool TryMultipleRooms(Order order, List<Room> rooms, DateTime start, int duration)
        {
            if (order.Type == RoomType.Double)
            {
                var singleRooms = rooms
                    .Where(r => r.Type == RoomType.Single && IsRoomFree(r, start, duration))
                    .Take(2)
                    .ToList();
                if (singleRooms.Count == 2)
                {
                    foreach (var r in singleRooms)
                        BookRoom(r, order, start, duration);
                    return true;
                }
            }
            else if (order.Type == RoomType.Triple)
            {
                var single = rooms
                    .FirstOrDefault(r => r.Type == RoomType.Single && IsRoomFree(r, start, duration));
                var doubleRoom = rooms
                    .FirstOrDefault(r => r.Type == RoomType.Double && IsRoomFree(r, start, duration));
                if (single != null && doubleRoom != null)
                {
                    BookRoom(single, order, start, duration);
                    BookRoom(doubleRoom, order, start, duration);
                    return true;
                }

                var threeSingles = rooms
                    .Where(r => r.Type == RoomType.Single && IsRoomFree(r, start, duration))
                    .Take(3)
                    .ToList();
                if (threeSingles.Count == 3)
                {
                    foreach (var r in threeSingles)
                        BookRoom(r, order, start, duration);
                    return true;
                }
            }
            return false;
        }

        private List<RoomType> GetAllowedTypes(RoomType requested)
        {
            switch (requested)
            {
                case RoomType.Single:
                    return new List<RoomType> { RoomType.Single, RoomType.Double };
                case RoomType.Double:
                    return new List<RoomType> { RoomType.Double, RoomType.Triple };
                case RoomType.Triple:
                    return new List<RoomType> { RoomType.Triple, RoomType.Vip };
                case RoomType.Vip:
                    return new List<RoomType> { RoomType.Vip };
                default:
                    return new List<RoomType> { requested };
            }
        }

        private bool IsRoomFree(Room room, DateTime start, int duration)
        {
            DateTime end = start.AddDays(duration);
            foreach (var res in room.Reservations)
            {
                if (start < res.CheckOut && end > res.CheckIn)
                    return false;
            }
            return true;
        }

        private void BookRoom(Room room, Order order, DateTime start, int duration)
        {
            room.Reservations.Add(new Reservation
            {
                OrderId = order.Id,
                CheckIn = start,
                CheckOut = start.AddDays(duration)
            });
            order.IsAssigned = true;
            order.AssignedDate = start;
            order.AssignedRoomIds.Add(room.Id);
        }
    }
}