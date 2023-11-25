using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

class Teste_1
//observacao: ativar modo streamer!
{
    const uint WM_KEYDOWN = 0x0100;
    const uint WM_KEYUP = 0x0101;
    const uint VK_1 = 0x31;  // Virtual key code for '1'

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    static void Main()
    {
        string processName = "elementclient";
        IntPtr targetWindow = FindWindow(null, "Perfect World Shadow");

        if (targetWindow != IntPtr.Zero)
        {
            // Bring the window to the foreground
            SetForegroundWindow(targetWindow);

            // Keydown
            PostMessage(targetWindow, WM_KEYDOWN, (IntPtr)VK_1, IntPtr.Zero);

            System.Threading.Thread.Sleep(100);

            // Keyup
            PostMessage(targetWindow, WM_KEYUP, (IntPtr)VK_1, IntPtr.Zero);
        }
        else
        {
            Console.WriteLine($"Window not found.");
        }
    }
}
