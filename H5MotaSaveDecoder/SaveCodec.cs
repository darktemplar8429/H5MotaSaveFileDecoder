using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace H5MotaSaveDecoder;

public static class SaveCodec
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, MaxDepth = 256 };
    private static readonly JsonSerializerOptions Compact = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, MaxDepth = 256 };

    public static string Decode(string text)
    {
        using var document = Parse(LzString.DecompressFromBase64(text));
        return JsonSerializer.Serialize(document.RootElement, Pretty);
    }

    public static string Encode(string json)
    {
        using var document = Parse(json.TrimStart('\uFEFF'));
        string compact = JsonSerializer.Serialize(document.RootElement, Compact);
        var escaped = new StringBuilder(compact.Length);
        for (int i = 0; i < compact.Length; i++)
        {
            char ch = compact[i];
            // System.Text.Json escapes surrogate pairs with uppercase hex; match the game.
            // Consume escaped backslashes as pairs so literal "\\uABCD" text stays untouched.
            if (ch == '\\' && i + 1 < compact.Length)
            {
                escaped.Append(ch).Append(compact[++i]);
                if (compact[i] == 'u' && i + 4 < compact.Length)
                {
                    escaped.Append(compact.Substring(i + 1, 4).ToLowerInvariant());
                    i += 4;
                }
                continue;
            }
            // The game's localSave.js escapes U+007F..U+FFFF before compression.
            if (ch >= 0x7f) escaped.Append("\\u").Append(((int)ch).ToString("x4"));
            else escaped.Append(ch);
        }
        return LzString.CompressToBase64(escaped.ToString());
    }

    private static JsonDocument Parse(string text)
    {
        var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 256 });
        if (document.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
        {
            document.Dispose();
            throw new InvalidDataException("存档 JSON 的顶层必须是对象或数组。");
        }
        return document;
    }

    public static string OutputPath(string source, bool decode)
    {
        string name = Path.GetFileName(source);
        if (decode) name += "_encode.json";
        else if (name.EndsWith("_encode.json", StringComparison.OrdinalIgnoreCase)) name = name[..^12];
        else name = Path.GetFileNameWithoutExtension(name);
        if (string.IsNullOrWhiteSpace(name) || name is "." or "..")
            throw new InvalidDataException("无法生成输出文件名，请先重命名所选文件。");
        string sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(source))!;
        return Path.Combine(sourceDirectory, decode ? "H5MTSD_decode_json" : "H5MTSD_encode", name);
    }

    public static void ConvertFile(string source, string output, bool decode)
    {
        string text = File.ReadAllText(source, Utf8);
        string converted = decode ? Decode(text) : Encode(text);
        // Validate the complete result before creating or replacing the output.
        if (!decode) _ = Decode(converted);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        string temporary = output + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, converted, Utf8);
            File.Move(temporary, output, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
