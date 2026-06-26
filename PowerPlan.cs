using System.Diagnostics;

namespace Battery_Monitor_0._1._0;

public static class PowerPlan
{
    public static string GetActivePowerPlan()
    {
        ProcessStartInfo psi = new()
        {
            FileName = "powercfg",
            Arguments = "/getactivescheme",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process? process = Process.Start(psi);

        if (process == null)
            return "Unknown";

        string output = process.StandardOutput.ReadToEnd();

        process.WaitForExit();

        int start = output.IndexOf('(');
        int end = output.IndexOf(')');

        if (start >= 0 &&
            end > start)
        {
            return output.Substring(
                start + 1,
                end - start - 1);
        }

        return "Unknown";
    }
}