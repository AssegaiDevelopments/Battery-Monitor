using System.Windows.Forms;

namespace Battery_Monitor_0._1._0;

public static class BatteryMonitor
{
    public static int GetBatteryPercentage()
        {
            PowerStatus power = SystemInformation.PowerStatus;

            return (int)(power.BatteryLifePercent * 100);
    }

    public static bool IsCharging()
        {
            PowerStatus power = SystemInformation.PowerStatus;

            return power.PowerLineStatus == PowerLineStatus.Online;
    }

}