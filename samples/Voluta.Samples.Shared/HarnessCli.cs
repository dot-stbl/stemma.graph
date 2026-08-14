namespace Voluta.Samples.Shared;

public static class HarnessCli
{
    public static bool HasFlag(string[] args, string flag)
    {
        return args.Any(argument => string.Equals(argument, flag, StringComparison.OrdinalIgnoreCase));
    }

    public static string? GetOption(string[] args, string name)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    public static IChatCompletionClient CreateChatClient(bool offline)
    {
        if (offline)
        {
            CliUi.Node("chat", "offline · ScriptedChatClient");
            return new ScriptedChatClient();
        }

        if (OpenAiCompatibleChatClient.TryCreateFromEnvironment(out var client, out var error) && client is not null)
        {
            CliUi.Node("chat", "OpenAI-compatible · from env");
            return client;
        }

        CliUi.Warn(error ?? "chat client unavailable");
        CliUi.Node("chat", "falling back · ScriptedChatClient");
        return new ScriptedChatClient();
    }

    /// <summary>
    ///     Creates a chat client; caller disposes if the instance is <see cref="IDisposable" />.
    /// </summary>
    public static IChatCompletionClient CreateChatClient(bool offline, out IDisposable? lifetime)
    {
        var client = CreateChatClient(offline);
        lifetime = client as IDisposable;
        return client;
    }

    public static bool Confirm(string prompt)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"  ? {prompt} ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("[y/N] ");
        Console.ResetColor();
        var line = Console.ReadLine();
        return line is not null
               && (line.Equals("y", StringComparison.OrdinalIgnoreCase)
                   || line.Equals("yes", StringComparison.OrdinalIgnoreCase));
    }
}
