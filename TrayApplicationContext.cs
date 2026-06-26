using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Battery_Monitor_0._1._0;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon trayIcon;
    readonly string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
    private readonly ContextMenuStrip trayMenu;
    private readonly ToolStripMenuItem powerPlanLabel;
    private readonly ToolStripMenuItem batteryLabel;
    private readonly System.Windows.Forms.Timer timer;
    private readonly ToolStripMenuItem repeatAlertsMenuItem;
    private bool alertShown = false;

    public TrayApplicationContext()
    {

            timer = new System.Windows.Forms.Timer();

            timer.Interval = 60000;
            timer.Tick += OnTimerTick;

            timer.Start();

            trayMenu = new ContextMenuStrip();


            batteryLabel = new ToolStripMenuItem("Battery: ...")
            {
                Enabled = false
            };

            powerPlanLabel = new ToolStripMenuItem("Power Plan: ...")
            {
                Enabled = false
            };

            trayMenu.Items.Add(batteryLabel);
            trayMenu.Items.Add(powerPlanLabel);

            trayMenu.Items.Add(new ToolStripSeparator());

            repeatAlertsMenuItem = new ToolStripMenuItem("Repeat Alerts")
                {
                    CheckOnClick = true,
                    Checked = true
                };

                repeatAlertsMenuItem.Click += OnRepeatAlertsChanged;

            trayMenu.Items.Add(repeatAlertsMenuItem);
            trayMenu.Items.Add("Notification Test", null, OnNotificationTest);
            trayMenu.Items.Add("Battery Test", null, OnBatteryTest);
            trayMenu.Items.Add("Refresh", null, OnRefresh);

             trayMenu.Items.Add(new ToolStripSeparator());

            trayMenu.Items.Add("Exit", null, OnExit);

            //icon path;
            Icon icon =
                    File.Exists(iconPath)
                        ? new Icon(iconPath)
                        : SystemIcons.Information;

            trayIcon = new NotifyIcon
            {
                Icon = icon,
                Text = "Battery Monitor",
                Visible = true,
                ContextMenuStrip = trayMenu
            };

            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            UpdateBatteryStatus();
    }

    private void OnRepeatAlertsChanged(object? sender, EventArgs e)
    {
        if (repeatAlertsMenuItem.Checked)
        {
            ShowNotification(
                "Battery Monitor",
                "Repeat alerts enabled.");
        }
        else
        {
            ShowNotification(
                "Battery Monitor",
                "Repeat alerts disabled.");
        }
    }

    private void OnNotificationTest(object? sender, EventArgs e)
    {
        ShowNotification("Notification test","Hello, World!", ToolTipIcon.Info);
    }

    private void OnBatteryTest(object? sender, EventArgs e)
    {
        int batteryPercent = BatteryMonitor.GetBatteryPercentage();
        bool isCharging = BatteryMonitor.IsCharging();
        if (isCharging)
        {
            ShowNotification("Battery Monitor",$"Battery is charging and at {batteryPercent}%", ToolTipIcon.None);
        }
        else
        {
            ShowNotification("Battery Monitor",$"Battery is unplugged and at {batteryPercent}%", ToolTipIcon.None);
        }

    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        UpdateBatteryStatus();
    }

    private void UpdateBatteryStatus()
    {
        int batteryPercent =
        BatteryMonitor.GetBatteryPercentage();

        bool isCharging =
        BatteryMonitor.IsCharging();

        string state = isCharging ? "Charging" : "Battery";
        trayIcon.Text =
        $"Battery: {batteryPercent}%";

        batteryLabel.Text =
        $"Battery: {batteryPercent}% ({state})";

        powerPlanLabel.Text =
        $"Power Plan: {PowerPlan.GetActivePowerPlan()}";

        //if charging, above 80%, and once function disabled
        if (repeatAlertsMenuItem.Checked){
            if (batteryPercent >= 80 &&
                isCharging)
                {
                System.Media.SystemSounds.Exclamation.Play();
                ShowNotification(
                    "Please remove the charger immediately",
                    $"Battery is at {batteryPercent}%",
                    ToolTipIcon.Warning);
                }
        }
        else ////if charging, above 80%, and once function toggled
        {
            if (batteryPercent >= 80 &&
                isCharging && !alertShown)
                {
                System.Media.SystemSounds.Exclamation.Play();
                ShowNotification(
                    "Please remove the charger as soon as possible",
                    $"Battery is at {batteryPercent}%",
                    ToolTipIcon.Warning);

                alertShown = true;

            }
            //reset alert;
            if (batteryPercent < 80 ||
                !isCharging)
            {
                alertShown = false;
            }
        }


    }


    private void OnPowerModeChanged(
            object? sender,
            PowerModeChangedEventArgs e)
    {
        ShowNotification(
        "Power Event",
        $"Mode: {e.Mode}"
        );

        if (e.Mode == PowerModes.StatusChange)
        {
            UpdateBatteryStatus();
        }
    }

    private void OnRefresh(object? sender, EventArgs e)
        {
            UpdateBatteryStatus();

            ShowNotification(
                "Battery Monitor",
                "Battery status refreshed."
            );
        }

    private void ShowNotification(
        string title,
        string message,
        ToolTipIcon icon = ToolTipIcon.Info)
        {
            trayIcon.ShowBalloonTip(
                    5000,
                    title,
                    message,
                    icon
                );
        }

    private void OnExit(object? sender, EventArgs e)
        {
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            trayIcon.Visible = false;
            trayIcon.Dispose();

            ExitThread();
        }
}