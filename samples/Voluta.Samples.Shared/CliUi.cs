using Voluta.Abstractions.Streaming;

namespace Voluta.Samples.Shared;

/// <summary>
///     Terminal styling for CLI samples — dim meta, role tags, panels
///     (Claude Code / modern agent-CLI look). Falls back cleanly when
///     stdout is redirected or colors are unsupported.
/// </summary>
public static class CliUi
{
    private static readonly bool Color =
        !Console.IsOutputRedirected
        && (Environment.GetEnvironmentVariable("NO_COLOR") is null
            || Environment.GetEnvironmentVariable("NO_COLOR") is "");

    public static void Banner(string sample, string subtitle, params (string Key, string Value)[] meta)
    {
        Blank();
        Accent("  ◆  ");
        Bold($"voluta · {sample}");
        Blank();
        Dim($"  {subtitle}");
        if (meta.Length > 0)
        {
            Blank();
            foreach (var (key, value) in meta)
            {
                Dim("  ");
                Muted($"{key,-9}");
                Dim(value);
                Blank();
            }
        }

        Rule();
    }

    public static void Section(string title)
    {
        Blank();
        Accent("  ▸ ");
        Bold(title);
        Blank();
    }

    public static void Rule()
    {
        Dim("  ────────────────────────────────────────");
        Blank();
    }

    public static void Blank()
    {
        Console.WriteLine();
    }

    public static void Dim(string text)
    {
        Write(text, ConsoleColor.DarkGray);
    }

    public static void Muted(string text)
    {
        Write(text, ConsoleColor.DarkCyan);
    }

    public static void Info(string text)
    {
        WriteLine("  " + text, ConsoleColor.Gray);
    }

    public static void Ok(string text)
    {
        Write("  ✓ ", ConsoleColor.Green);
        WriteLine(text, ConsoleColor.Gray);
    }

    public static void Warn(string text)
    {
        Write("  ! ", ConsoleColor.Yellow);
        WriteLine(text, ConsoleColor.Yellow);
    }

    public static void Error(string text)
    {
        Write("  ✗ ", ConsoleColor.Red);
        WriteLine(text, ConsoleColor.Red);
    }

    public static void Node(string name, string message)
    {
        Write("  ", ConsoleColor.Gray);
        Write(Tag(name), RoleColor(name));
        Write(" ", ConsoleColor.Gray);
        WriteLine(message, ConsoleColor.Gray);
    }

    public static void KeyValue(string key, string? value)
    {
        Write("  ", ConsoleColor.Gray);
        Muted($"{key,-12}");
        WriteLine(value ?? "—", ConsoleColor.White);
    }

    public static void Bullet(string text)
    {
        Dim("    · ");
        WriteLine(text, ConsoleColor.Gray);
    }

    public static void Panel(string title, string body)
    {
        Blank();
        Accent("  ┌─ ");
        Bold(title);
        Blank();
        foreach (var line in body.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            Dim("  │ ");
            WriteLine(line, ConsoleColor.White);
        }

        Dim("  └────────────────────────────────────");
        Blank();
    }

    public static void StreamEvent(StreamEvent item)
    {
        var nodes = item.NodeNames.Count == 0 ? "—" : string.Join(",", item.NodeNames);
        Dim($"  · step {item.Step}  {item.Kind}  [{nodes}]");
        Blank();
        foreach (var write in item.Writes)
        {
            Dim($"      {write.ChannelName} ← ");
            WriteLine(FormatValue(write.Value), ConsoleColor.DarkGray);
        }

        if (item.Payload is not null)
        {
            Dim("      payload ← ");
            WriteLine(FormatValue(item.Payload), ConsoleColor.DarkYellow);
        }
    }

    public static string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",
            string text => text,
            System.Collections.IEnumerable enumerable and not string =>
                "[" + string.Join(", ", enumerable.Cast<object?>().Select(static item => item?.ToString() ?? "null")) + "]",
            _ => value.ToString() ?? "null",
        };
    }

    public static void Messages(object? messages)
    {
        if (messages is string or null || messages is not System.Collections.IEnumerable list)
        {
            return;
        }

        Section("messages");
        foreach (var message in list)
        {
            Bullet(message?.ToString() ?? "");
        }
    }

    private static string Tag(string name)
    {
        return $"[{name}]";
    }

    private static ConsoleColor RoleColor(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "agent" or "plan" or "answer" or "synthesize" or "brief" or "creative" => ConsoleColor.Magenta,
            "tools" or "search" or "retrieve" or "place" or "setup" or "mcp" or "chat" => ConsoleColor.Cyan,
            "gate" or "review" or "risk_gate" => ConsoleColor.Yellow,
            "notify" or "intake" => ConsoleColor.Blue,
            _ => ConsoleColor.White,
        };
    }

    private static void Accent(string text)
    {
        Write(text, ConsoleColor.DarkYellow);
    }

    private static void Bold(string text)
    {
        WriteLine(text, ConsoleColor.White);
    }

    private static void Write(string text, ConsoleColor color)
    {
        if (!Color)
        {
            Console.Write(text);
            return;
        }

        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = previous;
    }

    private static void WriteLine(string text, ConsoleColor color)
    {
        Write(text, color);
        Console.WriteLine();
    }
}
