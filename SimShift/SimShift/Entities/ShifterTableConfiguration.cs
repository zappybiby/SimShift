using System;
using System.Collections.Generic;
using System.Linq;

using SimShift.Dialogs;
using SimShift.Models;

namespace SimShift.Entities
{
    public class ShifterTableConfiguration
    {
        public float Mass;

        public ShifterTableConfigurationDefault Mode;

        public int SpdPerGear;

        // Speed / Load / [Gear]
        public Dictionary<int, Dictionary<double, int>> tableGear;

        public Dictionary<int, Dictionary<double, double>> tableThrottle;

        public ShifterTableConfiguration(ShifterTableConfigurationDefault def, IDrivetrain drivetrain, int spdPerGear, float staticMass)
        {
            this.Mode = def;
            this.SpdPerGear = spdPerGear;
            this.Mass = staticMass;

            this.Air = new Ets2Aero();
            this.Drivetrain = drivetrain;
            this.MaximumSpeed = 600;

            switch (def)
            {
                case ShifterTableConfigurationDefault.PeakRpm:
                    this.DefaultByPeakRpm();
                    break;
                case ShifterTableConfigurationDefault.Performance:
                    this.DefaultByPowerPerformance();
                    break;
                case ShifterTableConfigurationDefault.Economy:
                    this.DefaultByPowerEconomy();
                    break;
                case ShifterTableConfigurationDefault.Efficiency:
                    this.DefaultByPowerEfficiency();
                    break;
                case ShifterTableConfigurationDefault.AlsEenOpa:
                    this.DefaultByOpa();
                    break;
                case ShifterTableConfigurationDefault.Henk:
                    this.DefaultByHenk();
                    break;

                case ShifterTableConfigurationDefault.PowerEfficiency:
                    this.DefaultByPowerEfficiency2();
                    break;
            }

            if (spdPerGear > 0)
            {
                var spdPerGearReduced = spdPerGear - staticMass / 1000 / 1.25;
                if (spdPerGearReduced < 1)
                {
                    spdPerGearReduced = 1;
                }

                Console.WriteLine("Spd per gear:" + spdPerGearReduced);
                this.MinimumSpeedPerGear((int) Math.Round(spdPerGearReduced));
            }

            string l = string.Empty;
            for (var r = 0; r < 2500; r += 10)
            {
                var fuel = this.Drivetrain.CalculateFuelConsumption(r, 1);
                var ratio = drivetrain.CalculatePower(r, 1) / fuel;

                l += r + "," + this.Drivetrain.CalculatePower(r, 1) + "," + this.Drivetrain.CalculatePower(r, 0) + "," + fuel + "," + ratio + "\r\n";
            }

            // File.WriteAllText("./ets2engine.csv", l);
        }

        public Ets2Aero Air { get; private set; }

        public IDrivetrain Drivetrain { get; private set; }

        public int MaximumSpeed { get; private set; }

        public void DefaultByHenk()
        {
            var shiftRpmHigh = new float[12] { 1000, 1000, 1000, 1100, 1700, 1900, 2000, 2000, 1900, 1800, 1500, 1300 };
            var shiftRpmLow = new float[12] { 750, 750, 750, 750, 750, 750, 750, 800, 850, 800, 850, 900 };

            this.tableGear = new Dictionary<int, Dictionary<double, int>>();
            this.tableThrottle = new Dictionary<int, Dictionary<double, double>>();

            // Make sure there are 20 rpm steps, and 20 load steps
            // (20x20 = 400 items)
            for (int speed = 0; speed <= this.MaximumSpeed; speed += 1)
            {
                this.tableGear.Add(speed, new Dictionary<double, int>());
                this.tableThrottle.Add(speed, new Dictionary<double, double>());
                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    var gearSet = false;
                    var smallestDelta = double.MaxValue;
                    var smallestDeltaGear = 0;
                    var highestGearBeforeStalling = 0;
                    for (int gear = 0; gear < this.Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = this.Drivetrain.GearRatios[gear] * speed;
                        if (calculatedRpm < shiftRpmLow[gear])
                        {
                            continue;
                        }

                        highestGearBeforeStalling = gear;
                        if (calculatedRpm > shiftRpmHigh[gear])
                        {
                            continue;
                        }

                        var driveRpm = shiftRpmLow[gear] + (shiftRpmHigh[gear] - shiftRpmLow[gear]) * load;
                        var delta = Math.Abs(calculatedRpm - driveRpm);

                        if (delta < smallestDelta)
                        {
                            smallestDelta = delta;
                            smallestDeltaGear = gear;
                            gearSet = true;
                        }
                    }

                    if (gearSet)
                    {
                        this.tableGear[speed].Add(load, smallestDeltaGear + 1);
                    }
                    else
                    {
                        this.tableGear[speed].Add(load, highestGearBeforeStalling + 1);
                    }

                    this.tableThrottle[speed].Add(load, 1);
                }
            }
        }

        public void DefaultByOpa()
        {
            this.tableGear = new Dictionary<int, Dictionary<double, int>>();
            this.tableThrottle = new Dictionary<int, Dictionary<double, double>>();

            // Make sure there are 20 rpm steps, and 20 load steps
            // (20x20 = 400 items)
            for (int speed = 0; speed <= this.MaximumSpeed; speed += 1)
            {
                this.tableGear.Add(speed, new Dictionary<double, int>());
                this.tableThrottle.Add(speed, new Dictionary<double, double>());
                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    this.tableThrottle[speed].Add(load, 1);
                    var gearSet = false;
                    var shiftRpm = 800 + 600 * load;
                    var highestGearBeforeStalling = 0;
                    for (int gear = 0; gear < this.Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = this.Drivetrain.GearRatios[gear] * speed;
                        if (calculatedRpm < 800)
                        {
                            continue;
                        }

                        highestGearBeforeStalling = gear;
                        if (calculatedRpm > shiftRpm)
                        {
                            continue;
                        }

                        gearSet = true;
                        this.tableGear[speed].Add(load, gear + 1);
                        break;
                    }

                    if (!gearSet)
                    {
                        this.tableGear[speed].Add(load, highestGearBeforeStalling + 1);
                    }
                }
            }
        }

        public void DefaultByPeakRpm()
        {
            this.tableGear = new Dictionary<int, Dictionary<double, int>>();
            this.tableThrottle = new Dictionary<int, Dictionary<double, double>>();

            // Make sure there are 20 rpm steps, and 20 load steps
            // (20x20 = 400 items)
            for (int speed = 0; speed <= this.MaximumSpeed; speed += 1)
            {
                this.tableGear.Add(speed, new Dictionary<double, int>());
                this.tableThrottle.Add(speed, new Dictionary<double, double>());
                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    this.tableThrottle[speed].Add(load, 1);
                    var gearSet = false;
                    var latestGearThatWasNotStalling = 1;

                    var shiftRpm = this.Drivetrain.StallRpm + (this.Drivetrain.MaximumRpm - 700 - this.Drivetrain.StallRpm) * load;

                    // shiftRpm = 3000 + (Drivetrain.MaximumRpm - 3000-1000) * load;
                    for (int gear = 0; gear < this.Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = this.Drivetrain.GearRatios[gear] * speed;
                        if (calculatedRpm < this.Drivetrain.StallRpm * 1.75)
                        {
                            continue;
                        }

                        latestGearThatWasNotStalling = gear;
                        if (calculatedRpm > shiftRpm)
                        {
                            continue;
                        }

                        gearSet = true;
                        this.tableGear[speed].Add(load, gear + 1);
                        break;
                    }

                    if (!gearSet)
                    {
                        this.tableGear[speed].Add(load, latestGearThatWasNotStalling == 1 ? 1 : latestGearThatWasNotStalling + 1);
                    }
                }
            }
        }

        public void DefaultByPowerEconomy()
        {
            var maxPwr = this.Drivetrain.CalculateMaxPower() * 0.75;
            maxPwr = 500;
            this.tableGear = new Dictionary<int, Dictionary<double, int>>();
            this.tableThrottle = new Dictionary<int, Dictionary<double, double>>();

            // Make sure there are 20 rpm steps, and 10 load steps
            for (int speed = 0; speed <= this.MaximumSpeed; speed += 1)
            {
                this.tableGear.Add(speed, new Dictionary<double, int>());
                this.tableThrottle.Add(speed, new Dictionary<double, double>());
                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    this.tableThrottle[speed].Add(load, 1);
                    var gearSet = false;
                    double req = Math.Max(25, load * maxPwr);

                    var bestFuelEfficiency = double.MaxValue;
                    var bestFuelGear = 0;
                    var highestValidGear = 11;

                    for (int gear = 0; gear < this.Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = this.Drivetrain.GearRatios[gear] * speed;

                        if (calculatedRpm <= this.Drivetrain.StallRpm * 1.033333)
                        {
                            highestValidGear = 0;
                            continue;
                        }

                        if (calculatedRpm >= this.Drivetrain.MaximumRpm)
                        {
                            continue;
                        }

                        var thr = this.Drivetrain.CalculateThrottleByPower(calculatedRpm, req);

                        if (thr > 1)
                        {
                            continue;
                        }

                        if (thr < 0)
                        {
                            continue;
                        }

                        if (double.IsNaN(thr) || double.IsInfinity(thr))
                        {
                            continue;
                        }

                        var fuel = this.Drivetrain.CalculateFuelConsumption(calculatedRpm, thr);

                        if (bestFuelEfficiency >= fuel)
                        {
                            bestFuelEfficiency = fuel;
                            bestFuelGear = gear;
                            gearSet = true;
                        }
                    }

                    if (!gearSet)
                    {
                        if (this.Drivetrain is Ets2Drivetrain)
                        {
                            highestValidGear = Math.Max(2, highestValidGear);
                        }

                        this.tableGear[speed].Add(load, 1 + highestValidGear);
                    }
                    else
                    {
                        bestFuelGear = Math.Max(2, bestFuelGear);
                        if (this.Drivetrain is Ets2Drivetrain)
                        {
                            highestValidGear = Math.Max(2, bestFuelGear);
                        }

                        this.tableGear[speed].Add(load, bestFuelGear + 1);
                    }
                }
            }
        }

        public void DefaultByPowerEfficiency()
        {
            this.tableGear = new Dictionary<int, Dictionary<double, int>>();
            this.tableThrottle = new Dictionary<int, Dictionary<double, double>>();

            // Make sure there are 20 rpm steps, and 10 load steps
            for (int speed = 0; speed <= this.MaximumSpeed; speed += 1)
            {
                this.tableGear.Add(speed, new Dictionary<double, int>());
                this.tableThrottle.Add(speed, new Dictionary<double, double>());
                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    this.tableThrottle[speed].Add(load, 1);
                    var gearSet = false;
                    var bestFuelEfficiency = double.MinValue;
                    var bestFuelGear = 0;

                    for (int gear = 0; gear < this.Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = this.Drivetrain.GearRatios[gear] * speed;

                        if (calculatedRpm < this.Drivetrain.StallRpm * 1.5)
                        {
                            continue;
                        }

                        if (calculatedRpm > this.Drivetrain.MaximumRpm)
                        {
                            continue;
                        }

                        var thr = (load < 0.05) ? 0.05 : load;

                        var pwr = this.Drivetrain.CalculatePower(calculatedRpm, thr);
                        var fuel = this.Drivetrain.CalculateFuelConsumption(calculatedRpm, thr);
                        var efficiency = pwr / fuel;

                        if (efficiency > bestFuelEfficiency)
                        {
                            bestFuelEfficiency = efficiency;
                            bestFuelGear = gear;
                            gearSet = true;
                        }
                    }

                    if (!gearSet)
                    {
                        if (this.Drivetrain is Ets2Drivetrain && this.Drivetrain.Gears >= 10)
                        {
                            this.tableGear[speed].Add(load, 3);
                        }
                        else
                        {
                            this.tableGear[speed].Add(load, 1);
                        }
                    }
                    else
                    {
                        if (this.Drivetrain is Ets2Drivetrain && this.Drivetrain.Gears >= 10)
                        {
                            bestFuelGear = Math.Max(2, bestFuelGear);
                        }

                        this.tableGear[speed].Add(load, bestFuelGear + 1);
                    }
                }
            }
        }

        public void DefaultByPowerPerformance()
        {
            this.tableGear = new Dictionary<int, Dictionary<double, int>>();
            this.tableThrottle = new Dictionary<int, Dictionary<double, double>>();

            // Make sure there are 20 rpm steps, and 10 load steps
            for (int speed = 0; speed <= this.MaximumSpeed; speed += 1)
            {
                this.tableGear.Add(speed, new Dictionary<double, int>());
                this.tableThrottle.Add(speed, new Dictionary<double, double>());
                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    this.tableThrottle[speed].Add(load, 1);
                    var gearSet = false;

                    var bestPower = double.MinValue;
                    var bestPowerGear = 0;
                    var latestGearThatWasNotStalling = 1;

                    for (int gear = 0; gear < this.Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = this.Drivetrain.GearRatios[gear] * speed;
                        if (calculatedRpm < this.Drivetrain.StallRpm)
                        {
                            calculatedRpm = this.Drivetrain.StallRpm;
                        }

                        if (calculatedRpm < 1200)
                        {
                            continue;
                        }

                        var pwr = this.Drivetrain.CalculatePower(calculatedRpm + 200, load < 0.2 ? 0.2 : load);

                        latestGearThatWasNotStalling = gear;
                        if (calculatedRpm > this.Drivetrain.MaximumRpm)
                        {
                            continue;
                        }

                        if (gear == 0 && calculatedRpm > this.Drivetrain.MaximumRpm - 200)
                        {
                            continue;
                        }

                        if (pwr > bestPower)
                        {
                            bestPower = pwr;
                            bestPowerGear = gear;
                            gearSet = true;
                        }
                    }

                    // if (speed < 30 )
                    // tableGear[speed].Add(load, latestGearThatWasNotStalling);
                    // else 
                    if (!gearSet)
                    {
                        this.tableGear[speed].Add(load, latestGearThatWasNotStalling == 1 ? 1 : latestGearThatWasNotStalling + 1);
                    }
                    else
                    {
                        this.tableGear[speed].Add(load, bestPowerGear + 1);
                    }
                }
            }
        }

        public ShifterTableLookupResult Lookup(double speed, double load)
        {
            var speedA = 0.0;
            var speedB = 0.0;
            var loadA = 0.0;
            var loadB = 0.0;

            foreach (var spd in this.tableGear.Keys)
            {
                if (spd >= speed && speedA <= speed)
                {
                    speedB = spd;
                    break;
                }

                speedA = spd;
            }

            foreach (var ld in this.tableGear[(int) speedA].Keys)
            {
                if (ld >= load && loadA <= load)
                {
                    loadB = ld;
                    break;
                }

                loadA = ld;
            }

            if (speedB == speedA)
            {
                speedA = this.tableGear.Keys.FirstOrDefault();
                speedB = this.tableGear.Keys.Skip(1).FirstOrDefault();
            }

            if (loadB == loadA)
            {
                loadA = this.tableGear[(int) speedA].Keys.FirstOrDefault();
                loadB = this.tableGear[(int) speedA].Keys.Skip(1).FirstOrDefault();
            }

            var gear = 1.0 / (speedB - speedA) / (loadB - loadA) * (this.tableGear[(int) speedA][loadA] * (speedB - speed) * (loadB - load) + this.tableGear[(int) speedB][loadA] * (speed - speedA) * (loadB - load) + this.tableGear[(int) speedA][loadB] * (speedB - speed) * (load - loadA) + this.tableGear[(int) speedB][loadB] * (speed - speedA) * (load - loadA));
            if (double.IsNaN(gear))
            {
                gear = 1;
            }

            // Look up the closests RPM.
            var closestsSpeed = this.tableGear.Keys.OrderBy(x => Math.Abs(speed - x)).FirstOrDefault();
            var closestsLoad = this.tableGear[closestsSpeed].Keys.OrderBy(x => Math.Abs(x - load)).FirstOrDefault();

            // return new ShifterTableLookupResult((int)Math.Round(gear), closestsSpeed, closestsLoad);
            return new ShifterTableLookupResult(this.tableGear[closestsSpeed][closestsLoad], this.tableThrottle[closestsSpeed][closestsLoad], closestsSpeed, closestsLoad);
        }

        public void MinimumSpeedPerGear(int minimum)
        {
            if (this.Drivetrain.Gears == 0)
            {
                return;
            }

            var loads = this.tableGear.FirstOrDefault().Value.Keys.ToList();
            var speeds = this.tableGear.Keys.ToList();

            // Clean up first gear.
            var lowestFirstGear = this.tableGear[minimum][loads.LastOrDefault()];

            // Set up for all gears
            for (int k = 0; k < minimum + 2; k++)
            {
                foreach (var load in loads)
                {
                    this.tableGear[k][load] = lowestFirstGear;
                }
            }

            foreach (var load in loads)
            {
                for (int i = 0; i < speeds.Count; i++)
                {
                    int startI = i;
                    int endI = i;

                    int g = this.tableGear[speeds[i]][load];

                    do
                    {
                        while (endI < speeds.Count - 1 && this.tableGear[speeds[endI]][load] == g)
                        {
                            endI++;
                        }

                        g++;
                    }
                    while (endI - startI < minimum && g < this.Drivetrain.Gears);

                    for (int j = startI; j <= endI; j++)
                    {
                        this.tableGear[speeds[j]][load] = g - 1;
                    }

                    i = endI;
                }
            }
        }

        public double RpmForSpeed(float speed, int gear)
        {
            if (gear > this.Drivetrain.GearRatios.Length)
            {
                return this.Drivetrain.StallRpm;
            }

            if (gear <= 0)
            {
                return this.Drivetrain.StallRpm + 50;
            }

            return this.Drivetrain.GearRatios[gear - 1] * speed * 3.6;
        }

        private void DefaultByPowerEfficiency2()
        {
            this.tableGear = new Dictionary<int, Dictionary<double, int>>();
            this.tableThrottle = new Dictionary<int, Dictionary<double, double>>();

            if (this.Drivetrain.Gears == 0)
            {
                return;
            }

            // Make sure there are 20 rpm steps, and 10 load steps
            for (int speed = 0; speed <= this.MaximumSpeed; speed += 1)
            {
                this.tableGear.Add(speed, new Dictionary<double, int>());
                this.tableThrottle.Add(speed, new Dictionary<double, double>());

                Dictionary<int, float> pwrPerGear = new Dictionary<int, float>();

                // Populate:
                for (int gear = 0; gear < this.Drivetrain.Gears; gear++)
                {
                    var calculatedRpm = this.Drivetrain.GearRatios[gear] * speed;
                    var power = (float) this.Drivetrain.CalculatePower(calculatedRpm, 1);
                    pwrPerGear.Add(gear, power);
                }

                var maxPwrAvailable = pwrPerGear.Values.Max() * 0.85;

                for (var load = 0.0; load <= 1.0; load += 0.1)
                {
                    Dictionary<int, float> efficiencyPerGear = new Dictionary<int, float>();
                    var highestGearBeforeStalling = 0;

                    for (int gear = 0; gear < this.Drivetrain.Gears; gear++)
                    {
                        var calculatedRpm = this.Drivetrain.GearRatios[gear] * speed;
                        if (calculatedRpm > this.Drivetrain.StallRpm)
                        {
                            highestGearBeforeStalling = gear;
                        }

                        var power = (float) this.Drivetrain.CalculatePower(calculatedRpm, 1);
                        var fuel = (float) this.Drivetrain.CalculateFuelConsumption(calculatedRpm, Math.Max(0.05, load));
                        efficiencyPerGear.Add(gear, fuel / power);
                    }

                    var bestGear = highestGearBeforeStalling;
                    var bestGearV = 100.0f;
                    foreach (var kvp in efficiencyPerGear)
                    {
                        if (kvp.Value < bestGearV && kvp.Value > 0)
                        {
                            bestGearV = kvp.Value;
                            bestGear = kvp.Key;
                        }
                    }

                    var actualRpm = this.Drivetrain.GearRatios[bestGear] * speed;

                    var reqThr = this.Drivetrain.CalculateThrottleByPower(actualRpm, load * maxPwrAvailable);
                    var thrScale = reqThr / Math.Max(load, 0.1);
                    if (thrScale > 1.5)
                    {
                        thrScale = 1.5;
                    }

                    this.tableGear[speed].Add(load, bestGear + 1);
                    this.tableThrottle[speed].Add(load, thrScale);
                }
            }
        }
    }
}