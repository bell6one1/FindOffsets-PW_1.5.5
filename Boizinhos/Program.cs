using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class MemoryReader
{
    const uint WM_KEYDOWN = 0x0100;
    const uint WM_KEYUP = 0x0101;

    const uint VK_F12 = 0x7B;
    const uint VK_TAB = 0x09;
    const uint VK_F1 = 0x70;

    const uint VK_0 = 0x30;
    const uint VK_1 = 0x31;
    const uint VK_2 = 0x32;
    const uint VK_3 = 0x33;
    const uint VK_4 = 0x34;
    const uint VK_5 = 0x35;
    const uint VK_6 = 0x36;
    const uint VK_7 = 0x37;
    const uint VK_8 = 0x38;
    const uint VK_9 = 0x39;

    const uint VK_A = 0x41;
    const uint VK_B = 0x42;
    const uint VK_C = 0x43;
    const uint VK_D = 0x44;
    const uint VK_E = 0x45;
    const uint VK_F = 0x46;
    const uint VK_G = 0x47;
    const uint VK_H = 0x48;
    const uint VK_I = 0x49;
    const uint VK_J = 0x4A;
    const uint VK_K = 0x4B;
    const uint VK_L = 0x4C;
    const uint VK_M = 0x4D;
    const uint VK_N = 0x4E;
    const uint VK_O = 0x4F;
    const uint VK_P = 0x50;
    const uint VK_Q = 0x51;
    const uint VK_R = 0x52;
    const uint VK_S = 0x53;
    const uint VK_T = 0x54;
    const uint VK_U = 0x55;
    const uint VK_V = 0x56;
    const uint VK_W = 0x57;
    const uint VK_X = 0x58;
    const uint VK_Y = 0x59;
    const uint VK_Z = 0x5A;

    const uint VK_CTRL = 0x11;
    const uint VK_ALT = 0x12;
    const uint VK_SHIFT = 0x10;


    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

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

    static async Task Main()
    {
        Console.WriteLine("Enter target character names separated by commas:");
        string targetCharacterNamesInput = Console.ReadLine();
        string[] targetCharacterNames = targetCharacterNamesInput.Split(',');

        const int PROCESS_VM_READ = 0x0010; 

        // Calculate relevant processes
        var relevantProcesses = Process.GetProcessesByName("elementclient_64")
            .Where(p => IsTargetProcess(p, targetCharacterNames))
            .Select(p => new { Process = p, ProcessHandle = OpenProcess(PROCESS_VM_READ, false, p.Id) })
            .ToList();

        while (true)
        {
            foreach (var relevantProcess in relevantProcesses)
            {
                if (relevantProcess.ProcessHandle == IntPtr.Zero)
                {
                    Console.WriteLine($"Failed to open process handle for {relevantProcess.Process.ProcessName} (ID: {relevantProcess.Process.Id})");
                    continue;
                }

                try
                {
                    string characterName = ReadCharacterName(relevantProcess.ProcessHandle, new IntPtr(0xE444A4), 0x1C, 0x34, 0x6FC, 0x0);

                    if (characterName != null)
                    {
                        // Inside the loop, after retrieving the character name
                        Console.WriteLine($"Character Name: {characterName}");

                        Console.WriteLine($"Process: {relevantProcess.Process.ProcessName} (ID: {relevantProcess.Process.Id}), Character Name: {characterName}");

                        IntPtr gameWindowHandle = relevantProcess.Process.MainWindowHandle;

                        if (gameWindowHandle != IntPtr.Zero)
                        {
                            await ProcessCharacter(gameWindowHandle);
                        }
                    }
                }
                catch (System.Runtime.InteropServices.SEHException sehEx)
                {
                    Console.WriteLine($"SEHException: {sehEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception during processing: {ex.Message}");
                }

            }

            await Task.Delay(2000);
        }
    }

    static bool IsTargetProcess(Process process, string[] targetNames)
    {
        try
        {
            string characterName = ReadCharacterName(process.Handle, new IntPtr(0xE444A4), 0x1C, 0x34, 0x6FC, 0x0);
            return characterName != null && targetNames.Contains(characterName.Trim(), StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    static async Task ProcessCharacter(IntPtr gameWindowHandle)
    {
        Console.WriteLine($"Processing character in window: {gameWindowHandle}");
        await Task.Delay(100);

        // Simulate key presses
        SimulateKeyPress(gameWindowHandle, VK_1);
        await Task.Delay(300);
        SimulateKeyPress(gameWindowHandle, VK_TAB);
        await Task.Delay(100);
        SimulateKeyPress(gameWindowHandle, VK_1);
        await Task.Delay(100);
    }

    static void SimulateKeyPress(IntPtr windowHandle, uint keyCode)
    {
        PostMessage(windowHandle, WM_KEYDOWN, (IntPtr)keyCode, IntPtr.Zero);
        Thread.Sleep(50);
        PostMessage(windowHandle, WM_KEYUP, (IntPtr)keyCode, IntPtr.Zero);
    }

    static void SimulateKeyRelease(IntPtr windowHandle, uint keyCode)
    {
        PostMessage(windowHandle, WM_KEYUP, (IntPtr)keyCode, IntPtr.Zero);
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

    static string ReadCharacterName(Process process)
    {
        IntPtr baseAddress = new IntPtr(0xE444A4);
        return ReadCharacterName(process.Handle, baseAddress, 0x1C, 0x34, 0x6FC, 0x0);
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
}
