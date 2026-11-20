using System;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace RecoilStudio
{
    public static class NativeInput
    {
        public const int INPUT_MOUSE = 0;
        public const uint MOUSEEVENTF_MOVE = 0x0001;

        public const int VK_LBUTTON = 0x01;
        public const int VK_RBUTTON = 0x02;
        public const int VK_MBUTTON = 0x04;
        public const int VK_XBUTTON1 = 0x05;
        public const int VK_XBUTTON2 = 0x06;
        public const int VK_BACK = 0x08;
        public const int VK_TAB = 0x09;
        public const int VK_RETURN = 0x0D;
        public const int VK_SHIFT = 0x10;
        public const int VK_CONTROL = 0x11;
        public const int VK_MENU = 0x12;
        public const int VK_PAUSE = 0x13;
        public const int VK_CAPITAL = 0x14;
        public const int VK_ESCAPE = 0x1B;
        public const int VK_SPACE = 0x20;
        public const int VK_OEM_102 = 0xE2;

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
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
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

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUT_UNION
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public INPUT_UNION u;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint uMilliseconds);

        public static void EnableDpiAwareness()
        {
            try
            {
                SetProcessDPIAware();
            }
            catch
            {
            }
        }

        public static void BeginHighResolutionTimer()
        {
            try
            {
                timeBeginPeriod(1);
            }
            catch
            {
            }
        }

        public static void EndHighResolutionTimer()
        {
            try
            {
                timeEndPeriod(1);
            }
            catch
            {
            }
        }

        public static bool IsKeyDown(int vKey)
        {
            if (vKey <= 0) return false;
            return (GetAsyncKeyState(vKey) & 0x8000) != 0;
        }

        public static void SendMouseMove(int dx, int dy)
        {
            if (dx == 0 && dy == 0) return;

            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].u.mi = new MOUSEINPUT
            {
                dx = dx,
                dy = dy,
                mouseData = 0,
                dwFlags = MOUSEEVENTF_MOVE,
                time = 0,
                dwExtraInfo = IntPtr.Zero
            };

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public static string GetKeyDisplayName(int vkCode)
        {
            if (vkCode <= 0) return "Ninguno";

            switch (vkCode)
            {
                case 0x04: return "Clic Medio";
                case 0x05: return "Mouse Lat. 1";
                case 0x06: return "Mouse Lat. 2";
                case 0x08: return "Retroceso";
                case 0x09: return "Tab";
                case 0x0D: return "Enter";
                case 0x10:
                case 0xA0:
                case 0xA1: return "Shift";
                case 0x11:
                case 0xA2:
                case 0xA3: return "Ctrl";
                case 0x12:
                case 0xA4:
                case 0xA5: return "Alt";
                case 0x13: return "Pausa";
                case 0x14: return "Bloq Mayús";
                case 0x1B: return "Esc";
                case 0x20: return "Espacio";
                case 0x21: return "Re Pág";
                case 0x22: return "Av Pág";
                case 0x23: return "Fin";
                case 0x24: return "Inicio";
                case 0x25: return "Flecha Izq";
                case 0x26: return "Flecha Arr";
                case 0x27: return "Flecha Der";
                case 0x28: return "Flecha Aba";
                case 0x2D: return "Insert";
                case 0x2E: return "Supr";
                case 0x5B:
                case 0x5C: return "Win";
                case 0x6A: return "Num *";
                case 0x6B: return "Num +";
                case 0x6D: return "Num -";
                case 0x6E: return "Num .";
                case 0x6F: return "Num /";
                case 0x90: return "Bloq Num";
                case 0x91: return "Bloq Despl";
                case 0xBA: return "Ñ / ;";
                case 0xBB: return "+ / =";
                case 0xBC: return ",";
                case 0xBD: return "-";
                case 0xBE: return ".";
                case 0xBF: return "Ç / }";
                case 0xC0: return "` / º";
                case 0xDB: return "' / ?";
                case 0xDC: return "\\ / Ç";
                case 0xDD: return "¡ / ¿";
                case 0xDE: return "´ / ¨";
                case 0xE2: return "< > (ISO)";
            }

            if (vkCode >= 0x70 && vkCode <= 0x7B)
            {
                return "F" + (vkCode - 0x70 + 1).ToString();
            }

            if (vkCode >= 0x60 && vkCode <= 0x69)
            {
                return "Num " + (vkCode - 0x60).ToString();
            }

            if (vkCode >= 0x30 && vkCode <= 0x39)
            {
                return ((char)vkCode).ToString();
            }

            if (vkCode >= 0x41 && vkCode <= 0x5A)
            {
                return ((char)vkCode).ToString();
            }

            try
            {
                Key wpfKey = KeyInterop.KeyFromVirtualKey(vkCode);
                if (wpfKey != Key.None)
                {
                    return wpfKey.ToString();
                }
            }
            catch
            {
            }

            return string.Format("Tecla 0x{0:X2}", vkCode);
        }
    }
}
