using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SimShift.Dialogs;
using SimShift.Entities;
using SimShift.Models;
using SimShift.Services;

namespace SimShift.Simulation
{
    public class SimulationEnvironment
    {
        private IDrivetrain drivetrain;

        private ShifterTableConfiguration shifter;

        private double Speed = 0.0f;

        public SimulationEnvironment()
        {
            this.drivetrain = new Ets2Drivetrain();
            Main.Load(this.drivetrain, "Settings/Drivetrain/eurotrucks2.scania.g7ld6x2.ini");
            this.shifter = new ShifterTableConfiguration(ShifterTableConfigurationDefault.Performance, this.drivetrain, 1, 0);

            this.Speed = 30 / 3.6;
            StringBuilder sim = new StringBuilder();
            for (int k = 0; k < 10000; k++)
            {
                this.Tick();
                sim.AppendLine(k + "," + this.Speed);
            }

            File.WriteAllText("./sim.csv", sim.ToString());
        }

        public void Tick()
        {
            // Model : engine
            var topGear = this.drivetrain.Gears - 1;
            var engineRpm = this.drivetrain.CalculateRpmForSpeed(topGear, (float) this.Speed);
            var enginePower = this.drivetrain.CalculatePower(engineRpm, 1.0f);

            // Model: aero
            var aero = this.Speed * this.Speed * 0.5;

            var acceleration = enginePower - aero;
            acceleration /= 100;
            this.Speed += acceleration * 1.0 / 100;
        }
    }
}