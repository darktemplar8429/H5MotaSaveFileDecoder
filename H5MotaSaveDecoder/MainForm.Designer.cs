#nullable enable

namespace H5MotaSaveDecoder;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;
    private Label titleLabel = null!;
    private Label subtitleLabel = null!;
    private Button decodeButton = null!;
    private Button encodeButton = null!;
    private Label outputLabel = null!;
    private Label statusLabel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        titleLabel = new Label();
        subtitleLabel = new Label();
        decodeButton = new Button();
        encodeButton = new Button();
        outputLabel = new Label();
        statusLabel = new Label();
        SuspendLayout();
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Microsoft YaHei UI", 19F, FontStyle.Bold);
        titleLabel.Location = new Point(30, 24);
        titleLabel.Text = "H5 魔塔存档解码器";
        subtitleLabel.AutoSize = true;
        subtitleLabel.ForeColor = Color.FromArgb(95, 108, 125);
        subtitleLabel.Location = new Point(33, 72);
        subtitleLabel.Text = "存档转为 JSON · 编辑后重新打包";
        decodeButton.BackColor = Color.FromArgb(20, 118, 108);
        decodeButton.FlatStyle = FlatStyle.Flat;
        decodeButton.FlatAppearance.BorderSize = 0;
        decodeButton.Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold);
        decodeButton.ForeColor = Color.White;
        decodeButton.Location = new Point(34, 118);
        decodeButton.Size = new Size(232, 64);
        decodeButton.Text = "解码存档";
        decodeButton.TabIndex = 0;
        decodeButton.UseVisualStyleBackColor = false;
        decodeButton.Click += DecodeButton_Click;
        encodeButton.BackColor = Color.FromArgb(228, 239, 237);
        encodeButton.FlatStyle = FlatStyle.Flat;
        encodeButton.FlatAppearance.BorderSize = 0;
        encodeButton.Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold);
        encodeButton.ForeColor = Color.FromArgb(15, 88, 82);
        encodeButton.Location = new Point(284, 118);
        encodeButton.Size = new Size(232, 64);
        encodeButton.Text = "打包存档";
        encodeButton.TabIndex = 1;
        encodeButton.UseVisualStyleBackColor = false;
        encodeButton.Click += EncodeButton_Click;
        outputLabel.AutoSize = false;
        outputLabel.Size = new Size(483, 44);
        outputLabel.ForeColor = Color.FromArgb(95, 108, 125);
        outputLabel.Location = new Point(33, 202);
        outputLabel.Text = "输出到所选文件所在文件夹：\r\nH5MTSD_decode_json / H5MTSD_encode";
        statusLabel.AutoEllipsis = true;
        statusLabel.ForeColor = Color.FromArgb(95, 108, 125);
        statusLabel.Location = new Point(33, 250);
        statusLabel.Size = new Size(483, 32);
        statusLabel.Text = "请选择一项操作";
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(249, 251, 250);
        ClientSize = new Size(550, 290);
        Controls.Add(titleLabel);
        Controls.Add(subtitleLabel);
        Controls.Add(decodeButton);
        Controls.Add(encodeButton);
        Controls.Add(outputLabel);
        Controls.Add(statusLabel);
        Font = new Font("Microsoft YaHei UI", 10F);
        ForeColor = Color.FromArgb(29, 43, 55);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "H5 魔塔存档解码器";
        ResumeLayout(false);
        PerformLayout();
    }
}
