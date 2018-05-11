using System;

namespace Core.Foundation.Helpers
{
    public static class ConsoleHelper
    {
        public static void WriteLine(string message, ConsoleColor? color = null)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color ?? originalColor;
            Console.WriteLine($"{DateTime.Now}: {message}");
            Console.ForegroundColor = originalColor;
        }
    }
}
