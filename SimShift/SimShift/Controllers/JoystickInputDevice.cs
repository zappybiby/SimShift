using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Win32;

namespace SimShift.Controllers
{
    public class JoystickInputDevice
    {
        // private const string RegKeyAxisData = @"SYSTEM\ControlSet001\Control\MediaProperties\PrivateProperties\Joystick\OEM";
        private const string RegKeyAxisData = @"System\CurrentControlSet\Control\MediaProperties\PrivateProperties\DirectInput\VID_045E&PID_02FF\Calibration\0\Type";

        private const string RegKeyPlace = @"System\CurrentControlSet\Control\MediaProperties\PrivateProperties\Joystick\OEM\";

        private const string RegReferencePlace = @"System\CurrentControlSet\Control\MediaResources\Joystick\DINPUT.DLL\CurrentJoystickSettings";

        /******************* STATIC ******************/
        static int deviceNumber = 0;

        public Dictionary<int, string> AxisNames = new Dictionary<int, string>();

        public JOYCAPS data;

        public int id;

        public JoystickInputDevice(JOYCAPS captured, int device)
        {
            this.id = device;

            // Copy all members.
            this.data = new JOYCAPS();
            this.data.szPname = captured.szPname;
            this.data.wMid = captured.wMid;
            this.data.wPid = captured.wPid;
            this.data.wXmin = captured.wXmin;
            this.data.wXmax = captured.wXmax;
            this.data.wYmin = captured.wYmin;
            this.data.wYmax = captured.wYmax;
            this.data.wZmin = captured.wZmin;
            this.data.wZmax = captured.wZmax;
            this.data.wNumButtons = captured.wNumButtons;
            this.data.wPeriodMin = captured.wPeriodMin;
            this.data.wPeriodMax = captured.wPeriodMax;

            // Search register.
            RegistryKey rf = Registry.CurrentUser.OpenSubKey(RegReferencePlace);
            if (rf == null)
            {
                return;
            }

            string USBDevice = Convert.ToString(rf.GetValue("Joystick" + (1 + this.id).ToString() + "OEMName"));
            RegistryKey usb = Registry.CurrentUser.OpenSubKey(RegKeyPlace);
            usb = usb.OpenSubKey(USBDevice);
            if (usb == null)
            {
                return;
            }

            this.Name = (string) usb.GetValue("OEMName");

            RegistryKey axisMaster = Registry.CurrentUser.OpenSubKey(RegKeyAxisData);
            this.AxisNames = new Dictionary<int, string>();
            if (axisMaster != null)
            {
                axisMaster = axisMaster.OpenSubKey("Axes");
                if (axisMaster != null)
                {
                    foreach (string name in axisMaster.GetSubKeyNames())
                    {
                        RegistryKey axis = axisMaster.OpenSubKey(name);
                        this.AxisNames.Add(Convert.ToInt32(name), (string) axis.GetValue(string.Empty));
                        axis.Close();
                    }

                    axisMaster.Close();
                }
            }

            rf.Close();
            usb.Close();
        }

        public string Name { get; private set; }

        public static IEnumerable<JoystickInputDevice> Search(string name)
        {
            var results1 = Search();
            var results2 = results1.Where(dev => dev.Name.ToLower().Contains(name.ToLower()));

            return results2;
        }

        public static IEnumerable<JoystickInputDevice> Search()
        {
            List<JoystickInputDevice> Joysticks = new List<JoystickInputDevice>();

            JOYCAPS CapturedJoysticks;
            uint devs = JoystickMethods.joyGetNumDevs();
            for (deviceNumber = 0; deviceNumber < devs; deviceNumber++)
            {
                uint res = JoystickMethods.joyGetDevCaps(deviceNumber, out CapturedJoysticks, JOYCAPS.Size);
                if (res != 165)
                {
                    Joysticks.Add(new JoystickInputDevice(CapturedJoysticks, deviceNumber));
                }
            }

            return Joysticks;
        }
    }
}