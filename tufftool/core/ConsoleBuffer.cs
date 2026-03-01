using System.Collections.Concurrent;

namespace TuffTool.Core;

public static class ConsoleBuffer
{
    private static readonly ConcurrentQueue<string> _messages = new();
    private static readonly object _lock = new object();
    private const int MaxLines = 500;

    public static void WriteLine(string message)
    {
        lock (_lock)
        {
            _messages.Enqueue(message);
            while (_messages.Count > MaxLines)
            {
                _messages.TryDequeue(out _);
            }
        }
    }

    public static string[] GetMessages()
    {
        lock (_lock)
        {
            return _messages.ToArray();
        }
    }

    public static void Clear()
    {
        lock (_lock)
        {
            while (_messages.TryDequeue(out _)) { }
        }
    }
}
