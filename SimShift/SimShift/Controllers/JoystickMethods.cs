using System;
using System.Runtime.InteropServices;

namespace SimShift.Controllers
{
    public static class JoystickMethods
    {
        [DllImport("Winmm.dll")]
        public static extern uint joyGetDevCaps(int uJoyID, out JOYCAPS pjc, int cbjc);

        [DllImport("Winmm.dll")]
        public static extern uint joyGetNumDevs();

        [DllImport("Winmm.dll")]
        public static extern uint joyGetPosEx(int uJoyID, out JOYINFOEX pji);
    }

    public enum JoystickError
    {
        NoError = 0,

        InvalidParameters = 165,

        NoCanDo = 166,

        Unplugged = 167
    }

    [Flags()]
    public enum JoystickFlags
    {
        JOY_RETURNX = 0x1,

        JOY_RETURNY = 0x2,

        JOY_RETURNZ = 0x4,

        JOY_RETURNR = 0x8,

        JOY_RETURNU = 0x10,

        JOY_RETURNV = 0x20,

        JOY_RETURNPOV = 0x40,

        JOY_RETURNBUTTONS = 0x80,

        JOY_RETURNALL = JOY_RETURNX | JOY_RETURNY | JOY_RETURNZ | JOY_RETURNR | JOY_RETURNU | JOY_RETURNV | JOY_RETURNPOV | JOY_RETURNBUTTONS
    }

    public class WinMM
    {
        // Verreweg van compleet
        public const int MAXPNAMELEN = 32;
    }

    [Flags]
    public enum JoystCapsFlags
    {
        HasZ = 0x1,

        HasR = 0x2,

        HasU = 0x4,

        HasV = 0x8,

        HasPov = 0x16,

        HasPov4Dir = 0x32,

        HasPovContinuous = 0x64
    }

    public struct JOYCAPS
    {
        public short wMid;

        public short wPid;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = WinMM.MAXPNAMELEN)]
        public string szPname;

        public uint wXmin;

        public uint wXmax;

        public uint wYmin;

        public uint wYmax;

        public uint wZmin;

        public uint wZmax;

        public uint wNumButtons;

        public uint wPeriodMin;

        public uint wPeriodMax;

        public uint RMin;

        public uint RMax;

        public uint UMin;

        public uint UMax;

        public uint VMin;

        public uint VMax;

        public JoystCapsFlags Capabilities;

        public uint MaxAxes;

        public uint NumAxes;

        public uint MaxButtons;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string RegKey;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string OemVxD;

        public static readonly int SizeInBytes;

        public static int Size => Marshal.SizeOf(default(JOYCAPS));
    }

    public struct JOYINFOEX
    {
        public int dwSize;

        public JoystickFlags dwFlags;

        public int dwXpos; // Current X-coordinate.

        public int dwYpos; // Current Y-coordinate.

        public int dwZpos; // Current Z-coordinate.

        public int dwRpos; // Current position of the rudder or fourth joystick axis.

        public int dwUpos; // Current fifth axis position.

        public int dwVpos; // Current sixth axis position.

        public int dwButtons; // Current state of the 32 joystick buttons (bits)

        public int dwButtonNumber; // Current button number that is pressed.

        public int dwPOV; // Current position of the point-of-view control (0..35,900, deg*100)

        public int dwReserved1;

        public int dwReserved2;
    }

    public struct JOYINFO
    {
        public int wXpos; // Current X-coordinate.

        public int wYpos; // Current Y-coordinate.

        public int wZpos; // Current Z-coordinate.

        public int wButtons; // Current state of joystick buttons.
    }
}