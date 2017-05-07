﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SimShift.Controllers;
using SimShift.Data;
using SimShift.Entities;
using SimShift.MapTool;
using SimShift.Models;
using SimShift.Utils;

namespace SimShift.Services
{
    public class Main
    {
        public static ACC ACC;

        // Modules
        public static Antistall Antistall;

        public static CameraHorizon CameraHorizon;

        public static Profiles CarProfile;

        public static ControlChain Controls;

        public static CruiseControl CruiseControl;

        public static DataArbiter Data;

        public static IDrivetrain Drivetrain;

        public static DrivetrainCalibrator DrivetrainCalibrator;

        public static LaneAssistance LaneAssistance;

        public static LaunchControl LaunchControl;

        public static ProfileSwitcher ProfileSwitcher;

        public static List<JoystickInput> RawJoysticksIn = new List<JoystickInput>();

        public static List<JoystickOutput> RawJoysticksOut = new List<JoystickOutput>();

        public static Speedlimiter Speedlimiter;

        public static TractionControl TractionControl;

        public static Transmission Transmission;

        public static TransmissionCalibrator TransmissionCalibrator;

        public static VariableSpeedTransmission VariableSpeedControl;

        public static bool VST = false; // { get { return Main.DrivetrainCalibrator.Active? false:true; }}

        private static Dictionary<JoyControls, double> AxisFeedback = new Dictionary<JoyControls, double>();

        private static Dictionary<JoyControls, bool> ButtonFeedback = new Dictionary<JoyControls, bool>();

        private static bool dskCtlActive = false;

        private static float lastThrottle = 0.0f;

        private static int profileIndexLoaded = 0;

        private static bool ps4CtlActive = false;

        private static bool requiresSetup = true;

        public static JoystickInput Controller
        {
            get
            {
                return ps4CtlActive || dskCtlActive ? RawJoysticksIn[0] : RawJoysticksIn[1];
            }
        }

        public static Ets2Mapper LoadedMap { get; set; }

        public static bool Running { get; private set; }

        public static double GetAxisIn(JoyControls c)
        {
            switch (c)
            {
                case JoyControls.VstLever:
                    if (VST) return 1 - RawJoysticksIn[0].GetAxis(2) / Math.Pow(2, 16);
                    else
                    {
                        return 1;
                    }
                    break;

                case JoyControls.Steering:
                    if (ps4CtlActive)
                    {
                        var s = RawJoysticksIn[0].GetAxis(0) / Math.Pow(2, 16) - 0.5;

                        s *= 2;
                        var wasn = s < 0;
                        s = s * s;
                        s /= 2;
                        if (wasn) s *= -1;
                        s += 0.5;
                        return s;
                    }
                    else if (dskCtlActive)
                    {
                        var steer1 = Controller.GetAxis(3) / Math.Pow(2, 15) - 1;
                        var steer2 = Controller.GetAxis(0) / Math.Pow(2, 15) - 1;
                        if (steer1 < 0) steer1 = steer1 * steer1 * -1;
                        else steer1 *= steer1;
                        if (steer2 < 0) steer2 = steer2 * steer2 * -1;
                        else steer2 *= steer2;
                        if (Math.Abs(steer1) > Math.Abs(steer2)) return (steer1 + 1) / 2;
                        else return (steer2 + 1) / 2;
                    }
                    else return RawJoysticksIn[1].GetAxis(0) / Math.Pow(2, 16);

                case JoyControls.Throttle:

                    double t = 0;

                    if (ps4CtlActive)
                    {
                        t = RawJoysticksIn[0].GetAxis(4) / Math.Pow(2, 16);
                        t = t * t;
                        return t;
                    }
                    else if (dskCtlActive)
                    {
                        if (VST)
                        {
                            t = 1 - RawJoysticksIn[1].GetAxis(2) / Math.Pow(2, 16);
                        }
                        else
                        {
                            t = (0.5 - (RawJoysticksIn[0].GetAxis(2) / Math.Pow(2, 16))) * 2;
                        }
                    }
                    else
                    {
                        t = 1 - RawJoysticksIn[1].GetAxis(2) / Math.Pow(2, 16);
                    }

                    if (t < 0) t = 0;

                    t = t * 0.05 + lastThrottle * 0.95;
                    lastThrottle = (float) t;

                    return t;

                case JoyControls.Brake:
                    if (ps4CtlActive)
                    {
                        var b = RawJoysticksIn[0].GetAxis(5) / Math.Pow(2, 16);
                        b = b * b;
                        return b;
                    }
                    else if (dskCtlActive)
                    {
                        if (VST)
                        {
                            var b = 1 - RawJoysticksIn[1].GetAxis(3) / Math.Pow(2, 16);
                            if (b < 0) b = 0;

                            if (Main.Data.Active == null || Main.Data.Active.Application == "TestDrive2") return b;
                            return b * b;
                        }
                        else
                        {
                            return ((RawJoysticksIn[0].GetAxis(2) / Math.Pow(2, 16)) - 0.5) * 2;
                        }
                    }
                    else
                    {
                        var b = 1 - RawJoysticksIn[1].GetAxis(3) / Math.Pow(2, 16);
                        if (b < 0) b = 0;

                        if (Main.Data.Active == null || Main.Data.Active.Application == "TestDrive2") return b;
                        return b * b;
                    }
                case JoyControls.Clutch:
                    if (ps4CtlActive) return 0;
                    else if (dskCtlActive) return 0;
                    else return 1 - RawJoysticksIn[1].GetAxis(4) / Math.Pow(2, 16);

                case JoyControls.CameraHorizon: return 0; // was only on ps3 controller

                default: return 0.0;
            }
        }

        public static double GetAxisOut(JoyControls ctrl)
        {
            if (AxisFeedback.ContainsKey(ctrl)) return AxisFeedback[ctrl];
            return 0;
        }

        public static bool GetButtonIn(JoyControls c)
        {
            switch (c)
            {
                case JoyControls.Gear1: return !ps4CtlActive && !dskCtlActive && RawJoysticksIn[1].GetButton(8);
                case JoyControls.Gear2: return !ps4CtlActive && !dskCtlActive && RawJoysticksIn[1].GetButton(9);
                case JoyControls.Gear3: return !ps4CtlActive && !dskCtlActive && RawJoysticksIn[1].GetButton(10);
                case JoyControls.Gear4: return !ps4CtlActive && !dskCtlActive && RawJoysticksIn[1].GetButton(11);
                case JoyControls.Gear5: return !ps4CtlActive && !dskCtlActive && RawJoysticksIn[1].GetButton(12);
                case JoyControls.Gear6: return !ps4CtlActive && !dskCtlActive && RawJoysticksIn[1].GetButton(13);
                case JoyControls.Gear7: return false;
                case JoyControls.Gear8: return false;
                case JoyControls.GearR: return !ps4CtlActive && !dskCtlActive && RawJoysticksIn[1].GetButton(14);

                case JoyControls.GearRange1: return !ps4CtlActive && !dskCtlActive && RawJoysticksIn[1].GetButton(6);

                case JoyControls.GearRange2: return !ps4CtlActive && !dskCtlActive && RawJoysticksIn[1].GetButton(17);

                /*** NOT FUNCTIONAL ***/
                case JoyControls.LaneAssistance:
                    if (ps4CtlActive) return false;
                    else if (dskCtlActive) return RawJoysticksIn[0].GetButton(7);
                    else return RawJoysticksIn[1].GetButton(7);

                case JoyControls.VstChange: return !ps4CtlActive && !dskCtlActive && RawJoysticksIn[1].GetButton(10);
                case JoyControls.MeasurePower: return !ps4CtlActive && !dskCtlActive && RawJoysticksIn[1].GetButton(11);

                // PS3 (via DS3 tool) L1/R1
                case JoyControls.GearDown:
                    if (ps4CtlActive) return RawJoysticksIn[0].GetButton(4);
                    else if (dskCtlActive) return RawJoysticksIn[0].GetButton(8);
                    else if (Transmission.Enabled) return RawJoysticksIn[1].GetButton(8);
                    else return false;
                case JoyControls.GearUp:
                    if (ps4CtlActive) return RawJoysticksIn[0].GetButton(5);
                    else if (dskCtlActive) return RawJoysticksIn[0].GetButton(4);
                    else if (Transmission.Enabled) return RawJoysticksIn[1].GetButton(9);
                    else return false;
                case JoyControls.CruiseControlMaintain:
                    if (ps4CtlActive) return RawJoysticksIn[0].GetButton(3);
                    else if (dskCtlActive) return RawJoysticksIn[0].GetButton(9);
                    else return RawJoysticksIn[1].GetButton(15);

                case JoyControls.CruiseControlUp: return !ps4CtlActive && !dskCtlActive && RawJoysticksIn[1].GetPov(2);
                case JoyControls.CruiseControlDown: return !ps4CtlActive && !dskCtlActive && RawJoysticksIn[1].GetPov(0);
                case JoyControls.CruiseControlOnOff: return !ps4CtlActive && !dskCtlActive && RawJoysticksIn[1].GetPov(1);

                case JoyControls.LaunchControl:
                    if (ps4CtlActive) return false;
                    else if (dskCtlActive) return RawJoysticksIn[0].GetButton(6);
                    else return RawJoysticksIn[1].GetButton(18);

                default: return false;
            }
            // Map user config -> controller
        }

        public static bool GetButtonOut(JoyControls ctrl)
        {
            if (ButtonFeedback.ContainsKey(ctrl)) return ButtonFeedback[ctrl];
            return false;
        }

        public static bool Load(IConfigurable target, string iniFile)
        {
            Debug.WriteLine("Loading configuration file " + iniFile);
            // Reset to default
            target.ResetParameters();
            try
            {
                // Load custom settings from INI file
                using (var ini = new IniReader(iniFile, true))
                {
                    ini.AddHandler(
                        (x) =>
                            {
                                if (target.AcceptsConfigs.Any(y => y == x.Group))
                                {
                                    target.ApplyParameter(x);
                                }
                            });
                    ini.Parse();
                }
                return true;
            }
            catch
            {
                Debug.WriteLine("Failed to load configuration from " + iniFile);
            }
            return false;
            // DONE :)
        }

        public static void LoadNextProfile(float staticMass)
        {
            if (profileIndexLoaded >= CarProfile.Loaded.Count) profileIndexLoaded = 0;
            if (CarProfile.Loaded.Count == 0) return;
            CarProfile.Load(CarProfile.Loaded.Skip(profileIndexLoaded).FirstOrDefault().Name, staticMass);
            profileIndexLoaded++;
            if (profileIndexLoaded >= CarProfile.Loaded.Count)
            {
                profileIndexLoaded = 0;
            }
        }

        public static void ReloadProfile(float staticMass)
        {
            if (CarProfile == null) return;
            if (CarProfile.Loaded.Count == 0) return;
            var pr = profileIndexLoaded - 1;
            if (pr < 0) pr = CarProfile.Loaded.Count - 1;

            CarProfile.Load(CarProfile.Loaded.Skip(pr).FirstOrDefault().Name, staticMass);
        }

        public static void SetAxisOut(JoyControls c, double value)
        {
            switch (c)
            {
                default: break;

                case JoyControls.Steering:
                    RawJoysticksOut[0].SetAxis(HID_USAGES.HID_USAGE_RX, value / 2);
                    break;

                case JoyControls.Throttle:
                    RawJoysticksOut[0].SetAxis(HID_USAGES.HID_USAGE_X, value / 2);
                    break;

                case JoyControls.Brake:
                    RawJoysticksOut[0].SetAxis(HID_USAGES.HID_USAGE_Y, value / 2);
                    break;

                case JoyControls.Clutch:
                    RawJoysticksOut[0].SetAxis(HID_USAGES.HID_USAGE_Z, value / 2);
                    break;
            }

            try
            {
                if (AxisFeedback.ContainsKey(c)) AxisFeedback[c] = value;
                else AxisFeedback.Add(c, value);
            }
            catch
            { }
        }

        public static void SetButtonOut(JoyControls c, bool value)
        {
            switch (c)
            {
                default: break;

                case JoyControls.Gear1:
                    RawJoysticksOut[0].SetButton(1, value);
                    break;

                case JoyControls.Gear2:
                    RawJoysticksOut[0].SetButton(2, value);
                    break;

                case JoyControls.Gear3:
                    RawJoysticksOut[0].SetButton(3, value);
                    break;

                case JoyControls.Gear4:
                    RawJoysticksOut[0].SetButton(4, value);
                    break;

                case JoyControls.Gear5:
                    RawJoysticksOut[0].SetButton(5, value);
                    break;

                case JoyControls.Gear6:
                    RawJoysticksOut[0].SetButton(6, value);
                    break;

                case JoyControls.Gear7:
                    RawJoysticksOut[0].SetButton(11, value);
                    break;

                case JoyControls.Gear8:
                    RawJoysticksOut[0].SetButton(12, value);
                    break;

                case JoyControls.GearR:
                    RawJoysticksOut[0].SetButton(7, value);
                    break;

                case JoyControls.GearRange1:
                    RawJoysticksOut[0].SetButton(8, value);
                    break;

                case JoyControls.GearRange2:
                    RawJoysticksOut[0].SetButton(9, value);
                    break;

                case JoyControls.CruiseControlMaintain:
                    RawJoysticksOut[0].SetButton(10, value);
                    break;
            }
            try
            {
                if (ButtonFeedback.ContainsKey(c)) ButtonFeedback[c] = value;
                else ButtonFeedback.Add(c, value);
            }
            catch
            { }
        }

        public static void SetMap(Ets2Mapper ets2Map)
        {
            LoadedMap = ets2Map;
        }

        public static bool Setup()
        {
            if (requiresSetup)
            {
                requiresSetup = false;

                JoystickInput deskCtrl, g25Wheel, ps4Ctrl;

                // Joysticks
                var dskObj = JoystickInputDevice.Search("Hotas").FirstOrDefault();
                deskCtrl = dskObj == null ? default(JoystickInput) : new JoystickInput(dskObj);

                var ps4Obj = JoystickInputDevice.Search("Wireless Controller").FirstOrDefault();
                ps4Ctrl = ps4Obj == null ? default(JoystickInput) : new JoystickInput(ps4Obj);

                var g25Obj = JoystickInputDevice.Search("G25").FirstOrDefault();
                g25Wheel = g25Obj == null ? default(JoystickInput) : new JoystickInput(g25Obj);
                var vJoy = new JoystickOutput();

                // add main controller:
                if (dskCtlActive) RawJoysticksIn.Add(deskCtrl);
                else if (ps4CtlActive) RawJoysticksIn.Add(ps4Ctrl);
                else RawJoysticksIn.Add(default(JoystickInput));
                RawJoysticksIn.Add(g25Wheel);
                RawJoysticksOut.Add(vJoy);

                // Data source
                Data = new DataArbiter();

                Data.CarChanged += (s, e) =>
                    {
                        if (Data.Active.Application == "eurotrucks2") Drivetrain = new Ets2Drivetrain();
                        else Drivetrain = new GenericDrivetrain();

                        // reset all modules
                        Antistall.ResetParameters();
                        CruiseControl.ResetParameters();
                        Drivetrain.ResetParameters();
                        Transmission.ResetParameters();
                        TractionControl.ResetParameters();
                        Speedlimiter.ResetParameters();

                        CarProfile = new Profiles(Data.Active.Application, Data.Telemetry.Car);
                        LoadNextProfile(10000);
                    };

                // TODO: Temporary..
                Data.AppActive += (s, e) => { CameraHorizon.CameraHackEnabled = Data.Active.Application == "TestDrive2"; };

                if (deskCtrl == null && g25Wheel == null && ps4Ctrl == null)
                {
                    MessageBox.Show("No controllers found");
                    return false;
                }

                // Modules
                Antistall = new Antistall();
                ACC = new ACC();
                CruiseControl = new CruiseControl();
                Drivetrain = new GenericDrivetrain();
                Transmission = new Transmission();
                TractionControl = new TractionControl();
                ProfileSwitcher = new ProfileSwitcher();
                Speedlimiter = new Speedlimiter();
                LaunchControl = new LaunchControl();
                DrivetrainCalibrator = new DrivetrainCalibrator();
                TransmissionCalibrator = new TransmissionCalibrator();
                LaneAssistance = new LaneAssistance();
                VariableSpeedControl = new VariableSpeedTransmission();
                CameraHorizon = new CameraHorizon();

                // Controls
                Controls = new ControlChain();

                Data.Run();
                return true;
            }
            return false;
        }

        public static void Start()
        {
            var isNowRunning = Running;
            if (requiresSetup) isNowRunning = Setup();
            //
            if (!Running)
            {
                Data.DataReceived += Tick;
                Running = isNowRunning;
            }
        }

        public static void Stop()
        {
            if (Running)
            {
                Data.DataReceived -= Tick;
                Running = false;
            }
        }

        public static void Store(IEnumerable<IniValueObject> settings, string f)
        {
            StringBuilder export = new StringBuilder();
            // Groups
            var groups = settings.Select(x => x.Group).Distinct();

            foreach (var group in groups)
            {
                export.AppendLine("[" + group + "]");

                foreach (var setting in settings.Where(x => x.Group == group))
                {
                    export.AppendLine(setting.Key + "=" + setting.RawValue);
                }

                export.AppendLine(" ");
            }
            try
            {
                File.WriteAllText(f, export.ToString());
                Debug.WriteLine("Exported settings to " + f);
            }
            catch
            { }
        }

        public static void Tick(object sender, EventArgs e)
        {
            Controls.Tick(Data.Active);
        }
    }
}