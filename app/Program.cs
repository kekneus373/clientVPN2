using System.Runtime.InteropServices;
using System.Threading;

namespace App;

internal static class Program
{
    private const string MutexName = @"Local\CrystalFinanceVpnLauncher";
    internal const string ActivationMessageName = "CrystalFinanceVpnLauncher.Activate";
    internal const string WindowTitle = "clientVPN v2";
    private const int ActivationRetryCount = 30;
    private const int ActivationRetryDelayMilliseconds = 100;

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
                    "clientVPN v2",
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
        if (message == 0)
        {
            MessageBox.Show(
                "The VPN Launcher is already running, but its activation message could not be registered.",
                WindowTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        for (int attempt = 0; attempt < ActivationRetryCount; attempt++)
        {
            IntPtr existingWindow = FindWindow(null, WindowTitle);
            if (existingWindow != IntPtr.Zero)
            {
                if (PostMessage(existingWindow, message, IntPtr.Zero, IntPtr.Zero))
                {
                    return;
                }

                int error = Marshal.GetLastWin32Error();
                MessageBox.Show(
                    $"The VPN Launcher is already running, but Windows could not activate its window (error {error}).",
                    WindowTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Thread.Sleep(ActivationRetryDelayMilliseconds);
        }

        MessageBox.Show(
            "The VPN Launcher is already starting, but its window did not become available.",
            WindowTitle,
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);
}
