using System.Runtime.InteropServices;
using System.Threading;

namespace App;

internal static class Program
{
    private const string MutexName = @"Local\CrystalFinanceVpnLauncher";
    private const string ActivationMessageName = "CrystalFinanceVpnLauncher.Activate";

    [STAThread]
    private static void Main()
    {
        // The mutex elects one launcher; the registered message wakes that instance when launched again.
        using var instanceMutex = new Mutex(initiallyOwned: true, MutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            ActivateExistingInstance();
            return;
        }

        try
        {
            ApplicationConfiguration.Initialize();

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    exception.Message,
                    "VPN Launcher",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        finally
        {
            instanceMutex.ReleaseMutex();
        }
    }

    private static void ActivateExistingInstance()
    {
        uint message = RegisterWindowMessage(ActivationMessageName);
        if (message == 0 || !PostMessage(new IntPtr(0xffff), message, IntPtr.Zero, IntPtr.Zero))
        {
            MessageBox.Show(
                "The VPN Launcher is already running, but its window could not be activated.",
                "VPN Launcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);
}
