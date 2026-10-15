using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JewelBlitzArena.Data
{
    public static class AuthoredJson
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        public static string Serialize(AuthoredContentPack pack) =>
            JsonSerializer.Serialize(pack, Options);

        public static AuthoredContentPack Deserialize(string json) =>
            JsonSerializer.Deserialize<AuthoredContentPack>(json, Options)
            ?? new AuthoredContentPack();

        public static void WriteToFile(AuthoredContentPack pack, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(path, Serialize(pack));
        }

        public static AuthoredContentPack ReadFromFile(string path) =>
            Deserialize(File.ReadAllText(path));
    }
}
