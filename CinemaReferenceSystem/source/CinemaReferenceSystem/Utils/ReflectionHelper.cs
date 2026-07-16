using System.Reflection;
using System.Text;

namespace CinemaReferenceSystem.Utils;

public static class ReflectionHelper
{
    public static string ToDetailedString(object obj)
    {
        if (obj == null) return "null";

        var type = obj.GetType();
        var sb = new StringBuilder();
        sb.AppendLine($"{type.Name}:");

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.Name != "PasswordHash");

        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj);
            if (value is DateTime dt) value = dt.ToString("dd.MM.yyyy HH:mm");
            if (value is decimal d) value = d.ToString("0.00");

            sb.AppendLine($"  {prop.Name}: {value}");
        }

        return sb.ToString().TrimEnd();
    }

    public static void PrintList<T>(IEnumerable<T> items, string title)
    {
        Console.WriteLine($"\n=== {title} ===");
        if (!items.Any())
        {
            Console.WriteLine("Список пустой.");
            return;
        }

        int index = 1;
        foreach (var item in items)
        {
            Console.WriteLine($"{index++}.");
            Console.WriteLine(ToDetailedString(item));
            Console.WriteLine("─".PadRight(50, '─'));
        }
    }
}