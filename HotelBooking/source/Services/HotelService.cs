using HotelBooking.Algorithms;
using HotelBooking.Models;
using System;
using System.Linq;

namespace HotelBooking.Services
{
    public class HotelService
    {
        private readonly Hotel hotel;
        private readonly IBookingAlgorithm algorithm;
        private readonly OrderGenerator generator;

        public Hotel Hotel => hotel;

        public HotelService()
        {
            hotel = new Hotel();
            hotel.InitializeRooms();
            hotel.InitializeReservations();

            algorithm = new GreedyBookingAlgorithm();
            generator = new OrderGenerator();
        }

        public void NextDay()
        {
            DateTime today = DateTime.Today.AddDays(hotel.CurrentDay - 1);

            var newOrders = generator.GenerateOrders(hotel.Rooms, today);

            hotel.NewOrders = newOrders;
            hotel.Orders.AddRange(newOrders);

            hotel.Statistics.TotalOrders += newOrders.Count;

            algorithm.ProcessOrders(hotel.Orders, hotel.Rooms, today);

            // Запоминаем дату, которую только что обработали
            hotel.LastProcessedDate = today;

            hotel.Statistics.AcceptedOrders = hotel.Orders.Count(o => o.IsAssigned);
            hotel.Statistics.RejectedOrders = hotel.Orders.Count(o =>
                !o.IsAssigned && o.Deadline < today);
            hotel.Statistics.TotalIncome = CalculateIncome();

            hotel.CurrentDay++;
        }

        private decimal CalculateIncome()
        {
            decimal income = 0;
            foreach (var room in hotel.Rooms)
            {
                foreach (var res in room.Reservations)
                {
                    int days = (res.CheckOut - res.CheckIn).Days;
                    income += days * room.PricePerDay;
                }
            }
            return income;
        }
    }
}