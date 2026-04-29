using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoCutoutStudio;

internal sealed class MainForm : Form
{
    private readonly CheckerPictureBox _preview = new();
    private readonly Button _openButton = new();
    private readonly Button _saveButton = new();
    private readonly Button _processButton = new();
    private readonly TrackBar _tolerance = new();
    private readonly TrackBar _feather = new();
    private readonly TrackBar _cleanup = new();
    private readonly CheckBox _shadow = new();
    private readonly Label _status = new();
    private Bitmap? _source;
    private Bitmap? _result;
    private string? _currentPath;
    private bool _isProcessing;

    public MainForm()
    {
        Text = "Auto Cutout Studio";
        MinimumSize = new Size(980, 640);
        BackColor = Color.FromArgb(246, 247, 249);
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        AllowDrop = true;

        BuildLayout();
        WireEvents();
        UpdateStatus("拖入图片，或点击打开图片开始抠图。");
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(18),
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 290));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 4, 16, 0),
        };

        var title = new Label
        {
            Text = "自动抠图工作台",
            Font = new Font(Font.FontFamily, 18F, FontStyle.Bold),
            Width = 250,
            Height = 42,
        };

        var subtitle = new Label
        {
            Text = "本地处理图片，导出透明 PNG。",
            ForeColor = Color.FromArgb(84, 93, 105),
            Width = 250,
            Height = 32,
        };

        ConfigureButton(_openButton, "打开图片");
        ConfigureButton(_processButton, "应用参数并重新抠图");
        ConfigureButton(_saveButton, "保存 PNG");
        _saveButton.Enabled = false;

        ConfigureSlider(_tolerance, 8, 90, 34);
        ConfigureSlider(_feather, 0, 10, 3);
        ConfigureSlider(_cleanup, 0, 5, 1);

        _shadow.Text = "保留柔和阴影";
        _shadow.Checked = true;
        _shadow.Width = 250;
        _shadow.Height = 34;

        _status.Width = 250;
        _status.Height = 110;
        _status.ForeColor = Color.FromArgb(57, 66, 80);

        panel.Controls.Add(title);
        panel.Controls.Add(subtitle);
        panel.Controls.Add(Spacer(12));
        panel.Controls.Add(_openButton);
        panel.Controls.Add(_processButton);
        panel.Controls.Add(_saveButton);
        panel.Controls.Add(Spacer(20));
        panel.Controls.Add(SliderGroup("背景容差", _tolerance));
        panel.Controls.Add(SliderGroup("边缘羽化", _feather));
        panel.Controls.Add(SliderGroup("边缘清理", _cleanup));
        panel.Controls.Add(_shadow);
        panel.Controls.Add(Spacer(12));
        panel.Controls.Add(_status);

        _preview.Dock = DockStyle.Fill;
        _preview.BackColor = Color.White;
        _preview.SizeMode = PictureBoxSizeMode.Zoom;

        root.Controls.Add(panel, 0, 0);
        root.Controls.Add(_preview, 1, 0);
        Controls.Add(root);
    }

    private void WireEvents()
    {
        _openButton.Click += (_, _) => OpenImage();
        _saveButton.Click += (_, _) => SaveImage();
        _processButton.Click += async (_, _) => await ProcessImageAsync();
        _tolerance.ValueChanged += (_, _) => MarkOptionsChanged();
        _feather.ValueChanged += (_, _) => MarkOptionsChanged();
        _cleanup.ValueChanged += (_, _) => MarkOptionsChanged();
        _shadow.CheckedChanged += (_, _) => MarkOptionsChanged();

        DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
            }
        };

        DragDrop += async (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                LoadImage(files[0]);
                await ProcessImageAsync();
            }
        };
    }

    private static void ConfigureButton(Button button, string text)
    {
        button.Text = text;
        button.Width = 250;
        button.Height = 42;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Color.FromArgb(31, 111, 235);
        button.ForeColor = Color.White;
        button.Cursor = Cursors.Hand;
        button.Margin = new Padding(0, 0, 0, 10);
    }

    private static void ConfigureSlider(TrackBar slider, int min, int max, int value)
    {
        slider.Minimum = min;
        slider.Maximum = max;
        slider.Value = value;
        slider.TickFrequency = Math.Max(1, (max - min) / 5);
        slider.Width = 250;
    }

    private Control SliderGroup(string label, TrackBar slider)
    {
        var box = new Panel { Width = 250, Height = 74, Margin = new Padding(0, 0, 0, 10) };
        var caption = new Label { Text = label, Width = 250, Height = 24, ForeColor = Color.FromArgb(39, 48, 62) };
        slider.Top = 28;
        box.Controls.Add(caption);
        box.Controls.Add(slider);
        return box;
    }

    private static Control Spacer(int height) => new Panel { Width = 250, Height = height };

    private void OpenImage()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.webp|所有文件|*.*",
            Title = "选择要抠图的图片",
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            LoadImage(dialog.FileName);
            _ = ProcessImageAsync();
        }
    }

    private void LoadImage(string path)
    {
        _source?.Dispose();
        _result?.Dispose();
        _result = null;
        _currentPath = path;

        using var stream = File.OpenRead(path);
        using var loaded = new Bitmap(stream);
        _source = new Bitmap(loaded);
        _preview.Image = _source;
        _saveButton.Enabled = false;
        UpdateStatus($"已载入：{Path.GetFileName(path)}");
    }

    private async Task ProcessImageAsync()
    {
        if (_source is null || _isProcessing)
        {
            return;
        }

        _isProcessing = true;
        SetBusy(true);
        var options = new CutoutOptions(_tolerance.Value, _feather.Value, _cleanup.Value, _shadow.Checked);
        try
        {
            var sourceCopy = new Bitmap(_source);
            var result = await Task.Run(() =>
            {
                using (sourceCopy)
                {
                    return CutoutProcessor.RemoveBackground(sourceCopy, options);
                }
            });

            _result?.Dispose();
            _result = result;
            _preview.Image = _result;
            _saveButton.Enabled = true;
            UpdateStatus($"抠图完成：容差 {options.Tolerance}，羽化 {options.Feather}，清理 {options.EdgeCleanup}。调整参数后点击按钮重新应用。");
        }
        catch (Exception ex)
        {
            UpdateStatus($"处理失败：{ex.Message}");
        }
        finally
        {
            _isProcessing = false;
            SetBusy(false);
        }
    }

    private void SaveImage()
    {
        if (_result is null)
        {
            return;
        }

        string defaultName = _currentPath is null
            ? "cutout.png"
            : $"{Path.GetFileNameWithoutExtension(_currentPath)}-cutout.png";

        using var dialog = new SaveFileDialog
        {
            Filter = "透明 PNG|*.png",
            FileName = defaultName,
            Title = "保存透明 PNG",
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _result.Save(dialog.FileName, ImageFormat.Png);
            UpdateStatus($"已保存：{dialog.FileName}");
        }
    }

    private void SetBusy(bool busy)
    {
        _openButton.Enabled = !busy;
        _processButton.Enabled = !busy && _source is not null;
        _tolerance.Enabled = !busy;
        _feather.Enabled = !busy;
        _cleanup.Enabled = !busy;
        _shadow.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void MarkOptionsChanged()
    {
        if (_source is null || _isProcessing)
        {
            return;
        }

        UpdateStatus($"参数已调整：容差 {_tolerance.Value}，羽化 {_feather.Value}，清理 {_cleanup.Value}。点击“应用参数并重新抠图”生效。");
    }

    private void UpdateStatus(string text) => _status.Text = text;

    private sealed class CheckerPictureBox : PictureBox
    {
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            const int size = 18;
            using var light = new SolidBrush(Color.FromArgb(244, 246, 248));
            using var dark = new SolidBrush(Color.FromArgb(224, 228, 234));

            for (int y = 0; y < Height; y += size)
            {
                for (int x = 0; x < Width; x += size)
                {
                    e.Graphics.FillRectangle(((x / size) + (y / size)) % 2 == 0 ? light : dark, x, y, size, size);
                }
            }

            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
        }
    }
}
