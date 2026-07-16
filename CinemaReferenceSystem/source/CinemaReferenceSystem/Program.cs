using CinemaReferenceSystem;
using CinemaReferenceSystem.Models;
using CinemaReferenceSystem.Services;
using CinemaReferenceSystem.Utils;

class Program
{
    static async Task Main()
    {
        var db = new DatabaseService(DatabaseConfig.ConnectionString);
        var cinemaService = new CinemaService(db);
        var hallService = new HallService(db);
        var movieService = new MovieService(db);
        var sessionService = new SessionService(db);
        var ticketService = new TicketService(db);
        var authService = new AuthService(db);

        User? currentUser = null;

        // === АВТОРИЗАЦИЯ ===
        while (currentUser == null)
        {
            Console.Clear();
            Console.WriteLine("=== СИСТЕМА КИНОТЕАТРОВ ===");
            Console.WriteLine("1 - Регистрация");
            Console.WriteLine("2 - Вход");
            Console.WriteLine("0 - Выход");
            Console.Write("Выбор: ");

            var choice = Console.ReadLine();
            if (choice == "0") return;

            if (choice == "1")
            {
                await RegisterUser(authService);
            }
            else if (choice == "2")
            {
                currentUser = await LoginUser(authService);
            }
        }

        Console.WriteLine($"\nДобро пожаловать, {currentUser.Username} ({currentUser.Role})!");

        // === ГЛАВНОЕ МЕНЮ ===
        if (currentUser.Role == "admin")
            await AdminMainMenu(cinemaService, hallService, movieService, sessionService, ticketService);
        else
            await UserMainMenu(sessionService, ticketService);
    }

    // ==================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ====================

    static async Task RegisterUser(AuthService authService)
    {
        Console.Write("Логин: ");
        var username = Console.ReadLine() ?? "";
        Console.Write("Пароль: ");
        var password = Console.ReadLine() ?? "";

        var success = await authService.RegisterAsync(username, password);
        Console.WriteLine(success ? "✅ Регистрация прошла успешно!" : "❌ Не удалось зарегистрироваться");
    }

    static async Task<User?> LoginUser(AuthService authService)
    {
        Console.Write("Логин: ");
        var username = Console.ReadLine() ?? "";
        Console.Write("Пароль: ");
        var password = Console.ReadLine() ?? "";

        var user = await authService.LoginAsync(username, password);
        if (user == null)
            Console.WriteLine("❌ Неверный логин или пароль");

        return user;
    }

    static async Task AdminMainMenu(
        CinemaService cinemaService,
        HallService hallService,
        MovieService movieService,
        SessionService sessionService,
        TicketService ticketService)
    {
        var menu = new Dictionary<int, (string, Func<Task>)>
        {
            {1, ("Кинотеатры", () => CinemaMenu(cinemaService))},
            {2, ("Залы", () => HallMenu(hallService, cinemaService))},
            {3, ("Фильмы", () => MovieMenu(movieService))},
            {4, ("Сеансы", () => SessionMenu(sessionService, hallService, movieService, cinemaService, ticketService))},
            {5, ("Билеты (просмотр)", () => TicketMenu(ticketService, sessionService))}
        };

        await MenuHelper.ShowMenuAsync("ГЛАВНОЕ МЕНЮ (ADMIN)", menu, false);
    }

    static async Task UserMainMenu(SessionService sessionService, TicketService ticketService)
    {
        var menu = new Dictionary<int, (string, Func<Task>)>
        {
            {1, ("Просмотр и покупка билетов", () => TicketMenu(ticketService, sessionService))}
        };

        await MenuHelper.ShowMenuAsync("ГЛАВНОЕ МЕНЮ (USER)", menu, false);
    }

    // ==================== МЕНЮ СУЩНОСТЕЙ (с generics + рефлексией) ====================

    static async Task CinemaMenu(CinemaService service)
    {
        var menu = new Dictionary<int, (string, Func<Task>)>
        {
            {1, ("Показать все кинотеатры", async () => ReflectionHelper.PrintList(await service.GetAllAsync(), "КИНОТЕАТРЫ"))},
            {2, ("Добавить кинотеатр", async () => await AddCinema(service))}
        };
        await MenuHelper.ShowMenuAsync("КИНОТЕАТРЫ", menu);
    }

    static async Task HallMenu(HallService hallService, CinemaService cinemaService)
    {
        var menu = new Dictionary<int, (string, Func<Task>)>
        {
            {1, ("Показать все залы", async () => ReflectionHelper.PrintList(await hallService.GetAllAsync(), "ЗАЛЫ"))},
            {2, ("Показать залы кинотеатра", async () => await ShowHallsByCinema(hallService, cinemaService))},
            {3, ("Добавить зал", async () => await AddHall(hallService, cinemaService))}
        };
        await MenuHelper.ShowMenuAsync("ЗАЛЫ", menu);
    }

    static async Task MovieMenu(MovieService service)
    {
        var menu = new Dictionary<int, (string, Func<Task>)>
        {
            {1, ("Показать все фильмы", async () => ReflectionHelper.PrintList(await service.GetAllAsync(), "ФИЛЬМЫ"))},
            {2, ("Добавить фильм", async () => await AddMovie(service))}
        };
        await MenuHelper.ShowMenuAsync("ФИЛЬМЫ", menu);
    }

    static async Task SessionMenu(
        SessionService sessionService,
        HallService hallService,
        MovieService movieService,
        CinemaService cinemaService,
        TicketService ticketService)
    {
        var menu = new Dictionary<int, (string, Func<Task>)>
        {
            {1, ("Показать все сеансы", async () => ReflectionHelper.PrintList(await sessionService.GetDetailedAllAsync(), "СЕАНСЫ"))},
            {2, ("Добавить сеанс", async () => await AddSession(sessionService, hallService, movieService, cinemaService, ticketService))}
        };
        await MenuHelper.ShowMenuAsync("СЕАНСЫ", menu);
    }

    static async Task TicketMenu(TicketService ticketService, SessionService sessionService)
    {
        var menu = new Dictionary<int, (string, Func<Task>)>
        {
            {1, ("Показать билеты сеанса", async () => await ShowTickets(ticketService, sessionService))},
            {2, ("Купить билет", async () => await BuyTicket(ticketService))}
        };
        await MenuHelper.ShowMenuAsync("БИЛЕТЫ", menu);
    }

    // ==================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ ВВОДА ====================

    static async Task AddCinema(CinemaService service)
    {
        Console.Write("Название: "); var name = Console.ReadLine() ?? "";
        Console.Write("Адрес: "); var address = Console.ReadLine() ?? "";
        Console.Write("Телефон: "); var phone = Console.ReadLine();

        await service.AddAsync(new Cinema { Name = name, Address = address, Phone = phone });
        Console.WriteLine("✅ Кинотеатр добавлен!");
    }

    static async Task AddHall(HallService hallService, CinemaService cinemaService)
    {
        // (можно расширить позже)
        Console.Write("ID кинотеатра: ");
        if (!int.TryParse(Console.ReadLine(), out int cinemaId)) return;

        Console.Write("Номер зала: "); if (!int.TryParse(Console.ReadLine(), out int hallNum)) return;
        Console.Write("Рядов: "); if (!int.TryParse(Console.ReadLine(), out int rows)) return;
        Console.Write("Мест в ряду: "); if (!int.TryParse(Console.ReadLine(), out int seatsPerRow)) return;

        var hall = new Hall
        {
            CinemaId = cinemaId,
            HallNumber = hallNum,
            RowsCount = rows,
            SeatsPerRow = seatsPerRow
        };

        await hallService.AddAsync(hall);
        Console.WriteLine("✅ Зал добавлен!");
    }

    static async Task AddMovie(MovieService service)
    {
        Console.Write("Название: "); var title = Console.ReadLine() ?? "";
        Console.Write("Жанр: "); var genre = Console.ReadLine();
        Console.Write("Длительность (мин): "); if (!int.TryParse(Console.ReadLine(), out int dur)) return;
        Console.Write("Режиссёр: "); var director = Console.ReadLine();

        await service.AddAsync(new Movie { Title = title, Genre = genre, DurationMinutes = dur, Director = director });
        Console.WriteLine("✅ Фильм добавлен!");
    }

    static async Task AddSession(SessionService ss, HallService hs, MovieService ms, CinemaService cs, TicketService ts)
    {
        // (оставил вашу логику, но теперь через рефлексию)
        Console.Write("ID кинотеатра: "); if (!int.TryParse(Console.ReadLine(), out int cid)) return;
        Console.Write("Номер зала: "); if (!int.TryParse(Console.ReadLine(), out int hn)) return;

        var hallId = await hs.GetHallIdAsync(cid, hn);
        if (hallId == null) { Console.WriteLine("Зал не найден!"); return; }

        Console.Write("ID фильма: "); if (!int.TryParse(Console.ReadLine(), out int mid)) return;
        Console.Write("Дата и время (dd.MM.yyyy HH:mm): "); if (!DateTime.TryParse(Console.ReadLine(), out DateTime time)) return;
        Console.Write("Цена: "); if (!decimal.TryParse(Console.ReadLine(), out decimal price)) return;

        var session = new Session { HallId = hallId.Value, MovieId = mid, StartTime = time, Price = price };
        int sessionId = await ss.AddAsync(session);

        var layout = await hs.GetHallLayoutAsync(session.HallId);
        if (layout != null)
            await ts.GenerateTicketsAsync(sessionId, layout.Value.rows, layout.Value.seatsPerRow);

        Console.WriteLine("✅ Сеанс и билеты созданы!");
    }

    static async Task ShowHallsByCinema(HallService hallService, CinemaService cinemaService)
    {
        Console.Write("ID кинотеатра: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) return;

        var halls = await hallService.GetByCinemaIdAsync(id);
        ReflectionHelper.PrintList(halls, $"ЗАЛЫ КИНОТЕАТРА {id}");
    }

    static async Task ShowTickets(TicketService ticketService, SessionService sessionService)
    {
        Console.Write("ID сеанса: ");
        if (!int.TryParse(Console.ReadLine(), out int sid)) return;

        var tickets = await ticketService.GetBySessionIdAsync(sid);
        ReflectionHelper.PrintList(tickets, $"БИЛЕТЫ СЕАНСА {sid}");
    }

    static async Task BuyTicket(TicketService ticketService)
    {
        Console.Write("ID сеанса: "); if (!int.TryParse(Console.ReadLine(), out int sid)) return;
        Console.Write("Ряд: "); if (!int.TryParse(Console.ReadLine(), out int row)) return;
        Console.Write("Место: "); if (!int.TryParse(Console.ReadLine(), out int seat)) return;

        var success = await ticketService.SellTicketAsync(sid, row, seat);
        Console.WriteLine(success ? "✅ Билет продан!" : "❌ Не удалось продать билет");
    }
}