using System.Text.Json;
using FeatBit.Cli.Serialization;

namespace FeatBit.Cli.Configuration;

public static class UserConfigStore
{
    private const string ConfigFileName = "config.json";
    private static readonly FeatBitJsonContext ReadContext = FeatBitJsonContext.Default;
    private static readonly FeatBitJsonContext WriteContext = new(new JsonSerializerOptions
    {
        WriteIndented = true
    });

    public static UserConfig Load()
    {
        var path = GetConfigPath();
        if (!File.Exists(path))
        {
            return new UserConfig();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize(json, ReadContext.UserConfig) ?? new UserConfig();
        }
        catch
        {
            return new UserConfig();
        }
    }

    public static void Save(UserConfig config)
    {
        var path = GetConfigPath();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(config, WriteContext.UserConfig);
        File.WriteAllText(path, json);
    }

    public static bool Clear()
    {
        var path = GetConfigPath();
        if (!File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        return true;
    }

    public static string GetConfigPath()
    {
        var overridePath = Environment.GetEnvironmentVariable("FEATBIT_USER_CONFIG_FILE");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return overridePath;
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(appData))
        {
            appData = Environment.CurrentDirectory;
        }

        return Path.Combine(appData, "featbit", ConfigFileName);
    }
}
