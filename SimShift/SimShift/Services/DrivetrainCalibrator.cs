using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using SimShift.Data.Common;
using SimShift.Entities;

namespace SimShift.Services
{
    public enum DrivetrainCalibrationStage
    {
        None,

        StartIdleRpm,

        FinishIdleRpm,

        StartMaxRpm,

        FinishMaxRpm,

        StartGears,

        FinishGears,

        StartGearRatios,

        // ..
        EndGearRatios,

        ShiftToFirst
    }

    /// <summary>
    ///     This module calibrates the "Drivetrain" object so most driving helps can be used.
    ///     The calibration data acquired is:
    ///     - Idle RPM
    ///     - Maximum RPM
    ///     - Gear Ratio's (forward gears only)
    /// </summary>
    public class DrivetrainCalibrator : IControlChainObj
    {
        private string calibrateShiftStyle = "up_1thr";

        private bool calibrationPreDone = false;

        private double clutch;

        private int gear;

        private float gearRatioSpeedCruise = 0.0f;

        private double maxRpmMeasured = 0;

        private double maxRpmTarget = 0;

        private DrivetrainCalibrationStage nextStage;

        private double previousEngineRpm = 0;

        private double previousThrottle = 0;

        private bool reqClutch;

        private bool reqThrottle;

        private double sample = 0;

        private int samplesTaken = 0;

        private int shiftToFirstRangeAttempt = 0;

        private DrivetrainCalibrationStage stage;

        private double throttle;

        public static int UncalibratedGear { get; set; }

        public bool Active => this.Calibrating;

        public bool Calibrating { get; private set; }

        public bool Enabled => this.Calibrating;

        public bool MeasurementSettled => DateTime.Now > this.MeasurementSettletime;

        public DateTime MeasurementSettletime { get; private set; }

        public IEnumerable<string> SimulatorsBan => new string[0];

        public IEnumerable<string> SimulatorsOnly => new string[0];

        private bool reqGears
        {
            get => Main.Transmission.OverruleShifts;

            set => Main.Transmission.OverruleShifts = value;
        }

        public double GetAxis(JoyControls c, double val)
        {
            switch (c)
            {
                case JoyControls.Throttle:
                    return this.throttle;
                    break;

                case JoyControls.Clutch:
                    return this.clutch;
                    break;

                default: return val;
            }
        }

        public bool GetButton(JoyControls c, bool val)
        {
            switch (c)
            {
                case JoyControls.Gear1: return 1 == this.gear;
                case JoyControls.Gear2: return 2 == this.gear;
                case JoyControls.Gear3: return 3 == this.gear;
                case JoyControls.Gear4: return 4 == this.gear;
                case JoyControls.Gear5: return 5 == this.gear;
                case JoyControls.Gear6: return 6 == this.gear;
                case JoyControls.Gear7: return 7 == this.gear;
                case JoyControls.Gear8: return 8 == this.gear;
                case JoyControls.GearR: return -1 == this.gear;
                default: return val;
            }
        }

        public bool Requires(JoyControls c)
        {
            switch (c)
            {
                case JoyControls.Throttle: return this.reqThrottle;
                case JoyControls.Clutch: return this.reqClutch;

                case JoyControls.Gear1:
                case JoyControls.Gear2:
                case JoyControls.Gear3:
                case JoyControls.Gear4:
                case JoyControls.Gear5:
                case JoyControls.Gear6:
                case JoyControls.Gear7:
                case JoyControls.Gear8:
                case JoyControls.GearR: return false; // return reqGears;

                default: return false;
            }
        }

        public void TickControls()
        { }

        public void TickTelemetry(IDataMiner data)
        {
            if (!Main.Data.Active.SupportsCar)
            {
                return;
            }

            bool wasCalibrating = this.Calibrating;
            this.Calibrating = !Main.Drivetrain.Calibrated;
            if (!wasCalibrating && this.Calibrating)
            {
                Debug.WriteLine("now calibrating");
                this.stage = DrivetrainCalibrationStage.StartIdleRpm;
            }

            if (this.stage != DrivetrainCalibrationStage.None && !this.Calibrating)
            {
                this.stage = DrivetrainCalibrationStage.None;
            }

            switch (this.stage)
            {
                case DrivetrainCalibrationStage.None:
                    this.reqGears = false;
                    this.reqThrottle = false;
                    this.reqClutch = false;
                    break;

                case DrivetrainCalibrationStage.StartIdleRpm:

                    this.reqClutch = true;
                    this.reqThrottle = true;
                    this.reqGears = true;
                    Main.Transmission.Shift(data.Telemetry.Gear, 0, this.calibrateShiftStyle);
                    if (data.Telemetry.EngineRpm < 300)
                    {
                        this.throttle = 1;
                        this.clutch = 1;
                        this.gear = 0;
                    }
                    else if (data.Telemetry.EngineRpm > 2000)
                    {
                        this.throttle = 0;
                        this.clutch = 1;
                        this.gear = 0;
                    }
                    else
                    {
                        this.throttle = 0;
                        this.clutch = 1;
                        this.gear = 0;

                        if (Math.Abs(data.Telemetry.EngineRpm - this.previousEngineRpm) < 1)
                        {
                            this.stage = DrivetrainCalibrationStage.FinishIdleRpm;

                            this.MeasurementSettletime = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 2750));
                        }
                    }

                    this.previousEngineRpm = data.Telemetry.EngineRpm;
                    break;
                case DrivetrainCalibrationStage.FinishIdleRpm:
                    if (this.MeasurementSettled)
                    {
                        Debug.WriteLine("Idle RPM: " + data.Telemetry.EngineRpm);
                        if (data.Telemetry.EngineRpm < 300)
                        {
                            this.stage = DrivetrainCalibrationStage.StartIdleRpm;
                        }
                        else
                        {
                            Main.Drivetrain.StallRpm = data.Telemetry.EngineRpm;

                            this.stage = DrivetrainCalibrationStage.StartMaxRpm;
                            this.maxRpmTarget = data.Telemetry.EngineRpm + 1000;
                            this.previousThrottle = 0;
                        }
                    }

                    break;

                case DrivetrainCalibrationStage.StartMaxRpm:
                    this.reqClutch = true;
                    this.reqThrottle = true;
                    this.reqGears = true;

                    this.clutch = 1;
                    this.throttle = 1;
                    this.maxRpmMeasured = 0;

                    this.MeasurementSettletime = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 1500));
                    this.stage = DrivetrainCalibrationStage.FinishMaxRpm;

                    break;
                case DrivetrainCalibrationStage.FinishMaxRpm:

                    this.throttle = 1;
                    this.maxRpmMeasured = Math.Max(this.maxRpmMeasured, data.Telemetry.EngineRpm);

                    if (this.MeasurementSettled)
                    {
                        if (Math.Abs(this.maxRpmMeasured - Main.Drivetrain.StallRpm) < 500)
                        {
                            Debug.WriteLine("Totally messed up MAX RPM.. resetting");
                            this.stage = DrivetrainCalibrationStage.StartIdleRpm;
                        }
                        else
                        {
                            Debug.WriteLine("Max RPM approx: " + this.maxRpmMeasured);

                            Main.Drivetrain.MaximumRpm = this.maxRpmMeasured - 300;

                            this.stage = DrivetrainCalibrationStage.ShiftToFirst;
                            this.nextStage = DrivetrainCalibrationStage.StartGears;
                        }
                    }

                    break;

                case DrivetrainCalibrationStage.StartGears:
                    this.reqClutch = true;
                    this.reqThrottle = true;
                    this.reqGears = true;

                    this.throttle = 0;
                    this.clutch = 1;
                    this.gear++;
                    Main.Transmission.Shift(data.Telemetry.Gear, this.gear, this.calibrateShiftStyle);
                    this.MeasurementSettletime = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 500));

                    this.stage = DrivetrainCalibrationStage.FinishGears;

                    break;
                case DrivetrainCalibrationStage.FinishGears:

                    if (this.MeasurementSettled && !Main.Transmission.IsShifting)
                    {
                        if (data.Telemetry.Gear != this.gear)
                        {
                            this.gear--;

                            // Car doesn't have this gear.
                            Debug.WriteLine("Gears: " + this.gear);

                            if (this.gear <= 0)
                            {
                                Debug.WriteLine("That's not right");
                                this.stage = DrivetrainCalibrationStage.StartGears;
                            }
                            else
                            {
                                Main.Drivetrain.Gears = this.gear;
                                Main.Drivetrain.GearRatios = new double[this.gear];

                                this.stage = DrivetrainCalibrationStage.ShiftToFirst;
                                this.nextStage = DrivetrainCalibrationStage.StartGearRatios;
                                this.MeasurementSettletime = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 500));
                                this.calibrationPreDone = false;
                            }

                            this.gear = 0;
                        }
                        else
                        {
                            this.stage = DrivetrainCalibrationStage.StartGears;
                        }
                    }

                    break;

                case DrivetrainCalibrationStage.ShiftToFirst:
                    if (!Main.Transmission.IsShifting && this.MeasurementSettled)
                    {
                        if (data.Telemetry.Gear != 1)
                        {
                            Main.Transmission.Shift(this.shiftToFirstRangeAttempt * Main.Transmission.RangeSize + 1, 1, this.calibrateShiftStyle);
                            this.shiftToFirstRangeAttempt++;

                            this.MeasurementSettletime = DateTime.Now.Add(new TimeSpan(0, 0, 0, 0, 100));
                            if (this.shiftToFirstRangeAttempt > 3)
                            {
                                this.shiftToFirstRangeAttempt = 0;
                            }
                        }
                        else
                        {
                            this.stage = this.nextStage;
                            this.MeasurementSettletime = DateTime.MaxValue;
                        }
                    }

                    break;

                case DrivetrainCalibrationStage.EndGearRatios:

                    if (Main.Drivetrain.GearRatios.Length >= data.Telemetry.Gear)
                    {
                        if (data.Telemetry.Gear <= 0)
                        {
                            this.stage = DrivetrainCalibrationStage.StartGearRatios;
                            break;
                        }

                        if (data.Telemetry.EngineRpm > Main.Drivetrain.StallRpm * 1.15)
                        {
                            var gr = Main.Drivetrain.GearRatios[data.Telemetry.Gear - 1];
                            if (gr != 0)
                            {
                                this.stage = DrivetrainCalibrationStage.StartGearRatios;
                                break;
                            }

                            this.reqThrottle = true;
                            this.throttle = this.gearRatioSpeedCruise - data.Telemetry.Speed;
                            this.throttle /= 3;

                            var ratio = data.Telemetry.EngineRpm / (3.6 * data.Telemetry.Speed);
                            if (ratio > 1000 || ratio < 1)
                            {
                                this.stage = DrivetrainCalibrationStage.StartGearRatios;
                                break;
                            }

                            Debug.WriteLine("Gear " + data.Telemetry.Gear + " : " + ratio);

                            // start sampling
                            if (this.sample == 0)
                            {
                                this.sample = ratio;
                            }
                            else
                            {
                                this.sample = this.sample * 0.9 + ratio * 0.1;
                            }

                            this.samplesTaken++;

                            if (this.samplesTaken == 50)
                            {
                                Main.Drivetrain.GearRatios[data.Telemetry.Gear - 1] = this.sample;
                            }
                        }
                        else
                        {
                            this.stage = DrivetrainCalibrationStage.StartGearRatios;
                            break;
                        }
                    }
                    else
                    {
                        this.stage = DrivetrainCalibrationStage.StartGearRatios;
                    }

                    break;

                case DrivetrainCalibrationStage.StartGearRatios:

                    this.reqGears = false;
                    this.reqThrottle = false;
                    this.reqClutch = false;

                    // Activate get-home-mode; which shifts at 4x stall rpm
                    Main.Transmission.GetHomeMode = true;

                    if (data.Telemetry.EngineRpm > Main.Drivetrain.StallRpm * 2)
                    {
                        // Driving at reasonable rpm's.
                        if (data.Telemetry.Gear > 0)
                        {
                            if (Main.Drivetrain.GearRatios.Length >= data.Telemetry.Gear && data.Telemetry.EngineRpm > Main.Drivetrain.StallRpm * 2)
                            {
                                var gr = Main.Drivetrain.GearRatios[data.Telemetry.Gear - 1];

                                if (gr == 0)
                                {
                                    this.samplesTaken = 0;
                                    this.gearRatioSpeedCruise = data.Telemetry.Speed;
                                    this.stage = DrivetrainCalibrationStage.EndGearRatios;
                                }
                            }
                        }

                        var GearsCalibrated = true;
                        for (int i = 0; i < Main.Drivetrain.Gears; i++)
                        {
                            if (Main.Drivetrain.GearRatios[i] < 1)
                            {
                                UncalibratedGear = i;

                                GearsCalibrated = false;
                            }
                        }

                        if (GearsCalibrated)
                        {
                            if (this.MeasurementSettled)
                            {
                                Main.Transmission.GetHomeMode = false;
                                Debug.WriteLine("Calibration done");
                                this.stage = DrivetrainCalibrationStage.None;

                                Main.Store(Main.Drivetrain.ExportParameters(), Main.Drivetrain.File);
                                Main.Load(Main.Drivetrain, Main.Drivetrain.File);
                            }

                            if (!this.calibrationPreDone)
                            {
                                this.calibrationPreDone = true;
                                this.MeasurementSettletime = DateTime.Now.Add(new TimeSpan(0, 0, 0, 3));
                            }
                        }
                    }

                    break;
            }
        }
    }
}