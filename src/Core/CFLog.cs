namespace CombatFramework.Core;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
}

public static class CFLog
{
    public static Action<LogLevel, string> Output { get; set; } = (_, _) => { };

    public static void Debug(string msg) => Output(LogLevel.Debug, msg);
    public static void Info(string msg) => Output(LogLevel.Info, msg);
    public static void Warning(string msg) => Output(LogLevel.Warning, msg);
    public static void Error(string msg) => Output(LogLevel.Error, msg);
}
