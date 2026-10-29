using Jint;
using Jint.Runtime;

namespace H5MotaSaveDecoder;

/// <summary>Runs the embedded JavaScript LZString library through Jint.</summary>
public static class LzString
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    private static readonly Lazy<string> Script = new(() =>
    {
        using var stream = typeof(LzString).Assembly.GetManifestResourceStream("H5MotaSaveDecoder.JavaScript.lz-string.js")
            ?? throw new InvalidOperationException("程序缺少内置的 LZString JavaScript 库，请重新安装。");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });

    public static string CompressToBase64(string input) => Invoke("compressToBase64", input);

    public static string DecompressFromBase64(string input)
    {
        input = input.Trim().TrimStart('\uFEFF');
        if (input.Length == 0) throw new InvalidDataException("存档文件为空。");
        int length = input.TrimEnd('=').Length;
        if (length == 0 || input.Length - length > 3) throw InvalidFormat();
        for (int i = 0; i < length; i++)
            if (Alphabet.IndexOf(input[i]) < 0) throw InvalidFormat();
        // Padding is not part of the bit stream. This also accepts unpadded game saves.
        return Invoke("decompressFromBase64", input[..length]);
    }

    private static string Invoke(string method, string input)
    {
        try
        {
            // Engines are not shared: concurrent conversions cannot corrupt JS state.
            // Only the embedded library is executed; file contents are string arguments.
            var engine = new Engine(options => options.TimeoutInterval(TimeSpan.FromMinutes(2)));
            engine.Execute(Script.Value);
            var result = engine.Invoke(engine.GetValue("LZString").AsObject().Get(method), input);
            if (!result.IsString()) throw InvalidFormat();
            return result.AsString();
        }
        catch (JavaScriptException error)
        {
            if (error.Message.Contains("H5MTSD_DECODE_LIMIT", StringComparison.Ordinal))
                throw new InvalidDataException("解码内容超过 64 MiB 字符限制。", error);
            throw new InvalidDataException("存档不是有效的 LZString Base64 数据，或文件已损坏。", error);
        }
        catch (TimeoutException error)
        {
            throw new InvalidDataException("存档转换超过 2 分钟，请检查文件是否过大或已损坏。", error);
        }
    }

    private static InvalidDataException InvalidFormat() => new("存档不是有效的 LZString Base64 数据，或文件已损坏。");
}
