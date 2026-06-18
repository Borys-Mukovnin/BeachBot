using System.Text.Json;

namespace BeachBot.Application.Storage;

/// <summary>Where BeachBot keeps its local data (saved partners, scheduled registrations, "remember me").</summary>
public static class AppPaths
{
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BeachBot");
}

/// <summary>Tiny helper for reading/writing a JSON file, tolerating a missing or corrupt file.</summary>
public static class JsonFileStore
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        IgnoreReadOnlyProperties = true, // skips computed props (e.g. FullName, IsPending)
        PropertyNameCaseInsensitive = true,
    };

    public static T Load<T>(string path, Func<T> createDefault)
    {
        try
        {
            if (!File.Exists(path))
                return createDefault();
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, Options) ?? createDefault();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return createDefault();
        }
    }

    public static void Save<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(value, Options);
        File.WriteAllText(path, json);
    }
}
