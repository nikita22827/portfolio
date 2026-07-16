using System;
using System.Collections.Generic;
using HotelBooking.Models;

namespace HotelBooking.Algorithms
{
    public interface IBookingAlgorithm
    {
        void ProcessOrders(
            List<Order> orders,
            List<Room> rooms,
            DateTime today);
    }
}