using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BabySmash
{
    class KeyControl
    {
        public static char GetDisplayChar(Key key)
        {
            // If a number on the normal number track is pressed, display the number.
            if (key >= Key.D0 && key <= Key.D9)
            {
                return (char)('0' + key - Key.D0);
            }

            // If a number on the numpad is pressed, display the number.
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
            {
                return (char)('0' + key - Key.NumPad0);
            }

            try
            {
                return char.ToUpperInvariant(KeyControl.TryGetLetter(key));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Assert(false, ex.ToString());
                return '*';
            }
        }

        public enum MapType : uint
        {
            MAPVK_VK_TO_VSC = 0x0,
            MAPVK_VSC_TO_VK = 0x1,
            MAPVK_VK_TO_CHAR = 0x2,
            MAPVK_VSC_TO_VK_EX = 0x3,
        }

        [DllImport("user32.dll")]
        public static extern int ToUnicode(
                uint wVirtKey,
                uint wScanCode,
                byte[] lpKeyState,
                [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
                        StringBuilder pwszBuff,
                int cchBuff,
                uint wFlags);

        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        public static char TryGetLetter(Key key)
        {
            char ch = ' ';

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
            StringBuilder stringBuilder = new StringBuilder(2);

            int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
            switch (result)
            {
                case -1:
                    break;
                case 0:
                    break;
                case 1:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
                default:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
            }
            return ch;
        }

    }
}
