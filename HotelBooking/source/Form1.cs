using System;
using System.Linq;
using System.Windows.Forms;
using HotelBooking.Enums;
using HotelBooking.Models;
using HotelBooking.Services;

namespace HotelBooking
{
    public partial class Form1 : Form
    {
        private HotelService hotelService;

        public Form1()
        {
            InitializeComponent();
            hotelService = new HotelService();
            ConfigureGrids();
            UpdateUI();
        }

        private string GetRoomTypeName(RoomType type)
        {
            switch (type)
            {
                case RoomType.Single: return "Одноместный";
                case RoomType.Double: return "Двухместный";
                case RoomType.Triple: return "Трёхместный";
                case RoomType.Vip: return "VIP";
                default: return type.ToString();
            }
        }

        private void ConfigureGrids()
        {
            dgvRooms.AutoGenerateColumns = false;
            dgvRooms.Columns.Clear();
            dgvRooms.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Номер", DataPropertyName = "Id", Width = 50 });
            dgvRooms.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Тип", DataPropertyName = "Type", Width = 80 });
            dgvRooms.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Цена", DataPropertyName = "Price", Width = 60 });
            dgvRooms.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Выезд", DataPropertyName = "CheckOutDate", Width = 80 });
            dgvRooms.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Осталось дней", DataPropertyName = "DaysLeft", Width = 80 });

            dgvOrders.AutoGenerateColumns = false;
            dgvOrders.Columns.Clear();
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "Id", Width = 40 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Тип", DataPropertyName = "Type", Width = 70 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Длит.", DataPropertyName = "Duration", Width = 50 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Поступил", DataPropertyName = "Arrival", Width = 80 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Дедлайн", DataPropertyName = "Deadline", Width = 80 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Статус", DataPropertyName = "Status", Width = 80 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Комнаты", DataPropertyName = "Room", Width = 100 });

            dgvStatistics.AutoGenerateColumns = true;
        }

        private void UpdateUI()
        {
            var hotel = hotelService.Hotel;
            DateTime today = DateTime.Today.AddDays(hotel.CurrentDay - 1);
            DateTime lastProcessed = hotel.LastProcessedDate;

            lblDay.Text = $"Дата: {today:dd.MM.yyyy}";

            dgvRooms.DataSource = hotel.Rooms.Select(r =>
            {
                var active = r.Reservations.FirstOrDefault(res => res.CheckIn <= today && res.CheckOut >= today);
                string checkOutStr = active == null ? "Свободен" : active.CheckOut.ToShortDateString();
                int daysLeft = active == null ? 0 : (active.CheckOut - today).Days + 1;
                return new
                {
                    Id = r.Id,
                    Type = GetRoomTypeName(r.Type),
                    Price = r.PricePerDay,
                    CheckOutDate = checkOutStr,
                    DaysLeft = daysLeft
                };
            }).ToList();

            var activeOrders = hotel.Orders
                .Where(o => !o.IsAssigned && o.Deadline.Date >= today.Date)
                .OrderBy(o => (int)o.Duration)
                .ThenBy(o => o.Id)
                .Select(o => new
                {
                    Id = o.Id,
                    Type = GetRoomTypeName(o.Type),
                    Duration = (int)o.Duration,
                    Arrival = o.ArrivalDate.ToShortDateString(),
                    Deadline = o.Deadline.ToShortDateString(),
                    Status = "Ожидает",
                    Room = "—"
                });

            var assignedToday = hotel.Orders
                .Where(o => o.IsAssigned && o.AssignedDate.HasValue && o.AssignedDate.Value.Date == lastProcessed.Date)
                .OrderBy(o => (int)o.Duration)
                .ThenBy(o => o.Id)
                .Select(o =>
                {
                    string roomsStr = o.AssignedRoomIds.Count > 0
                        ? string.Join(", ", o.AssignedRoomIds)
                        : "?";
                    return new
                    {
                        Id = o.Id,
                        Type = GetRoomTypeName(o.Type),
                        Duration = (int)o.Duration,
                        Arrival = o.ArrivalDate.ToShortDateString(),
                        Deadline = o.Deadline.ToShortDateString(),
                        Status = "Заселён",
                        Room = roomsStr
                    };
                });

            var allDisplayOrders = activeOrders.Concat(assignedToday).ToList();
            dgvOrders.DataSource = allDisplayOrders;

            dgvStatistics.DataSource = new[]
            {
                new
                {
                    Всего = hotel.Statistics.TotalOrders,
                    Принято = hotel.Statistics.AcceptedOrders,
                    Отклонено = hotel.Statistics.RejectedOrders,
                    Доход = hotel.Statistics.TotalIncome
                }
            };
        }

        private void btnNextDay_Click(object sender, EventArgs e)
        {
            hotelService.NextDay();
            UpdateUI();

            var h = hotelService.Hotel;
            DateTime yesterday = h.LastProcessedDate;
            rtbLog.AppendText($"Дата: {yesterday:dd.MM.yyyy}\n");
            rtbLog.AppendText($"Всего заказов: {h.Statistics.TotalOrders}\n");
            rtbLog.AppendText($"Принято: {h.Statistics.AcceptedOrders}\n");
            rtbLog.AppendText($"Отклонено: {h.Statistics.RejectedOrders}\n");
            rtbLog.AppendText($"Доход: {h.Statistics.TotalIncome}\n");
            rtbLog.AppendText("----------------------\n");
        }
    }
}