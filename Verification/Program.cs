using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using H5MotaSaveDecoder;

internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }
    private static void Reject(Action operation, string message)
    {
        bool rejected = false;
        try { operation(); } catch (Exception e) when (e is InvalidDataException or JsonException) { rejected = true; }
        Check(rejected, message);
    }

    [STAThread]
    private static void Main()
    {
        using var fixtures = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures.json")));
        foreach (var item in fixtures.RootElement.GetProperty("raw").EnumerateArray())
        {
            string plain = item.GetProperty("plain").GetString()!;
            string compressed = item.GetProperty("compressed").GetString()!;
            Check(LzString.CompressToBase64(plain) == compressed, "Compression differs from JS oracle");
            Check(LzString.DecompressFromBase64(compressed) == plain, "Decompression differs from JS oracle");
            Check(LzString.DecompressFromBase64(compressed.TrimEnd('=')) == plain, "Unpadded save changed");
            Check(LzString.DecompressFromBase64("\uFEFF" + compressed + "\r\n") == plain, "Save BOM/whitespace changed");
        }
        foreach (var item in fixtures.RootElement.GetProperty("saves").EnumerateArray())
        {
            string plain = item.GetProperty("plain").GetString()!;
            string compressed = item.GetProperty("compressed").GetString()!;
            string decoded = SaveCodec.Decode(compressed);
            Check(JsonNode.DeepEquals(JsonNode.Parse(decoded), JsonNode.Parse(plain)), "Save changed during decode");
            Check(JsonNode.DeepEquals(JsonNode.Parse(SaveCodec.Decode(SaveCodec.Encode(decoded))), JsonNode.Parse(plain)), "Save changed during round-trip");
            string actualPacked = SaveCodec.Encode(decoded);
            string expectedPacked = item.GetProperty("canonical").GetString()!;
            if (actualPacked != expectedPacked)
            {
                string a = LzString.DecompressFromBase64(actualPacked), b = LzString.DecompressFromBase64(expectedPacked);
                int offset = Enumerable.Range(0, Math.Min(a.Length, b.Length)).FirstOrDefault(i => a[i] != b[i]);
                Console.WriteLine($"Mismatch at {offset}: actual={a.Substring(offset, Math.Min(100, a.Length-offset))}; expected={b.Substring(offset, Math.Min(100, b.Length-offset))}");
            }
            Check(actualPacked == expectedPacked, "Packed save differs from game canonical encoding");
        }
        foreach (string invalid in new[] { "=", "Q====", "Q=Q", "!", "AA", "BYUwNmD" })
            Reject(() => LzString.DecompressFromBase64(invalid), "Damaged LZString accepted: " + invalid);
        string scriptLikeJson = JsonSerializer.Serialize(new
        {
            text = "'); throw new Error('must stay text'); //\n中文😀",
            integer = 9007199254740993L
        });
        Check(JsonNode.DeepEquals(JsonNode.Parse(scriptLikeJson), JsonNode.Parse(SaveCodec.Decode(SaveCodec.Encode(scriptLikeJson)))),
            "JavaScript-looking text must remain data");
        var vectors = fixtures.RootElement.GetProperty("saves").EnumerateArray()
            .Select(item => (Plain: item.GetProperty("plain").GetString()!, Packed: item.GetProperty("canonical").GetString()!)).ToArray();
        var concurrent = Enumerable.Range(0, 12).Select(i => Task.Run(() =>
        {
            var vector = vectors[i % vectors.Length];
            return SaveCodec.Encode(vector.Plain) == vector.Packed &&
                JsonNode.DeepEquals(JsonNode.Parse(SaveCodec.Decode(vector.Packed)), JsonNode.Parse(vector.Plain));
        })).ToArray();
        Check(Task.WhenAll(concurrent).GetAwaiter().GetResult().All(ok => ok), "Concurrent conversions interfered");
        try
        {
            _ = LzString.DecompressFromBase64(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "over-limit.lz.txt")));
            throw new Exception("Oversized decoded content was accepted");
        }
        catch (InvalidDataException error)
        {
            Check(error.Message.Contains("64 MiB"), "Decoded length guard did not report the existing limit");
        }
        string scratch = Path.Combine(AppContext.BaseDirectory, "test-output");
        Directory.CreateDirectory(scratch);
        Check(Path.GetFileName(SaveCodec.OutputPath("test_save2", true)) == "test_save2_encode.json", "Decode name");
        Check(Path.GetFileName(SaveCodec.OutputPath("test_save2_encode.json", false)) == "test_save2", "Encode name");
        Check(Path.GetFileName(SaveCodec.OutputPath("custom.json", false)) == "custom", "Custom name");
        Check(Path.GetFileName(SaveCodec.OutputPath("test.h5save_encode.json", false)) == "test.h5save", "Export file extension");
        Reject(() => SaveCodec.OutputPath("_encode.json", false), "Empty output name");
        Reject(() => SaveCodec.Decode(""), "Empty input");
        Reject(() => SaveCodec.Decode("not!a!save"), "Invalid characters");
        Reject(() => SaveCodec.Decode("BYUwNmD"), "Truncated input");
        Reject(() => SaveCodec.Encode("{broken}"), "Malformed JSON");
        Reject(() => SaveCodec.Encode("42"), "Non-container JSON");
        string source = Path.Combine(scratch, "source.json"), output = Path.Combine(scratch, "packed");
        File.WriteAllText(source, "{\"中文\":\"勇士😀\",\"n\":9007199254740993}", new UTF8Encoding(true));
        SaveCodec.ConvertFile(source, output, false);
        Check(SaveCodec.Decode(File.ReadAllText(output)).Contains("9007199254740993"), "Large integer precision");
        string good = File.ReadAllText(output);
        File.WriteAllText(source, "{broken");
        Reject(() => SaveCodec.ConvertFile(source, output, false), "Malformed file");
        Check(File.ReadAllText(output) == good, "Invalid input overwrote output");
        File.WriteAllText(source, "BYUwNmD");
        Reject(() => SaveCodec.ConvertFile(source, output, true), "Damaged compressed file");
        Check(File.ReadAllText(output) == good, "Damaged compressed input overwrote output");
        File.WriteAllText(source, "{\"hp\":123}");
        SaveCodec.ConvertFile(source, output, false);
        Check(SaveCodec.Decode(File.ReadAllText(output)).Contains("123"), "Overwrite output");

        // Output follows the selected file, including when selecting JSON inside a decode folder.
        string selectedDirectory = Path.Combine(scratch, "选中的文件 目录");
        Directory.CreateDirectory(selectedDirectory);
        string selectedJson = Path.Combine(selectedDirectory, "example_encode.json");
        File.WriteAllText(selectedJson, "{\"hp\":321}");
        string packedPath = SaveCodec.OutputPath(selectedJson, false);
        Check(packedPath == Path.Combine(selectedDirectory, "H5MTSD_encode", "example"), "Pack beside selected JSON");
        SaveCodec.ConvertFile(selectedJson, packedPath, false);
        string keepFile = Path.Combine(Path.GetDirectoryName(packedPath)!, "existing-file.txt");
        File.WriteAllText(keepFile, "keep");
        SaveCodec.ConvertFile(selectedJson, packedPath, false);
        Check(File.ReadAllText(keepFile) == "keep", "Reuse existing output folder without disturbing its files");
        string decodedPath = SaveCodec.OutputPath(packedPath, true);
        Check(decodedPath == Path.Combine(selectedDirectory, "H5MTSD_encode", "H5MTSD_decode_json", "example_encode.json"), "Decode beside selected save");
        SaveCodec.ConvertFile(packedPath, decodedPath, true);
        Check(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(decodedPath)), JsonNode.Parse(File.ReadAllText(selectedJson))), "File conversion at source-relative paths");

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        using var form = new MainForm();
        form.ShowInTaskbar = false;
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-20000, -20000);
        form.Show();
        Application.DoEvents();
        Check(form.Controls.OfType<Button>().Select(b => b.Text).Order().SequenceEqual(new[] { "打包存档", "解码存档" }.Order()), "Form buttons");
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        bitmap.Save(Path.Combine(scratch, "window-preview.png"));
        Console.WriteLine($"PASS: {checks} checks; JSON/LZString compatibility, file handling and WinForms construction.");
    }
}
