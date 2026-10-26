using System.Text.Json;

namespace H5MotaSaveDecoder;

public partial class MainForm : Form
{
    private bool busy;
    public MainForm() => InitializeComponent();
    private async void DecodeButton_Click(object? sender, EventArgs e) => await ConvertAsync(true);
    private async void EncodeButton_Click(object? sender, EventArgs e) => await ConvertAsync(false);

    private async Task ConvertAsync(bool decode)
    {
        if (busy) return;
        using var picker = new OpenFileDialog
        {
            Title = decode ? "选择需要解码的存档" : "选择需要打包的 JSON",
            Filter = decode ? "存档文件（所有文件）|*.*" : "JSON 文件 (*.json)|*.json",
            CheckFileExists = true,
            Multiselect = false,
            RestoreDirectory = true
        };
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            string output = SaveCodec.OutputPath(picker.FileName, decode);
            if (File.Exists(output) && MessageBox.Show(this, "以下文件已存在，是否覆盖？\n\n" + output,
                "确认覆盖", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            busy = true;
            decodeButton.Enabled = encodeButton.Enabled = false;
            UseWaitCursor = true;
            statusLabel.Text = decode ? "正在解码存档…" : "正在打包存档…";
            await Task.Run(() => SaveCodec.ConvertFile(picker.FileName, output, decode));
            statusLabel.Text = "已完成：" + Path.GetFileName(output);
            MessageBox.Show(this, "文件已保存至：\n\n" + output, decode ? "解码完成" : "打包完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception error)
        {
            statusLabel.Text = "转换未完成，请检查所选文件。";
            string message = error is JsonException json
                ? $"JSON 格式不正确，请检查内容。\n位置：第 {(json.LineNumber ?? 0) + 1} 行，第 {(json.BytePositionInLine ?? 0) + 1} 字节。"
                : error is UnauthorizedAccessException
                    ? "无法写入输出目录，请将所选文件放在有写入权限的文件夹后重试。"
                    : error.Message;
            MessageBox.Show(this, message, "转换失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            busy = false;
            decodeButton.Enabled = encodeButton.Enabled = true;
            UseWaitCursor = false;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (busy) { e.Cancel = true; statusLabel.Text = "正在处理文件，请等待转换完成后关闭。"; }
        base.OnFormClosing(e);
    }
}
