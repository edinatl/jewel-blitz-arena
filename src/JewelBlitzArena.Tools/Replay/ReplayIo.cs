using System.Text.Json;
using System.Text.Json.Serialization;

namespace JewelBlitzArena.Tools.Replay;

public static class ReplayIo
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static ReplayRecording Load(string path)
    {
        string json = File.ReadAllText(path);
        ReplayRecording? recording = JsonSerializer.Deserialize<ReplayRecording>(json, Options);
        if (recording is null)
        {
            throw new InvalidDataException($"Failed to deserialize replay: {path}");
        }

        if (recording.FormatVersion < ReplayRecording.MinSupportedFormatVersion
            || recording.FormatVersion > ReplayRecording.CurrentFormatVersion)
        {
            throw new InvalidDataException(
                $"Unsupported replay formatVersion {recording.FormatVersion} in {path} " +
                $"(supported {ReplayRecording.MinSupportedFormatVersion}..{ReplayRecording.CurrentFormatVersion}).");
        }

        recording.CpuHandoffs ??= Array.Empty<ReplayRecording.CpuHandoff>();
        recording.ItemSpawns ??= Array.Empty<ReplayRecording.ItemSpawn>();
        return recording;
    }

    public static void Save(string path, ReplayRecording recording)
    {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonSerializer.Serialize(recording, Options);
        File.WriteAllText(path, json);
    }
}
