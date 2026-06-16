using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows.Forms;

namespace BatteryMonitor;

public partial class Form1 : Form
{
    private bool notified = false;
    private readonly NotifyIcon trayIcon;
    private readonly ContextMenuStrip trayMenu;
    private readonly System.Windows.Forms.Timer timer;
    private readonly ToolStripMenuItem powerPlanLabel;

    public Form1()
    {
        InitializeComponent();

        // Prevent form from ever appearing
        this.ShowInTaskbar = false;
        this.WindowState = FormWindowState.Minimized;

        // Initialize menu FIRST
        trayMenu = new ContextMenuStrip();

        powerPlanLabel = new ToolStripMenuItem("Power Plan: ...")
        {
            Enabled = false
        };

        // Tray menu items when right clicking
        trayMenu.Items.Add("Notification Test",null, NotificationTest);
        trayMenu.Items.Add(powerPlanLabel);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Power Plan Settings", null, OpenPowerSettings);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit", null, OnExit);

        string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");

        trayIcon = new NotifyIcon
        {
            Text = "Battery Monitor",
            Icon = new Icon(iconPath),
            ContextMenuStrip = trayMenu,
            Visible = true
        };

        timer = new System.Windows.Forms.Timer
        {
            Interval = 60000 // safer than 30s
        };

        timer.Tick += (s, e) => CheckBattery();
        timer.Start();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Hide(); // ensure no blank window appears
    }

    private void OpenPowerSettings(object? sender, EventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "control.exe",
            Arguments = "powercfg.cpl",
            UseShellExecute = true
        });
    }

    private void NotificationTest(object? sender, EventArgs e)
    {
        Notification();
    }

    private void Notification()
    {
        var info = GetBatteryInfo();

        int level = info.level;
        bool charging = info.charging;
        string notificationText = $"Battery is at {level}%.\nPlease unplug the charger.";
        const MessageBoxButtons buttons = MessageBoxButtons.OK;
        const MessageBoxIcon messageBoxIcon = MessageBoxIcon.Warning;
        this.TopMost = true;
        DialogResult notification = MessageBox.Show(notificationText, "Notification Test", buttons, messageBoxIcon);
        this.TopMost = false;
    }



    private void CheckBattery()
    {
        try
        {
            var info = GetBatteryInfo();

            int level = info.level;
            bool charging = info.charging;

            if (level >= 79 && charging && !notified)
            {
                trayIcon.ShowBalloonTip(
                    5000,
                    "Battery Alert",
                    $"Battery is at {level}%. Consider unplugging charger.",
                    ToolTipIcon.Warning
                );
                Notification();

                notified = true;
            }

            // FIX: reset condition more reliable
            if (!charging || level < 80)
            notified = false;

            string plan = GetActivePowerPlan();

            powerPlanLabel.Text = $"Power Plan: {plan}";
            trayIcon.Text = $"Battery: {level}%";

            // safety: prevent tooltip crash
            if (trayIcon.Text.Length > 63)
                trayIcon.Text = trayIcon.Text[..63];// 0-63
        }
        catch
        {
            // swallow errors to prevent silent exit
        }
    }

    private static (int level, bool charging) GetBatteryInfo()
    {
        using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");

        foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
        {
            int level = Convert.ToInt32(obj["EstimatedChargeRemaining"]);
            int status = Convert.ToInt32(obj["BatteryStatus"]);

            return (level, status == 2);
        }

        return (0, false);
    }

    private static string GetActivePowerPlan()
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powercfg",
            Arguments = "/getactivescheme",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi);

        if (process == null)
            return "Unknown";

        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        int start = output.IndexOf('(');
        int end = output.IndexOf(')');

        if (start != -1 && end != -1 && end > start)
            return output.Substring(start + 1, end - start - 1);

        return "Unknown";
    }

    private void OnExit(object? sender, EventArgs e)
    {
        trayIcon.Visible = false;
        trayIcon.Dispose();
        Application.Exit();
    }
}