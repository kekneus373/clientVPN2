using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace App;

internal sealed class MainForm : Form
{
    private const string ActivationMessageName = "CrystalFinanceVpnLauncher.Activate";
    private const uint ShellChangeUpdateItem = 0x00002000;
    private const uint ShellChangePathW = 0x0005;

    private readonly LauncherSettings _settings;
    private readonly string _configPath;
    private readonly Button _primaryButton;
    private readonly Button _secondaryButton;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolTip _toolTip;
    private readonly uint _activationMessage;
    private bool _allowClose;
    private bool _isRunning;

    public MainForm()
    {
        _configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        _settings = LoadSettings(_configPath);
        _activationMessage = RegisterWindowMessage(ActivationMessageName);

        Text = "VPN Launcher";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = true;
        Icon = LoadTrayIcon(_settings.PrimaryButton.IconPath);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        ClientSize = new Size(320, 450);
        MinimumSize = Size;
        MaximumSize = Size;
        Font = new Font("Segoe UI Variable", 9F, FontStyle.Regular, GraphicsUnit.Point);

        var background = new GradientPanel(
            Color.FromArgb(239, 250, 255),
            Color.FromArgb(187, 229, 249))
        {
            Dock = DockStyle.Fill
        };
        Controls.Add(background);

        var refreshButton = new Button
        {
            Text = "↻",
            Font = new Font("Segoe UI Symbol", 18F, FontStyle.Regular, GraphicsUnit.Point),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(28, 104, 137),
            Size = new Size(42, 40),
            Location = new Point(8, 6),
            TabStop = false,
            UseVisualStyleBackColor = false
        };
        refreshButton.FlatAppearance.BorderSize = 0;
        refreshButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(90, 255, 255, 255);
        refreshButton.Click += (_, _) => RefreshIcons();
        background.Controls.Add(refreshButton);
        _toolTip = new ToolTip { ShowAlways = true };
        _toolTip.SetToolTip(refreshButton, "Refresh RDP icons");

        var helpButton = new Button
        {
            Text = "❓",
            Font = new Font("Segoe UI Emoji", 13F, FontStyle.Regular, GraphicsUnit.Point),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(28, 104, 137),
            Size = new Size(42, 40),
            Location = new Point(270, 6),
            TabStop = false,
            UseVisualStyleBackColor = false
        };
        helpButton.FlatAppearance.BorderSize = 0;
        helpButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(90, 255, 255, 255);
        helpButton.Click += (_, _) => _toolTip.Show($"Configuration file:{Environment.NewLine}{_configPath}", helpButton, 0, helpButton.Height, 5000);
        background.Controls.Add(helpButton);
        _toolTip.SetToolTip(helpButton, $"Configuration file: {_configPath}");

        var titleLabel = new Label
        {
            Text = _settings.Title,
            Font = new Font(Font.FontFamily, 14F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(26, 65, 83),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true,
            Location = new Point(14, 48),
            Size = new Size(292, 84)
        };
        background.Controls.Add(titleLabel);

        var mainButtonShadow = new ShadowPanel
        {
            Location = new Point(39, 138),
            Size = new Size(242, 187),
            BackColor = Color.Transparent
        };
        background.Controls.Add(mainButtonShadow);

        _primaryButton = CreateActionButton(_settings.PrimaryButton, new Size(228, 172));
        _primaryButton.Location = new Point(7, 4);
        _primaryButton.Click += async (_, _) => await RunBatchAsync(_settings.PrimaryButton, "primary");
        mainButtonShadow.Controls.Add(_primaryButton);

        _secondaryButton = new Button
        {
            Text = _settings.SecondaryButton.Label,
            Font = new Font(Font.FontFamily, 10F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(31, 87, 109),
            BackColor = Color.FromArgb(224, 245, 252),
            FlatStyle = FlatStyle.Flat,
            Size = new Size(238, 54),
            Location = new Point(41, 344),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        _secondaryButton.FlatAppearance.BorderColor = Color.FromArgb(128, 190, 211);
        _secondaryButton.FlatAppearance.BorderSize = 1;
        _secondaryButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(204, 237, 249);
        _secondaryButton.Click += async (_, _) => await RunBatchAsync(_settings.SecondaryButton, "secondary");
        background.Controls.Add(_secondaryButton);

        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Exit", null, (_, _) => ExitApplication());
        _notifyIcon = new NotifyIcon
        {
            Text = "VPN Launcher",
            Icon = (Icon)Icon.Clone(),
            ContextMenuStrip = trayMenu,
            Visible = true
        };
        _notifyIcon.MouseClick += (_, eventArgs) =>
        {
            if (eventArgs.Button == MouseButtons.Left)
            {
                RestoreFromTray();
            }
        };

        FormClosing += HandleFormClosing;
        Load += (_, _) => RefreshIcons();
    }

    protected override void WndProc(ref Message message)
    {
        if (_activationMessage != 0 && message.Msg == _activationMessage)
        {
            RestoreFromTray();
        }

        base.WndProc(ref message);
    }

    private static LauncherSettings LoadSettings(string configPath)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"The required configuration file was not found:{Environment.NewLine}{configPath}", configPath);
        }

        LauncherSettings settings;
        try
        {
            settings = JsonSerializer.Deserialize<LauncherSettings>(
                File.ReadAllText(configPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidDataException("The configuration file is empty or invalid.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Could not parse the configuration file:{Environment.NewLine}{configPath}{Environment.NewLine}{exception.Message}", exception);
        }

        string configDirectory = Path.GetDirectoryName(configPath)!;
        RequireText(settings.Title, "title");
        ValidateButton(settings.PrimaryButton, "primaryButton");
        ValidateButton(settings.SecondaryButton, "secondaryButton");
        if (settings.RdpFiles is null)
        {
            throw new InvalidDataException("The 'rdpFiles' setting must be an array.");
        }

        foreach (RdpFileSettings rdpFile in settings.RdpFiles)
        {
            if (rdpFile is null)
            {
                throw new InvalidDataException("The 'rdpFiles' array cannot contain null entries.");
            }

            RequireText(rdpFile.Path, "rdpFiles[].path");
            RequireText(rdpFile.HideWhen, "rdpFiles[].hideWhen");
            if (!string.Equals(rdpFile.HideWhen, "primary", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(rdpFile.HideWhen, "secondary", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"The 'hideWhen' value for '{rdpFile.Path}' must be 'primary' or 'secondary'.");
            }

            rdpFile.ResolvedPath = ResolvePath(configDirectory, rdpFile.Path);
        }

        settings.PrimaryButton.ResolvedBatchPath = ResolvePath(configDirectory, settings.PrimaryButton.BatchPath);
        settings.SecondaryButton.ResolvedBatchPath = ResolvePath(configDirectory, settings.SecondaryButton.BatchPath);
        settings.PrimaryButton.ResolvedIconPath = ResolveOptionalPath(configDirectory, settings.PrimaryButton.IconPath);
        settings.SecondaryButton.ResolvedIconPath = ResolveOptionalPath(configDirectory, settings.SecondaryButton.IconPath);

        EnsureFileExists(settings.PrimaryButton.ResolvedBatchPath, "primary batch file");
        EnsureFileExists(settings.SecondaryButton.ResolvedBatchPath, "secondary batch file");
        if (settings.PrimaryButton.ResolvedIconPath is not null)
        {
            EnsureFileExists(settings.PrimaryButton.ResolvedIconPath, "primary button icon");
        }

        if (settings.SecondaryButton.ResolvedIconPath is not null)
        {
            EnsureFileExists(settings.SecondaryButton.ResolvedIconPath, "secondary button icon");
        }

        return settings;
    }

    private static void ValidateButton(ButtonSettings? button, string settingName)
    {
        if (button is null)
        {
            throw new InvalidDataException($"The required '{settingName}' section is missing.");
        }

        RequireText(button.Label, $"{settingName}.label");
        RequireText(button.BatchPath, $"{settingName}.batchPath");
    }

    private static void RequireText(string? value, string settingName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"The required '{settingName}' value is missing from appsettings.json.");
        }
    }

    private static string ResolvePath(string baseDirectory, string path) =>
        Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(baseDirectory, path));

    private static string? ResolveOptionalPath(string baseDirectory, string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : ResolvePath(baseDirectory, path);

    private static void EnsureFileExists(string path, string description)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"The configured {description} was not found:{Environment.NewLine}{path}", path);
        }
    }

    private static Icon LoadTrayIcon(string? iconPath)
    {
        string? resolvedPath = ResolveOptionalPath(AppContext.BaseDirectory, iconPath);
        if (resolvedPath is not null && File.Exists(resolvedPath))
        {
            using var fileIcon = new Icon(resolvedPath);
            return (Icon)fileIcon.Clone();
        }

        return (Icon)SystemIcons.Application.Clone();
    }

    private static Button CreateActionButton(ButtonSettings settings, Size size)
    {
        var button = new RoundedActionButton
        {
            Text = settings.Label,
            Size = size,
            Font = new Font("Segoe UI Variable", 11F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(3, 132, 177),
            Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat,
            TextImageRelation = TextImageRelation.ImageAboveText,
            ImageAlign = ContentAlignment.MiddleCenter,
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = false,
            Padding = new Padding(14, 18, 14, 15)
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(3, 116, 157);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(2, 101, 138);

        if (settings.ResolvedIconPath is not null)
        {
            using var fileIcon = new Icon(settings.ResolvedIconPath);
            button.Image = fileIcon.ToBitmap();
        }

        return button;
    }

    private async Task RunBatchAsync(ButtonSettings settings, string profile)
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        _primaryButton.Enabled = false;
        _secondaryButton.Enabled = false;
        UseWaitCursor = true;

        try
        {
            string batchPath = settings.ResolvedBatchPath!;
            var startInfo = new ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe",
                Arguments = $"/d /c \"\"{batchPath}\"\"",
                WorkingDirectory = Path.GetDirectoryName(batchPath)!,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Could not start the batch file:{Environment.NewLine}{batchPath}");
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"The batch file failed with exit code {process.ExitCode}:{Environment.NewLine}{batchPath}");
            }

            // Update the selected profile only after its launcher script has completed successfully.
            SetIcons(profile);
            HideToTray();
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "VPN Launcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _isRunning = false;
            _primaryButton.Enabled = true;
            _secondaryButton.Enabled = true;
            UseWaitCursor = false;
        }
    }

    private void RefreshIcons()
    {
        var errors = new List<string>();
        // Refresh always reveals configured RDP files so the user can recover from a stale VPN selection.
        foreach (RdpFileSettings rdpFile in _settings.RdpFiles)
        {
            try
            {
                if (!File.Exists(rdpFile.ResolvedPath))
                {
                    errors.Add($"RDP file not found:{Environment.NewLine}{rdpFile.ResolvedPath}");
                    continue;
                }

                FileAttributes attributes = File.GetAttributes(rdpFile.ResolvedPath);
                File.SetAttributes(rdpFile.ResolvedPath, attributes & ~FileAttributes.Hidden);
                NotifyExplorer(rdpFile.ResolvedPath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            {
                errors.Add($"{rdpFile.ResolvedPath}{Environment.NewLine}{exception.Message}");
            }
        }

        ShowRdpErrors(errors);
    }

    private void SetIcons(string connectedProfile)
    {
        if (!_settings.HideRdp)
        {
            return;
        }

        var errors = new List<string>();
        foreach (RdpFileSettings rdpFile in _settings.RdpFiles)
        {
            try
            {
                if (!File.Exists(rdpFile.ResolvedPath))
                {
                    errors.Add($"RDP file not found:{Environment.NewLine}{rdpFile.ResolvedPath}");
                    continue;
                }

                FileAttributes attributes = File.GetAttributes(rdpFile.ResolvedPath);
                // A profile hides only files whose hideWhen value names that selected profile.
                bool shouldBeHidden = string.Equals(rdpFile.HideWhen, connectedProfile, StringComparison.OrdinalIgnoreCase);
                FileAttributes updatedAttributes = shouldBeHidden
                    ? attributes | FileAttributes.Hidden
                    : attributes & ~FileAttributes.Hidden;

                if (updatedAttributes != attributes)
                {
                    File.SetAttributes(rdpFile.ResolvedPath, updatedAttributes);
                    NotifyExplorer(rdpFile.ResolvedPath);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            {
                errors.Add($"{rdpFile.ResolvedPath}{Environment.NewLine}{exception.Message}");
            }
        }

        ShowRdpErrors(errors);
    }

    private void ShowRdpErrors(List<string> errors)
    {
        if (errors.Count > 0)
        {
            MessageBox.Show(
                this,
                string.Join($"{Environment.NewLine}{Environment.NewLine}", errors),
                "Could not update RDP icons",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private static void NotifyExplorer(string path)
    {
        // Notify Explorer after changing attributes so desktop icons update without a restart.
        SHChangeNotify(ShellChangeUpdateItem, ShellChangePathW, path, IntPtr.Zero);
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
        _notifyIcon.Visible = true;
    }

    private void RestoreFromTray()
    {
        if (!Visible)
        {
            Show();
            ShowInTaskbar = true;
        }

        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    private void HandleFormClosing(object? sender, FormClosingEventArgs eventArgs)
    {
        if (_allowClose)
        {
            return;
        }

        // Closing the window is a tray action; only the tray menu is allowed to end the process.
        eventArgs.Cancel = true;
        HideToTray();
    }

    private void ExitApplication()
    {
        _allowClose = true;
        _notifyIcon.Visible = false;
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.Dispose();
            _toolTip.Dispose();
            if (Icon is not null)
            {
                Icon.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern void SHChangeNotify(uint eventId, uint flags, string path, IntPtr item2);

    private sealed class LauncherSettings
    {
        public LauncherSettings()
        {
        }

        public string Title { get; set; } = string.Empty;
        public ButtonSettings PrimaryButton { get; set; } = null!;
        public ButtonSettings SecondaryButton { get; set; } = null!;
        public bool HideRdp { get; set; }
        public List<RdpFileSettings> RdpFiles { get; set; } = [];
    }

    private sealed class ButtonSettings
    {
        public ButtonSettings()
        {
        }

        public string Label { get; set; } = string.Empty;
        public string BatchPath { get; set; } = string.Empty;
        public string? IconPath { get; set; }
        public string? ResolvedBatchPath { get; set; }
        public string? ResolvedIconPath { get; set; }
    }

    private sealed class RdpFileSettings
    {
        public RdpFileSettings()
        {
        }

        public string Path { get; set; } = string.Empty;
        public string HideWhen { get; set; } = string.Empty;
        public string ResolvedPath { get; set; } = string.Empty;
    }

    private sealed class GradientPanel : Panel
    {
        private readonly Color _topColor;
        private readonly Color _bottomColor;

        public GradientPanel(Color topColor, Color bottomColor)
        {
            _topColor = topColor;
            _bottomColor = bottomColor;
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaintBackground(PaintEventArgs eventArgs)
        {
            using var brush = new LinearGradientBrush(ClientRectangle, _topColor, _bottomColor, LinearGradientMode.Vertical);
            eventArgs.Graphics.FillRectangle(brush, ClientRectangle);
        }
    }

    private sealed class ShadowPanel : Panel
    {
        public ShadowPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            for (int layer = 5; layer >= 1; layer--)
            {
                Rectangle bounds = new(7, 5 + layer, Width - 15, Height - 13);
                using GraphicsPath path = CreateRoundedPath(bounds, 23);
                using var pen = new Pen(Color.FromArgb(9, 23, 65, 83), layer + 1);
                eventArgs.Graphics.DrawPath(pen, path);
            }

            base.OnPaint(eventArgs);
        }

        private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    private sealed class RoundedActionButton : Button
    {
        public RoundedActionButton()
        {
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        protected override void OnResize(EventArgs eventArgs)
        {
            base.OnResize(eventArgs);
            var bounds = new Rectangle(0, 0, Width, Height);
            var path = new GraphicsPath();
            int diameter = 36;
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            Region = new Region(path);
            path.Dispose();
        }
    }
}
