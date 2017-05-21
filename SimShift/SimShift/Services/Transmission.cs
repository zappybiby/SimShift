using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Dialogs;
using SimShift.Entities;
using SimShift.Utils;

namespace SimShift.Services
{
    /// <summary>
    ///     Automatic shifter for vehicles. Support 1 reverse and up to 24 reverse gears using 6-speed 4-range gearbox.
    ///     Uses pre-calculated 2-dimensional shifter table that maps speed and throttle position to gear.
    /// </summary>
    public class Transmission : IControlChainObj, IConfigurable
    {
        public string ActiveShiftPatternStr;

        public ShifterTableConfiguration configuration;

        public int Gears = 6;

        // TODO: Move to car object
        public int RangeSize = 6;

        public int ShiftFrame = 0;

        public Dictionary<string, ShiftPattern> ShiftPatterns = new Dictionary<string, ShiftPattern>();

        public float StaticMass = 0;

        private int powerShiftStage = 0;

        private int powerShiftTimer = 0;

        private int shiftRetry = 0;

        private double transmissionThrottle;

        public Transmission()
        {
            this.configuration = new ShifterTableConfiguration(ShifterTableConfigurationDefault.PeakRpm, Main.Drivetrain, 20, 0);

            this.LoadShiftPattern("up_1thr", "normal");
            this.LoadShiftPattern("up_0thr", "normal");
            this.LoadShiftPattern("down_1thr", "normal");
            this.LoadShiftPattern("down_0thr", "normal");

            // Add power shift pattern
            var powerShiftPattern = new ShiftPattern();
            powerShiftPattern.Frames.Add(new ShiftPatternFrame(0, 1, false, false, true));
            powerShiftPattern.Frames.Add(new ShiftPatternFrame(0, 1, false, false, false));
            powerShiftPattern.Frames.Add(new ShiftPatternFrame(0, 1, false, false, true));
            powerShiftPattern.Frames.Add(new ShiftPatternFrame(1, 0.5, true, false, true));
            powerShiftPattern.Frames.Add(new ShiftPatternFrame(1, 0.5, true, false, true));
            this.ShiftPatterns.Add("PowerShift", powerShiftPattern);

            // Initialize all shfiting stuff.
            this.Shift(0, 1, "up_1thr");
            this.Enabled = true;
            this.IsShifting = false;
        }

        public static bool InReverse { get; set; }

        public IEnumerable<string> AcceptsConfigs => new[] { "ShiftCurve" };

        public bool Active => this.IsShifting;

        public ShiftPattern ActiveShiftPattern => this.ShiftPatterns[this.ActiveShiftPatternStr];

        public bool Enabled { get; set; }

        public int GameGear { get; private set; }

        public string GeneratedShiftTable { get; private set; }

        public bool GetHomeMode { get; set; }

        public bool IsShifting { get; private set; }

        public bool KickdownCooldown
        {
            get => this.KickdownTime > DateTime.Now;

            set => this.KickdownTime = DateTime.MinValue;
        }

        public bool KickdownEnable { get; private set; }

        public double KickdownLockedSpeed { get; private set; }

        public double KickdownPowerReset { get; private set; }

        public double KickdownRpmReset { get; private set; }

        public double KickdownSpeedReset { get; private set; }

        public DateTime KickdownTime { get; private set; }

        public double KickdownTimeout { get; private set; }

        public bool OverruleShifts { get; set; }

        public DateTime RangeButtonFreeze1Untill { get; private set; }

        public DateTime RangeButtonFreeze2Untill { get; private set; }

        public int RangeButtonSelectPhase
        {
            get
            {
                if (this.RangeButtonFreeze1Untill > DateTime.Now)
                {
                    return 1; // phase 1
                }

                if (this.RangeButtonFreeze2Untill > DateTime.Now)
                {
                    return 2; // phase 2
                }

                return 0; // phase 0
            }
        }

        public int ShiftCtrlNewGear { get; private set; }

        public int ShiftCtrlNewRange { get; private set; }

        public int ShiftCtrlOldGear { get; private set; }

        public int ShiftCtrlOldRange { get; private set; }

        public int ShiftDeadSpeed { get; private set; }

        public int ShiftDeadTime { get; private set; }

        public int ShifterGear => this.ShifterNewGear;

        public int ShifterNewGear => this.ShiftCtrlNewGear + this.ShiftCtrlNewRange * this.RangeSize;

        public int ShifterOldGear => this.ShiftCtrlOldGear + this.ShiftCtrlOldRange * this.RangeSize;

        public IEnumerable<string> SimulatorsBan => new string[0];

        public IEnumerable<string> SimulatorsOnly => new string[0];

        public int speedHoldoff { get; private set; }

        public DateTime TransmissionFreezeUntill { get; private set; }

        public bool TransmissionFrozen => this.TransmissionFreezeUntill > DateTime.Now;

        protected bool EnableSportShiftdown { get; set; }

        protected bool PowerShift { get; set; }

        protected double ThrottleScale { get; set; }

        public void ApplyParameter(IniValueObject obj)
        {
            switch (obj.Key)
            {
                case "ShiftDeadSpeed":
                    this.ShiftDeadSpeed = obj.ReadAsInteger();
                    break;
                case "ShiftDeadTime":
                    this.ShiftDeadTime = obj.ReadAsInteger();
                    break;

                case "GenerateSpeedHoldoff":
                    this.speedHoldoff = obj.ReadAsInteger();
                    break;

                case "EnableSportShiftdown":
                    this.EnableSportShiftdown = obj.ReadAsInteger() == 1;
                    break;

                case "KickdownEnable":
                    this.KickdownEnable = obj.ReadAsString() == "1";
                    break;
                case "KickdownTimeout":
                    this.KickdownTimeout = obj.ReadAsDouble();
                    break;
                case "KickdownSpeedReset":
                    this.KickdownSpeedReset = obj.ReadAsDouble();
                    break;
                case "KickdownPowerReset":
                    this.KickdownPowerReset = obj.ReadAsDouble();
                    break;
                case "KickdownRpmReset":
                    this.KickdownRpmReset = obj.ReadAsDouble();
                    break;

                case "PowerShift":
                    this.PowerShift = obj.ReadAsInteger() == 1;
                    break;

                case "Generate":
                    var def = ShifterTableConfigurationDefault.PeakRpm;
                    this.GeneratedShiftTable = obj.ReadAsString();
                    switch (this.GeneratedShiftTable)
                    {
                        case "Economy":
                            def = ShifterTableConfigurationDefault.Economy;
                            break;
                        case "Efficiency":
                            def = ShifterTableConfigurationDefault.Efficiency;
                            break;
                        case "Efficiency2":
                            def = ShifterTableConfigurationDefault.PowerEfficiency;
                            break;
                        case "Opa":
                            def = ShifterTableConfigurationDefault.AlsEenOpa;
                            break;
                        case "PeakRpm":
                            def = ShifterTableConfigurationDefault.PeakRpm;
                            break;
                        case "Performance":
                            def = ShifterTableConfigurationDefault.Performance;
                            break;
                        case "Henk":
                            def = ShifterTableConfigurationDefault.Henk;
                            break;
                    }

                    this.configuration = new ShifterTableConfiguration(def, Main.Drivetrain, this.speedHoldoff, this.StaticMass);
                    break;
            }
        }

        public IEnumerable<IniValueObject> ExportParameters()
        {
            List<IniValueObject> obj = new List<IniValueObject>();
            obj.Add(new IniValueObject(this.AcceptsConfigs, "ShiftDeadSpeed", this.ShiftDeadSpeed.ToString()));
            obj.Add(new IniValueObject(this.AcceptsConfigs, "ShiftDeadTime", this.ShiftDeadTime.ToString()));
            obj.Add(new IniValueObject(this.AcceptsConfigs, "GenerateSpeedHoldoff", this.speedHoldoff.ToString()));
            obj.Add(new IniValueObject(this.AcceptsConfigs, "Generate", this.GeneratedShiftTable));

            // TODO: Tables not supported yet.
            return obj;
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Throttle:

                    this.transmissionThrottle = val;
                    if (this.transmissionThrottle > 1)
                    {
                        this.transmissionThrottle = 1;
                    }

                    if (this.transmissionThrottle < 0)
                    {
                        this.transmissionThrottle = 0;
                    }

                    lock (this.ActiveShiftPattern)
                    {
                        if (!this.Enabled)
                        {
                            return val * this.ThrottleScale;
                        }

                        if (this.shiftRetry > 0)
                        {
                            return 0;
                        }

                        if (this.ShiftFrame >= this.ActiveShiftPattern.Count)
                        {
                            return val * this.ThrottleScale;
                        }

                        var candidateValue = this.IsShifting ? this.ActiveShiftPattern.Frames[this.ShiftFrame].Throttle * val : val;
                        candidateValue *= this.ThrottleScale;

                        return candidateValue;
                    }

                case JoyControls.Clutch:
                    lock (this.ActiveShiftPattern)
                    {
                        if (this.ShiftFrame >= this.ActiveShiftPattern.Count)
                        {
                            return val;
                        }

                        return this.ActiveShiftPattern.Frames[this.ShiftFrame].Clutch;
                    }

                default: return val;
            }
        }

        public bool GetButton(JoyControls c, bool val)
        {
            switch (c)
            {
                case JoyControls.Gear1: return this.GetShiftButton(1);
                case JoyControls.Gear2: return this.GetShiftButton(2);
                case JoyControls.Gear3: return this.GetShiftButton(3);
                case JoyControls.Gear4: return this.GetShiftButton(4);
                case JoyControls.Gear5: return this.GetShiftButton(5);
                case JoyControls.Gear6: return this.GetShiftButton(6);
                case JoyControls.Gear7: return this.GetShiftButton(7);
                case JoyControls.Gear8: return this.GetShiftButton(8);
                case JoyControls.GearR: return this.GetShiftButton(-1);
                case JoyControls.GearRange1: return this.GetRangeButton(1);
                case JoyControls.GearRange2: return this.GetRangeButton(2);

                default: return val;
            }
        }

        public void LoadShiftPatterns(List<ConfigurableShiftPattern> patterns)
        {
            this.ShiftPatterns.Clear();

            foreach (var p in patterns)
            {
                this.LoadShiftPattern(p.Region, p.File);
            }
        }

        public void RecalcTable()
        {
            this.configuration = new ShifterTableConfiguration(this.configuration.Mode, Main.Drivetrain, this.configuration.SpdPerGear, this.configuration.Mass);
        }

        public bool Requires(JoyControls c)
        {
            switch (c)
            {
                // Only required when changing
                case JoyControls.Throttle: return this.Enabled || true;

                case JoyControls.Clutch: return this.Enabled && this.IsShifting;

                // All gears.
                case JoyControls.GearR:
                case JoyControls.Gear1:
                case JoyControls.Gear2:
                case JoyControls.Gear3:
                case JoyControls.Gear4:
                case JoyControls.Gear5:
                case JoyControls.Gear6: return this.Enabled;

                case JoyControls.GearRange2:
                case JoyControls.GearRange1: return this.Enabled && Main.Data.Active.TransmissionSupportsRanges;

                case JoyControls.Gear7:
                case JoyControls.Gear8: return this.Enabled && !Main.Data.Active.TransmissionSupportsRanges;

                case JoyControls.GearUp:
                case JoyControls.GearDown: return this.Enabled;

                default: return false;
            }
        }

        public void ResetParameters()
        {
            this.configuration = new ShifterTableConfiguration(ShifterTableConfigurationDefault.PeakRpm, Main.Drivetrain, 10, 0);

            if (Main.Data.Active.Application == "TestDrive2")
            {
                this.LoadShiftPattern("up_1thr", "fast");
            }
            else
            {
                this.LoadShiftPattern("up_1thr", "normal");
            }

            this.EnableSportShiftdown = false;
            this.PowerShift = false;
        }

        public void Shift(int fromGear, int toGear, string style)
        {
            if (this.IsShifting)
            {
                return;
            }

            if (this.EnableSportShiftdown && Main.GetAxisIn(JoyControls.Throttle) < 0.2)
            {
                this.KickdownTime = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, (int) this.KickdownTimeout / 10));
            }
            else
            {
                this.KickdownTime = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, (int) this.KickdownTimeout));
            }

            if (this.PowerShift)
            {
                style = "PowerShift";
            }

            if (this.ShiftPatterns.ContainsKey(style))
            {
                this.ActiveShiftPatternStr = style;
            }
            else
            {
                this.ActiveShiftPatternStr = this.ShiftPatterns.Keys.FirstOrDefault();
            }

            // Copy old control to new control values
            this.ShiftCtrlOldGear = fromGear;
            if (this.ShiftCtrlOldGear == -1)
            {
                this.ShiftCtrlOldRange = 0;
            }
            else if (this.ShiftCtrlOldGear == 0)
            {
                this.ShiftCtrlOldRange = 0;
            }
            else if (this.ShiftCtrlOldGear >= 1 && this.ShiftCtrlOldGear <= this.RangeSize)
            {
                this.ShiftCtrlOldRange = 0;
            }
            else if (this.ShiftCtrlOldGear >= this.RangeSize + 1 && this.ShiftCtrlOldGear <= 2 * this.RangeSize)
            {
                this.ShiftCtrlOldRange = 1;
            }
            else if (this.ShiftCtrlOldGear >= 2 * this.RangeSize + 1 && this.ShiftCtrlOldGear <= 3 * this.RangeSize)
            {
                this.ShiftCtrlOldRange = 2;
            }

            this.ShiftCtrlOldGear -= this.ShiftCtrlOldRange * this.RangeSize;

            // Determine new range
            if (toGear == -1)
            {
                this.ShiftCtrlNewGear = -1;
                this.ShiftCtrlNewRange = 0;
            }
            else if (toGear == 0)
            {
                this.ShiftCtrlNewGear = 0;
                this.ShiftCtrlNewRange = 0;
            }
            else if (toGear >= 1 && toGear <= this.RangeSize)
            {
                this.ShiftCtrlNewGear = toGear;
                this.ShiftCtrlNewRange = 0;
            }
            else if (toGear >= this.RangeSize + 1 && toGear <= this.RangeSize * 2)
            {
                this.ShiftCtrlNewGear = toGear - this.RangeSize;
                this.ShiftCtrlNewRange = 1;
            }
            else if (toGear >= this.RangeSize * 2 + 1 && toGear <= this.RangeSize * 3)
            {
                this.ShiftCtrlNewGear = toGear - this.RangeSize * 2;
                this.ShiftCtrlNewRange = 2;
            }

            this.ShiftFrame = 0;
            this.IsShifting = true;
            this.powerShiftStage = 0;
        }

        public void TickControls()
        {
            if (this.IsShifting)
            {
                lock (this.ActiveShiftPattern)
                {
                    if (this.PowerShift)
                    {
                        var stage = this.powerShiftStage;
                        if (this.powerShiftStage == this.ActiveShiftPattern.Count - 2)
                        {
                            if (Main.Data.Telemetry.Gear == this.ShifterNewGear)
                            {
                                this.powerShiftStage++;
                            }
                        }
                        else if (this.powerShiftStage == this.ActiveShiftPattern.Count - 1)
                        {
                            this.IsShifting = false;
                            this.shiftRetry = 0;
                            this.TransmissionFreezeUntill = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, this.ShiftDeadTime + 50 * this.ShiftCtrlNewGear));
                        }
                        else
                        {
                            this.powerShiftStage++;
                        }

                        if (this.powerShiftStage != stage)
                        {
                            this.powerShiftTimer = 0;
                        }
                        else
                        {
                            this.powerShiftTimer++;
                            if (this.powerShiftTimer >= 100)
                            {
                                this.powerShiftTimer = 0;

                                // So we are shifting, check lagging by 1, and new gear doesn't work
                                // We re-shfit
                                var tmp = this.ShiftFrame;
                                this.IsShifting = false;
                                if (this.shiftRetry >= 7)
                                {
                                    this.GameGear = this.shiftRetry;
                                }

                                this.GameGear = this.GameGear % 18;
                                if (this.shiftRetry > 18 + 8)
                                {
                                    this.IsShifting = false;
                                    return;
                                }

                                this.Shift(this.GameGear, this.ShifterNewGear, "PowerShift");
                                this.ShiftFrame = 0;
                                this.shiftRetry++;

                                if (this.ShiftCtrlNewRange != this.ShiftCtrlOldRange)
                                {
                                    this.ShiftFrame = 0;
                                    this.RangeButtonFreeze1Untill = DateTime.Now;
                                    this.RangeButtonFreeze2Untill = DateTime.Now;
                                }
                            }
                        }

                        this.ShiftFrame = this.powerShiftStage;
                    }
                    else
                    {
                        this.ShiftFrame++;
                        if (this.ShiftFrame > this.ActiveShiftPattern.Count)
                        {
                            this.ShiftFrame = 0;
                        }

                        if (this.ShiftFrame >= this.ActiveShiftPattern.Count)
                        {
                            if (this.shiftRetry < 20 && this.ShiftFrame > 4 && this.GameGear != this.ShifterNewGear)
                            {
                                // So we are shifting, check lagging by 1, and new gear doesn't work
                                // We re-shfit
                                var tmp = this.ShiftFrame;
                                this.IsShifting = false;
                                if (this.shiftRetry >= 7)
                                {
                                    this.GameGear = this.shiftRetry;
                                }

                                this.Shift(this.GameGear, this.ShifterNewGear, "up_1thr");
                                this.ShiftFrame = 0;
                                this.shiftRetry++;

                                if (this.ShiftCtrlNewRange != this.ShiftCtrlOldRange)
                                {
                                    this.ShiftFrame = 0;
                                    this.RangeButtonFreeze1Untill = DateTime.Now;
                                    this.RangeButtonFreeze2Untill = DateTime.Now;
                                }
                            }
                            else
                            {
                                this.IsShifting = false;
                                this.shiftRetry = 0;
                                this.TransmissionFreezeUntill = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, this.ShiftDeadTime + 20 * this.ShiftCtrlNewGear));
                            }
                        }
                    }
                }
            }
        }

        public void TickTelemetry(IDataMiner data)
        {
            int idealGear = data.Telemetry.Gear;

            if (data.TransmissionSupportsRanges)
            {
                this.RangeSize = 6;
            }
            else
            {
                this.RangeSize = 8;
            }

            // TODO: Add generic telemetry object
            this.GameGear = data.Telemetry.Gear;
            if (this.IsShifting)
            {
                return;
            }

            if (this.TransmissionFrozen && !this.GetHomeMode)
            {
                return;
            }

            if (this.OverruleShifts)
            {
                return;
            }

            this.shiftRetry = 0;

            if (this.GetHomeMode)
            {
                if (idealGear < 1)
                {
                    idealGear = 1;
                }

                var lowRpm = Main.Drivetrain.StallRpm * 1.5;
                var highRpm = Main.Drivetrain.StallRpm * 3;

                if (data.Telemetry.EngineRpm < lowRpm && idealGear > 1)
                {
                    idealGear--;
                }

                if (data.Telemetry.EngineRpm > highRpm && idealGear < Main.Drivetrain.Gears)
                {
                    idealGear++;
                }
            }
            else
            {
                if (this.EnableSportShiftdown)
                {
                    this.transmissionThrottle = Math.Max(Main.GetAxisIn(JoyControls.Brake) * 8, this.transmissionThrottle);
                }

                this.transmissionThrottle = Math.Min(1, Math.Max(0, this.transmissionThrottle));
                var lookupResult = this.configuration.Lookup(data.Telemetry.Speed * 3.6, this.transmissionThrottle);
                idealGear = lookupResult.Gear;
                this.ThrottleScale = this.GetHomeMode ? 1 : lookupResult.ThrottleScale;

                if (Main.Data.Active.Application == "eurotrucks2")
                {
                    var ets2Miner = (Ets2DataMiner) data;
                    var maxGears = (int) Math.Round(Main.Drivetrain.Gears * (1 - ets2Miner.MyTelemetry.Damage.WearTransmission));

                    if (idealGear >= maxGears)
                    {
                        idealGear = maxGears;
                    }
                }

                if (data.Telemetry.Gear == 0 && this.ShiftCtrlNewGear != 0)
                {
                    Debug.WriteLine("Timeout");
                    this.ShiftCtrlNewGear = 0;
                    this.TransmissionFreezeUntill = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 250));
                    return;
                }
            }

            if (InReverse)
            {
                if (this.GameGear != -1 && Math.Abs(data.Telemetry.Speed) < 1)
                {
                    Debug.WriteLine("Shift from " + data.Telemetry.Gear + " to  " + idealGear);
                    this.Shift(data.Telemetry.Gear, -1, "up_1thr");
                }

                return;
            }

            // Kickdown?
            // What is basically does, is making this very-anxious gearbox tone down a bit in it's aggressive shift pattern.
            // With kickdown we specify 2 rules to make the gear "stick" longer with varying throttle positions.
            if (this.KickdownEnable)
            {
                if (this.KickdownCooldown)
                {
                    // We're on cooldown. Check if power/speed/rpm is able to reset this
                    // The scheme is as follow:
                    // if (delta of [current speed] and [speed of last shift] ) > speedReset: then reset cooldown
                    // if ([generated power this gear] / [generated power new gear]) > 1+powerReset: then reset cooldown
                    // if (currentRpm < [rpm * stationaryValue]) : then reset cooldown

                    // This makes sure the gearbox shifts down if on very low revs and almost about to stall.

                    // Note: only for speeding up
                    if ((data.Telemetry.Speed - this.KickdownLockedSpeed) > this.KickdownSpeedReset)
                    {
                        Debug.WriteLine("[Kickdown] Reset on overspeed");
                        this.KickdownCooldown = false;
                    }

                    var maxPwr = Main.Drivetrain.CalculateMaxPower();

                    var engineRpmCurrentGear = Main.Drivetrain.CalculateRpmForSpeed(this.ShifterOldGear - 1, data.Telemetry.Speed);
                    var pwrCurrentGear = Main.Drivetrain.CalculatePower(engineRpmCurrentGear, data.Telemetry.Throttle);

                    var engineRpmNewGear = Main.Drivetrain.CalculateRpmForSpeed(idealGear - 1, data.Telemetry.Speed);
                    var pwrNewGear = Main.Drivetrain.CalculatePower(engineRpmNewGear, data.Telemetry.Throttle);

                    // Debug.WriteLine("N"+pwrCurrentGear.ToString("000") + " I:" + pwrNewGear.ToString("000"));
                    // This makes sure the gearbox shifts down if on low revs and the user is requesting power from the engine
                    if (pwrNewGear / pwrCurrentGear > 1 + this.KickdownPowerReset && pwrNewGear / maxPwr > this.KickdownPowerReset)
                    {
                        Debug.WriteLine("[Kickdown] Reset on power / " + pwrCurrentGear + " / " + pwrNewGear);
                        this.KickdownCooldown = false;
                    }

                    // This makes sure the gearbox shifts up in decent time when reaching end of gears
                    if (Main.Drivetrain.StallRpm * this.KickdownRpmReset > data.Telemetry.EngineRpm)
                    {
                        Debug.WriteLine("[Kickdown] Reset on stalling RPM");
                        this.KickdownCooldown = true;
                    }
                }
                else
                { }
            }

            if (idealGear != data.Telemetry.Gear)
            {
                if (this.KickdownEnable && this.KickdownCooldown && !this.GetHomeMode)
                {
                    return;
                }

                this.KickdownLockedSpeed = Main.Data.Telemetry.Speed;
                var upShift = idealGear > data.Telemetry.Gear;
                var fullThrottle = data.Telemetry.Throttle > 0.6;

                var shiftStyle = (upShift ? "up" : "down") + "_" + (fullThrottle ? "1" : "0") + "thr";

                Debug.WriteLine("Shift from " + data.Telemetry.Gear + " to  " + idealGear);
                this.Shift(data.Telemetry.Gear, idealGear, shiftStyle);
            }
        }

        private bool GetRangeButton(int r)
        {
            if (Main.Data.Active == null || !Main.Data.Active.TransmissionSupportsRanges)
            {
                return false;
            }

            if (this.IsShifting && this.ShiftCtrlNewRange != this.ShiftCtrlOldRange)
            {
                // More debug values
                // Going to range 1 when old gear was outside range 1,
                // and new is in range 1.
                var engagingToRange1 = this.ShifterOldGear >= 7 && this.ShifterNewGear < 7;

                // Range 2 is engaged when the old gear was range 1 or 3, and the new one is range 2.
                var engagingToRange2 = (this.ShifterOldGear < 7 || this.ShifterOldGear > 12) && this.ShifterNewGear >= 7 && this.ShifterNewGear <= 12;

                // Range 2 is engaged when the old gear was range 1 or 2, and the new one is range 3.
                var engagingToRange3 = this.ShifterOldGear < 13 && this.ShifterNewGear >= 13;

                var engageR1Status = false;
                var engageR2Status = false;

                if (this.ShiftCtrlOldRange == 0)
                {
                    if (this.ShiftCtrlNewRange == 1)
                    {
                        engageR1Status = true;
                    }
                    else
                    {
                        engageR2Status = true;
                    }
                }
                else if (this.ShiftCtrlOldRange == 1)
                {
                    if (this.ShiftCtrlNewRange == 0)
                    {
                        engageR1Status = true;
                    }
                    else
                    {
                        engageR1Status = true;
                        engageR2Status = true;
                    }
                }
                else if (this.ShiftCtrlOldRange == 2)
                {
                    if (this.ShiftCtrlNewRange == 0)
                    {
                        engageR2Status = true;
                    }
                    else
                    {
                        engageR1Status = true;
                        engageR2Status = true;
                    }
                }

                switch (this.RangeButtonSelectPhase)
                {
                    // On
                    case 1:
                        if (r == 1)
                        {
                            return engageR1Status;
                        }

                        if (r == 2)
                        {
                            return engageR2Status;
                        }

                        return false;

                    // Off
                    case 2: return false;

                    // Evaluate and set phase 1(on) / phase 2 (off) timings
                    default:

                        Debug.WriteLine("Shift " + this.ShifterOldGear + "(" + this.ShiftCtrlOldRange + ") to " + this.ShifterNewGear + "(" + this.ShiftCtrlNewRange + ")");
                        Debug.WriteLine("R1: " + engageR1Status + " / R2: " + engageR2Status);
                        if (r == 1 && !engageR1Status)
                        {
                            return false;
                        }

                        if (r == 2 && !engageR2Status)
                        {
                            return false;
                        }

                        this.RangeButtonFreeze1Untill = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 100)); // 150ms ON
                        this.RangeButtonFreeze2Untill = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 1500)); // 150ms OFF

                        Debug.WriteLine("Yes");
                        return true;
                }
            }

            return false;
        }

        private bool GetShiftButton(int b)
        {
            if (this.IsShifting)
            {
                lock (this.ActiveShiftPattern)
                {
                    if (this.ShiftFrame >= this.ActiveShiftPattern.Count)
                    {
                        return false;
                    }

                    if (this.ActiveShiftPattern.Frames[this.ShiftFrame].UseOldGear)
                    {
                        return b == this.ShiftCtrlOldGear;
                    }

                    if (this.ActiveShiftPattern.Frames[this.ShiftFrame].UseNewGear)
                    {
                        return b == this.ShiftCtrlNewGear;
                    }

                    return false;
                }
            }

            return b == this.ShiftCtrlNewGear;
        }

        private void LoadShiftPattern(string pattern, string file)
        {
            // Add pattern if not existing.
            if (!this.ShiftPatterns.ContainsKey(pattern))
            {
                this.ShiftPatterns.Add(pattern, new ShiftPattern());
            }

            // Load configuration file
            Main.Load(this.ShiftPatterns[pattern], "Settings/ShiftPattern/" + file + ".ini");
        }
    }
}