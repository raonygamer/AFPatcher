namespace AFPatcher.Utility;

public static class Log
{
    public static void WriteLine(object? value = null, ConsoleColor color = ConsoleColor.Gray)
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(value);
        Console.ForegroundColor = oldColor;
    }
    
    public static void Write(object? value = null, ConsoleColor color = ConsoleColor.Gray)
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(value);
        Console.ForegroundColor = oldColor;
    }
    
    public static void SuccessLine(object? value = null) => WriteLine(value, ConsoleColor.DarkCyan);
    public static void TraceLine(object? value = null) => WriteLine(value, ConsoleColor.Yellow);
    public static void WarnLine(object? value = null) => WriteLine(value, ConsoleColor.DarkYellow);
    public static void ErrorLine(object? value = null) => WriteLine(value, ConsoleColor.Red);
    
    public static void Success(object? value = null) => Write(value, ConsoleColor.DarkCyan);
    public static void Trace(object? value = null) => Write(value, ConsoleColor.Yellow);
    public static void Warn(object? value = null) => Write(value, ConsoleColor.DarkYellow);
    public static void Error(object? value = null) => Write(value, ConsoleColor.Red);
}