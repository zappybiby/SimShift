using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Entities;

namespace SimShift.Services
{
    public class VariableSpeedTransmission : IControlChainObj
    {
        public static double reqpower = 0;

        public bool fast = false;

        private DateTime lastShifterTick = DateTime.Now;

        private double lastSpeedError = 0;

        private int shiftingRetry = 0;

        private double staticThrError = 0;

        private double UserThrottle = 0;

        private double variableBrake = 0.0;

        private double variableThrottle = 0.0;

        private bool wasEfficiency = false;

        public VariableSpeedTransmission()
        {
            this.Enabled = true;
        }

        public bool Active { get; private set; }

        public bool Efficiency { get; set; }

        public bool Enabled { get; set; }

        public int Gear { get; set; }

        public int LastSeenRange { get; set; }

        public bool Reverse { get; set; }

        public double SetSpeed { get; set; }

        public IEnumerable<string> SimulatorsBan => new string[0];

        public IEnumerable<string> SimulatorsOnly => new string[0];

        private ShiftPhase ShiftingPhase { get; set; }

        public double GetAxis(JoyControls c, double val)
        {
            if (c == JoyControls.Brake)
            {
                return val + this.variableBrake;
            }

            if (c == JoyControls.Throttle)
            {
                this.UserThrottle = val * 0.25f + this.UserThrottle * 0.75f;
                switch (this.ShiftingPhase)
                {
                    case ShiftPhase.OffThrottle:
                    case ShiftPhase.EngageGear:
                    case ShiftPhase.EngageRange:
                    case ShiftPhase.Evaluate:
                    case ShiftPhase.OnClutch:
                    case ShiftPhase.OffClutch: return 0;

                    default: return this.variableThrottle;
                }
            }

            if (c == JoyControls.Clutch)
            {
                switch (this.ShiftingPhase)
                {
                    case ShiftPhase.EngageGear:
                    case ShiftPhase.EngageRange:
                    case ShiftPhase.Evaluate:
                    case ShiftPhase.OnClutch: return 1;

                    default: return val;
                }
            }

            return val;
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

        public bool Requires(JoyControls c)
        {
            switch (c)
            {
                case JoyControls.Clutch:
                case JoyControls.Brake:
                case JoyControls.Throttle: return this.Enabled;

                // All gears.
                case JoyControls.GearR:
                case JoyControls.Gear1:
                case JoyControls.Gear2:
                case JoyControls.Gear3:
                case JoyControls.Gear4:
                case JoyControls.Gear5:
                case JoyControls.Gear6: return this.Enabled;

                case JoyControls.GearRange2:
                case JoyControls.GearRange1: return this.Enabled;
            }
            return false;
        }

        public void TickControls()
        { }

        public void TickTelemetry(IDataMiner data)
        {
            bool copyTargetThr = false;

            /** VARIABLE SPEED CONTROL **/
            var actualSpeed = data.Telemetry.Speed * 3.6;

            double thrP, thrI;
            if (data.Telemetry.Gear == -1)
            {
                this.SetSpeed = Main.GetAxisIn(JoyControls.VstLever) * 50;
                actualSpeed *= -1;

                thrP = 0.1;
                thrI = 0;
            }
            else
            {
                this.SetSpeed = Main.GetAxisIn(JoyControls.VstLever) * (this.fast ? 200 : 100);

                thrP = 0.015 + 0.15 * actualSpeed / 120;
                thrI = 0.02 - 0.015 * actualSpeed / 120;
                if (this.Efficiency)
                {
                    thrP *= 0.5;
                    thrI = 0.00025;
                }

                if (this.Efficiency != this.wasEfficiency)
                {
                    copyTargetThr = true;
                    this.staticThrError = this.variableThrottle - 2 * thrP * this.lastSpeedError;
                }

                this.wasEfficiency = this.Efficiency;
            }

            var speedErrorThr = this.SetSpeed - actualSpeed;
            this.staticThrError += speedErrorThr * thrI;
            if (this.staticThrError > 0.8)
            {
                this.staticThrError = 0.8;
            }

            if (this.staticThrError < -0.8)
            {
                this.staticThrError = -0.8;
            }

            this.lastSpeedError = speedErrorThr;
            var oldThr = this.variableThrottle;
            if (copyTargetThr)
            {
                this.variableThrottle = thrP * speedErrorThr;
                this.variableThrottle *= this.UserThrottle;

                // Theoratical value required to copy last throttle
                this.staticThrError = (1 - this.variableThrottle) / this.UserThrottle;

                // Deduce at low speeds
                var deductor = actualSpeed / 50;
                if (deductor > 1)
                {
                    deductor = 1;
                }

                if (deductor < 0.01)
                {
                    deductor = 0.01;
                }

                this.staticThrError *= deductor;

                // Add it.
                this.variableThrottle += this.staticThrError;
            }
            else
            {
                this.variableThrottle = thrP * speedErrorThr + this.staticThrError;
                this.variableThrottle *= this.UserThrottle;
            }

            if (this.variableThrottle > 1)
            {
                this.variableThrottle = 1;
            }

            if (this.variableThrottle < 0)
            {
                this.variableThrottle = 0;
            }

            var speedErrorBr = (actualSpeed - 3) - this.SetSpeed;
            if (speedErrorBr < 0)
            {
                speedErrorBr = 0;
            }

            if (actualSpeed < 50)
            {
                speedErrorBr *= (50 - actualSpeed) / 15 + 1;
            }

            this.variableBrake = 0.01 * speedErrorBr * speedErrorBr;
            if (this.variableBrake > 0.2)
            {
                this.variableBrake = 0.2;
            }

            if (this.variableBrake < 0)
            {
                this.variableBrake = 0;
            }

            if (this.variableBrake > 0.01)
            {
                this.variableThrottle = 0;
            }

            /** TRANSMISSION **/
            if (Main.Data.Active.Application == "eurotrucks2")
            {
                var ets2 = (Ets2DataMiner) Main.Data.Active;
                if (ets2.MyTelemetry.Job.TrailerAttached == false && !this.fast)
                {
                    this.Efficiency = true;
                }
                else
                {
                    if (this.Efficiency)
                    {
                        if ((this.SetSpeed - actualSpeed) > 5)
                        {
                            this.Efficiency = false;
                        }
                    }
                    else
                    {
                        if ((this.SetSpeed - actualSpeed) < 2)
                        {
                            this.Efficiency = true;
                        }
                    }
                }
            }

            // Efficiency = (SetSpeed - actualSpeed) < 5;
            if (DateTime.Now.Subtract(this.lastShifterTick).TotalMilliseconds > 40)
            {
                this.Active = this.ShiftingPhase == ShiftPhase.None ? false : true;
                this.lastShifterTick = DateTime.Now;
                switch (this.ShiftingPhase)
                {
                    case ShiftPhase.WaitButton:
                        if (Main.GetButtonIn(JoyControls.GearUp) == false && Main.GetButtonIn(JoyControls.GearDown) == false)
                        {
                            this.ShiftingPhase = ShiftPhase.None;
                        }

                        break;

                    case ShiftPhase.None:

                        // Reverse pressed?
                        if (Main.GetButtonIn(JoyControls.VstChange))
                        {
                            this.fast = !this.fast;
                            this.ShiftingPhase = ShiftPhase.WaitButton;
                        }

                        if (Main.GetButtonIn(JoyControls.GearDown))
                        {
                            this.Reverse = !this.Reverse;
                            this.ShiftingPhase = ShiftPhase.WaitButton;
                        }

                        if (this.Reverse)
                        {
                            if (data.Telemetry.Gear != -1)
                            {
                                this.Gear = -1;
                                this.ShiftingPhase = ShiftPhase.OffThrottle;
                            }
                        }
                        else
                        {
                            // Do nothing
                            if (data.Telemetry.Gear != this.Gear || this.Gear == 0)
                            {
                                if (this.Gear == 0)
                                {
                                    this.Gear++;
                                }

                                if (this.Gear == -1)
                                {
                                    this.Gear = 1;
                                }

                                this.ShiftingPhase = ShiftPhase.OffThrottle;
                            }
                            else
                            {
                                var curPower = Main.Drivetrain.CalculatePower(data.Telemetry.EngineRpm, data.Telemetry.Throttle);
                                if (curPower < 1)
                                {
                                    curPower = 1;
                                }

                                var curEfficiency = Main.Drivetrain.CalculateFuelConsumption(data.Telemetry.EngineRpm, data.Telemetry.Throttle) / curPower;
                                var reqPower = curPower * (this.variableThrottle - 0.5) * 2;
                                if (reqPower < 25)
                                {
                                    reqPower = 25;
                                }

                                if (reqPower > 0.5 * Main.Drivetrain.CalculateMaxPower())
                                {
                                    reqPower = 0.5 * Main.Drivetrain.CalculateMaxPower();
                                }

                                reqpower = reqPower;
                                int maxgears = Main.Drivetrain.Gears;
                                var calcEfficiency = this.Efficiency ? double.MaxValue : 0;
                                var calcEfficiencyGear = -1;
                                var calcThrottle = this.variableThrottle;
                                var calcPower = curPower;

                                var allStalled = true;
                                for (int k = 0; k < maxgears; k++)
                                {
                                    if (maxgears >= 12 && (k == 5))
                                    {
                                        continue;
                                    }

                                    if (maxgears >= 10 && (k == 1 || k == 3))
                                    {
                                        continue;
                                    }

                                    if (!this.Efficiency && k < 3)
                                    {
                                        continue;
                                    }

                                    // Always pick best efficient gear with power requirement met
                                    var rpm = Main.Drivetrain.CalculateRpmForSpeed(k, data.Telemetry.Speed);
                                    var orpm = Main.Drivetrain.CalculateRpmForSpeed(k, data.Telemetry.Speed);
                                    var estimatedPower = Main.Drivetrain.CalculatePower(rpm, this.variableThrottle);

                                    // RPM increase linear to throttle:
                                    rpm += estimatedPower / 1200 * 190 * this.variableThrottle;

                                    if (rpm < Main.Drivetrain.StallRpm && k > 0)
                                    {
                                        continue;
                                    }

                                    allStalled = false;
                                    if (orpm > Main.Drivetrain.MaximumRpm)
                                    {
                                        continue;
                                    }

                                    var thr = Main.Drivetrain.CalculateThrottleByPower(rpm, reqPower);
                                    if (thr > 1)
                                    {
                                        thr = 1;
                                    }

                                    if (thr < 0)
                                    {
                                        thr = 0;
                                    }

                                    var eff = Main.Drivetrain.CalculateFuelConsumption(rpm, thr) / reqPower;
                                    var pwr = Main.Drivetrain.CalculatePower(rpm, this.variableThrottle);

                                    if (this.Efficiency)
                                    {
                                        if (calcEfficiency > eff && eff * 1.1 < curEfficiency)
                                        {
                                            calcEfficiency = eff;
                                            calcEfficiencyGear = k;
                                            calcThrottle = thr;
                                            calcPower = pwr;
                                        }
                                    }
                                    else
                                    {
                                        if (pwr > calcEfficiency)
                                        {
                                            calcEfficiency = pwr;
                                            calcEfficiencyGear = k;
                                            calcPower = pwr;
                                        }
                                    }
                                }

                                if (allStalled)
                                {
                                    if (maxgears >= 10)
                                    {
                                        this.Gear = 3;
                                    }
                                    else
                                    {
                                        this.Gear = 1;
                                    }
                                }
                                else if (calcEfficiencyGear >= 0 && calcEfficiencyGear + 1 != this.Gear)
                                {
                                    // Hysterisis
                                    if (Math.Abs(curPower - calcPower) > 25)
                                    {
                                        this.Gear = calcEfficiencyGear + 1;
                                    }
                                }

                                if (this.Efficiency)
                                {
                                    // variableThrottle = Main.Drivetrain.CalculateThrottleByPower(
                                    // data.Telemetry.EngineRpm,
                                    // reqPower);
                                }
                                else
                                { }
                                if (this.Gear > 0 && this.Gear != data.Telemetry.Gear)
                                {
                                    this.ShiftingPhase = ShiftPhase.OffThrottle;
                                }
                            }
                        }

                        break;

                    case ShiftPhase.OffThrottle:
                        this.ShiftingPhase++;
                        break;

                    case ShiftPhase.OnClutch:
                        this.ShiftingPhase++;
                        break;

                    case ShiftPhase.EngageRange:
                        this.ShiftingPhase++;
                        break;

                    case ShiftPhase.EngageGear:
                        this.ShiftingPhase++;
                        break;

                    case ShiftPhase.Evaluate:
                        if (this.Gear == data.Telemetry.Gear)
                        {
                            this.LastSeenRange = this.DeductRangeFromGear(this.Gear);
                            this.ShiftingPhase++;
                        }
                        else
                        {
                            this.shiftingRetry++;
                            if (this.shiftingRetry > 50)
                            {
                                if (this.Gear > 0)
                                {
                                    this.Gear--;
                                }
                                else
                                {
                                    this.Gear = -1;
                                }

                                this.shiftingRetry = 0;
                                this.ShiftingPhase = ShiftPhase.EngageGear;
                            }
                            else if (this.shiftingRetry > 2)
                            {
                                this.LastSeenRange++;
                                this.LastSeenRange = this.LastSeenRange % 4;
                                this.ShiftingPhase = ShiftPhase.OnClutch;
                            }
                            else
                            {
                                this.ShiftingPhase = ShiftPhase.EngageGear;
                            }
                        }

                        break;

                    case ShiftPhase.OffClutch:
                        this.ShiftingPhase++;
                        break;

                    case ShiftPhase.OnThrottle:
                        this.shiftingRetry = 0;
                        this.ShiftingPhase = ShiftPhase.Cooldown;
                        break;

                    case ShiftPhase.Cooldown:
                        this.shiftingRetry++;
                        if (this.shiftingRetry > 4)
                        {
                            this.shiftingRetry = 0;
                            this.ShiftingPhase = ShiftPhase.None;
                        }

                        break;
                }
            }
        }

        private int DeductRangeFromGear(int gr)
        {
            if (gr >= 1 && gr <= 6)
            {
                return 1;
            }
            else if (gr >= 7 && gr <= 12)
            {
                return 2;
            }
            else if (gr >= 13 && gr <= 18)
            {
                return 3;
            }
            else if (gr >= 19 && gr <= 24)
            {
                return 4;
            }
            else
            {
                return -1;
            }
        }

        private bool GetRangeButton(int bt)
        {
            var currentRange = this.LastSeenRange;
            var newRange = this.DeductRangeFromGear(this.Gear);

            if (currentRange != newRange)
            {
                if (this.ShiftingPhase == ShiftPhase.EngageRange)
                {
                    return this.RangeSwitchTruthTable(bt, currentRange, newRange);
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        private bool GetShiftButton(int bt)
        {
            if (bt == -1)
            {
                return this.Gear == -1 && this.ShiftingPhase != ShiftPhase.EngageRange;
            }
            else
            {
                var range = this.DeductRangeFromGear(this.Gear) - 1;
                var activeGearInRange = this.Gear - range * 6;
                return activeGearInRange == bt && this.ShiftingPhase != ShiftPhase.EngageRange;
            }
        }

        private bool RangeSwitchTruthTable(int button, int old, int nw)
        {
            switch (old)
            {
                case 1:
                    if (nw == 1)
                    {
                        return false;
                    }

                    if (nw == 2)
                    {
                        return button == 1;
                    }

                    if (nw == 3)
                    {
                        return button == 2;
                    }

                    if (nw == 4)
                    {
                        return button == 1 || button == 2;
                    }

                    break;
                case 2:
                    if (nw == 1)
                    {
                        return button == 1;
                    }

                    if (nw == 2)
                    {
                        return false;
                    }

                    if (nw == 3)
                    {
                        return button == 1 || button == 2;
                    }

                    if (nw == 4)
                    {
                        return button == 2;
                    }

                    break;
                case 3:
                    if (nw == 1)
                    {
                        return button == 2;
                    }

                    if (nw == 2)
                    {
                        return button == 2 || button == 1;
                    }

                    if (nw == 3)
                    {
                        return false;
                    }

                    if (nw == 4)
                    {
                        return button == 1;
                    }

                    break;
                case 4:
                    if (nw == 1)
                    {
                        return button == 1 || button == 2;
                    }

                    if (nw == 2)
                    {
                        return button == 2;
                    }

                    if (nw == 3)
                    {
                        return button == 1;
                    }

                    if (nw == 4)
                    {
                        return false;
                    }

                    break;
            }
            return false;
        }
    }

    public enum ShiftPhase
    {
        WaitButton,

        None,

        OffThrottle,

        OnClutch,

        EngageRange,

        EngageGear,

        Evaluate,

        OffClutch,

        OnThrottle,

        Cooldown
    }
}