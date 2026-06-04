using System;
using System.Drawing;
using System.Windows.Forms;
using System.Management;
using System.Linq; // FIX: needed for .Cast<>

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
        this.ShowInTaskbar = false;

        // FIX: Initialize trayMenu BEFORE using it
        trayMenu = new ContextMenuStrip();

        powerPlanLabel = new ToolStripMenuItem("Power Plan: ...")
        {
            Enabled = false
        };

        trayMenu.Items.Add(powerPlanLabel);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Power Plan Settings", null, OpenPowerSettings);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Exit", null, OnExit);
        
        string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");

        trayIcon = new NotifyIcon
        {
            Text = "Battery Monitor (80%)",
            Icon = new Icon(iconPath),
            ContextMenuStrip = trayMenu,
            Visible = true
        };

        CheckBattery();

        timer = new System.Windows.Forms.Timer
        {
            Interval = 30000
        };

        timer.Tick += (s, e) => CheckBattery();
        timer.Start();

        Application.ThreadException += (s, e) =>
            {
                MessageBox.Show(e.Exception.ToString(), "Thread Exception");
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                MessageBox.Show(e.ExceptionObject.ToString(), "Unhandled Exception");
            };
    }

    protected override void OnLoad(EventArgs e)
    {
            base.OnLoad(e);

            ShowInTaskbar = false;
            Hide();
    }

    protected override void SetVisibleCore(bool value)
    {
        base.SetVisibleCore(false);
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

    private void CheckBattery()
    {
        try {
        var info = GetBatteryInfo();

        int level = info.level;
        bool charging = info.charging;

        if (level >= 80 && charging && !notified)
        {
            MessageBox.Show(
                $"Battery is at {level}%. Unplug charger.",
                "Battery Alert",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button1
                //MessageBoxOptions.DefaultDesktopOnly ;disabled for testing
            );

            notified = true;
        }

        if (level < 80)
        {
            notified = false;
        }

        string plan = GetActivePowerPlan();
        powerPlanLabel.Text = $"Power Plan: {plan}";
        trayIcon.Text = $"Battery: {level}% | {plan}";

         }
    catch (Exception ex)
    {
        MessageBox.Show(
            ex.ToString(),
            "Battery Monitor Error"
        );
    }
    }

    private static (int level, bool charging) GetBatteryInfo()
    {
        using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");

        foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
        {
            int level = Convert.ToInt32(obj["EstimatedChargeRemaining"]);
            int status = Convert.ToInt32(obj["BatteryStatus"]);

            bool charging = (status == 2);

            return (level, charging);
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

        if (process == null) return "Unknown"; // FIX: null safety

        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        int start = output.IndexOf('(');
        int end = output.IndexOf(')');

        if (start != -1 && end != -1 && end > start)
        {
            return output.Substring(start + 1, end - start - 1);
        }

        return "Unknown";
    }

    private void OnExit(object? sender, EventArgs e)
    {
        trayIcon.Visible = false;
        trayIcon.Dispose();
        Application.Exit();
    }
}