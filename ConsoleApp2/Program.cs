using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
class MemoryReader
{
    const int PROCESS_ALL_ACCESS = 0x1F0FFF;

    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        [Out] byte[] lpBuffer,
        int dwSize,
        out int lpNumberOfBytesRead
    );

    [DllImport("kernel32.dll")]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    const uint WM_KEYDOWN = 0x0100;
    const uint WM_KEYUP = 0x0101;

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    static void SimulateKeyPress(IntPtr windowHandle, uint keyCode)
    {
        PostMessage(windowHandle, WM_KEYDOWN, (IntPtr)keyCode, IntPtr.Zero);
    }

    static void SimulateKeyRelease(IntPtr windowHandle, uint keyCode)
    {
        PostMessage(windowHandle, WM_KEYUP, (IntPtr)keyCode, IntPtr.Zero);
    }


    const uint INPUT_KEYBOARD = 1;
    const uint KEYEVENTF_KEYUP = 0x0002;
    const uint VK_F12 = 0x7B;
    const uint VK_TAB = 0x09;
    const uint VK_F1 = 0x70;
    const uint VK_F2 = 0x70;
    const int SW_RESTORE = 9;


    static async Task Main()
    {
        Console.WriteLine("Enter target character names separated by commas:");
        string targetCharacterNamesInput = Console.ReadLine();
        string[] targetCharacterNames = targetCharacterNamesInput.Split(',');

        IntPtr baseAddress = new IntPtr(0xE444A4);

        List<Task> tasks = new List<Task>();
        SemaphoreSlim semaphore = new SemaphoreSlim(2);

        foreach (Process process in Process.GetProcesses())
        {
            IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);

            if (processHandle != IntPtr.Zero)
            {
                string characterName = ReadCharacterName(processHandle, baseAddress, 0x1C, 0x34, 0x6FC, 0x0);

                if (characterName != null && Array.Exists(targetCharacterNames, name => name.Trim().Equals(characterName.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine($"Process: {process.ProcessName} (ID: {process.Id}), Character Name: {characterName}");

                    IntPtr gameWindowHandle = process.MainWindowHandle;

                    if (gameWindowHandle != IntPtr.Zero)
                    {
                        tasks.Add(ProcessGame(process, baseAddress, gameWindowHandle, semaphore));
                    }
                }

                CloseHandle(processHandle);
            }
        }

        await Task.WhenAll(tasks);
    }

    static async Task ProcessGame(Process process, IntPtr baseAddress, IntPtr gameWindowHandle, SemaphoreSlim semaphore)
    {
        IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);

        if (processHandle != IntPtr.Zero)
        {
            try
            {
                while (true)
                {
                    Console.WriteLine($"Process: {process.ProcessName} (ID: {process.Id})");

                    int targetId = ReadTargetId(processHandle, baseAddress, 0x1C, 0x34, 0x5A4);

                    while (targetId == 0 || (targetId > 0 && targetId < 100000) || Math.Abs(targetId).ToString().Length >= 3 && Math.Abs(targetId).ToString().Reverse().ElementAt(2) == '1')
                    {
                        Console.WriteLine($"Target ID: {targetId}");
                        Console.WriteLine("Target ID matches the undesired value. Sending Tab key press...");

                        SetForegroundWindow(gameWindowHandle);
                        await Task.Delay(100);

                        SimulateKeyPress(gameWindowHandle, VK_F12);
                        await Task.Delay(50);
                        SimulateKeyRelease(gameWindowHandle, VK_F12);
                        await Task.Delay(300);
                        SimulateKeyPress(gameWindowHandle, VK_F12);
                        await Task.Delay(50);
                        SimulateKeyRelease(gameWindowHandle, VK_F12);
                        await Task.Delay(300);

                        SetForegroundWindow(gameWindowHandle);
                        await Task.Delay(100);

                        SimulateKeyPress(gameWindowHandle, VK_TAB);
                        await Task.Delay(50);
                        SimulateKeyRelease(gameWindowHandle, VK_TAB);
                        await Task.Delay(100);

                        targetId = ReadTargetId(processHandle, baseAddress, 0x1C, 0x34, 0x5A4);
                    }

                    if (gameWindowHandle != IntPtr.Zero)
                    {
                        SetForegroundWindow(gameWindowHandle);
                        await Task.Delay(100);

                        Console.WriteLine($"Sending F1 key press to window: {gameWindowHandle}");
                        SimulateKeyPress(gameWindowHandle, VK_F1);
                        await Task.Delay(100);
                        SimulateKeyRelease(gameWindowHandle, VK_F1);
                        await Task.Delay(5000);
                    }

                    SetForegroundWindow(gameWindowHandle);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in ProcessGame loop: {ex.Message}");
            }
            finally
            {
                CloseHandle(processHandle);
            }
        }
    }


    static int ReadTargetId(IntPtr processHandle, IntPtr baseAddress, params int[] offsets)
    {
        IntPtr address = baseAddress;

        foreach (int offset in offsets)
        {
            byte[] buffer = new byte[sizeof(int)];
            int bytesRead;

            try
            {
                if (!ReadProcessMemory(processHandle, address, buffer, buffer.Length, out bytesRead))
                {
                    return -1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during ReadProcessMemory: {ex.Message}");
                return -1; 
            }

            address = (IntPtr)BitConverter.ToInt32(buffer, 0);
            address = IntPtr.Add(address, offset);
        }

        int targetId;

        try
        {
            byte[] buffer = new byte[sizeof(int)];
            int bytesRead;

            if (!ReadProcessMemory(processHandle, address, buffer, buffer.Length, out bytesRead))
            {
                return -1;
            }

            targetId = BitConverter.ToInt32(buffer, 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during ReadProcessMemory: {ex.Message}");
            return -1;
        }

        return targetId;
    }
    static string ReadCharacterName(IntPtr processHandle, IntPtr baseAddress, params int[] offsets)
    {
        IntPtr address = baseAddress;

        foreach (int offset in offsets)
        {
            byte[] buffer = new byte[sizeof(int)];
            int bytesRead;

            try
            {
                if (!ReadProcessMemory(processHandle, address, buffer, buffer.Length, out bytesRead))
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during ReadProcessMemory: {ex.Message}");
                return null;
            }

            address = (IntPtr)BitConverter.ToInt32(buffer, 0);
            address = IntPtr.Add(address, offset);
        }

        List<byte> nameBytes = new List<byte>();
        byte[] bufferChar = new byte[sizeof(char)];
        int bytesReadChar;

        // Read characters until null terminator is encountered
        while (true)
        {
            try
            {
                if (!ReadProcessMemory(processHandle, address, bufferChar, bufferChar.Length, out bytesReadChar))
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during ReadProcessMemory: {ex.Message}");
                return null;
            }

            char character = BitConverter.ToChar(bufferChar, 0);
            if (character == '\0')
            {
                break; 
            }

            nameBytes.AddRange(bufferChar);
            address = IntPtr.Add(address, sizeof(char));
        }

        return Encoding.Unicode.GetString(nameBytes.ToArray());
    }

    static void SimulateKeyPress(uint keyCode)
    {
        INPUT[] inputs = new INPUT[1];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].U.ki.wVk = (ushort)keyCode;
        inputs[0].U.ki.dwFlags = 0;

        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    static void SimulateKeyRelease(uint keyCode)
    {
        INPUT[] inputs = new INPUT[1];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].U.ki.wVk = (ushort)keyCode;
        inputs[0].U.ki.dwFlags = KEYEVENTF_KEYUP;

        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }
}