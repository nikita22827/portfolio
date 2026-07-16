namespace CinemaReferenceSystem.Utils;

public static class MenuHelper
{
    public static async Task ShowMenuAsync(
        string title,
        Dictionary<int, (string Description, Func<Task> Action)> menuItems,
        bool clearScreen = true)
    {
        while (true)
        {
            if (clearScreen) Console.Clear();

            Console.WriteLine($"=== {title} ===");
            foreach (var item in menuItems.OrderBy(x => x.Key))
            {
                Console.WriteLine($"{item.Key} - {item.Value.Description}");
            }
            Console.WriteLine("0 - Назад / Выход");
            Console.Write("\nВыбор: ");

            var input = Console.ReadLine()?.Trim();

            if (input == "0") return;

            if (int.TryParse(input, out int choice) && menuItems.ContainsKey(choice))
            {
                try
                {
                    await menuItems[choice].Action();
                    Console.WriteLine("\nГотово! Нажмите любую клавишу...");
                    Console.ReadKey(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nОшибка: {ex.Message}");
                    Console.ReadKey(true);
                }
            }
            else
            {
                Console.WriteLine("Неверный выбор!");
                await Task.Delay(1200);
            }
        }
    }
}