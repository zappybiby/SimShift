using System.Collections.Generic;

using SimShift.Utils;

namespace SimShift.Models
{
    public class GenericDrivetrain : IDrivetrain
    {
        private Dictionary<double, GenericEngineData> Engine = new Dictionary<double, GenericEngineData>();

        public GenericDrivetrain()
        {
            this.Calibrated = true;
        }

        public IEnumerable<string> AcceptsConfigs => new[] { "Engine", "Gearbox" };

        public bool Calibrated { get; set; }

        public string File { get; set; }

        public double[] GearRatios { get; set; }

        public virtual int Gears { get; set; }

        public double MaximumRpm { get; set; }

        public double StallRpm { get; set; }

        protected float GearReverse { get; set; }

        public virtual void ApplyParameter(IniValueObject obj)
        {
            if (obj.Group == "Engine")
            {
                switch (obj.Key)
                {
                    case "Idle":
                        this.StallRpm = obj.ReadAsFloat();
                        break;
                    case "Max":
                        this.MaximumRpm = obj.ReadAsFloat();
                        break;

                    case "Power":
                        this.Engine.Add(obj.ReadAsFloat(0), new GenericEngineData(obj.ReadAsFloat(1), obj.ReadAsFloat(2)));
                        break;
                }
            }
            else if (obj.Group == "Gearbox")
            {
                switch (obj.Key)
                {
                    case"Gears":
                        this.Gears = obj.ReadAsInteger();
                        this.GearRatios = new double[this.Gears];
                        break;

                    case "Gear":
                        this.GearRatios[obj.ReadAsInteger(0)] = obj.ReadAsFloat(1);
                        break;

                    case "GearR":
                        this.GearReverse = obj.ReadAsFloat();
                        break;
                }
            }
        }

        public virtual double CalculateFuelConsumption(double rpm, double throttle)
        {
            var f = throttle * rpm / (this.MaximumRpm / 2);
            if (rpm > this.MaximumRpm / 2)
            {
                return f * f * throttle;
            }
            else
            {
                return f * f * throttle;
            }
        }

        public double CalculateMaxPower()
        {
            var pwr = 0.0;
            var pwrRpm = 0.0;
            for (var rpm = 0; rpm < this.MaximumRpm; rpm += 100)
            {
                var p = this.CalculatePower(rpm, 1);
                if (p > pwr)
                {
                    pwr = p;
                    pwrRpm = rpm;
                }
            }

            return pwr;
        }

        public virtual double CalculatePower(double rpm, double throttle)
        {
            return throttle * rpm;
        }

        public double CalculateRpmForSpeed(int gear, float speed)
        {
            if (this.GearRatios == null || gear < 0 || gear >= this.GearRatios.Length)
            {
                return 0;
            }

            return speed * 3.6 * this.GearRatios[gear];
        }

        public double CalculateSpeedForRpm(int gear, float rpm)
        {
            if (this.GearRatios == null || gear < 0 || gear >= this.GearRatios.Length)
            {
                return 0;
            }

            return rpm / this.GearRatios[gear] / 3.6;
        }

        public double CalculateThrottleByPower(double rpm, double powerRequired)
        {
            // 1 Nm @ 1000rpm = 0.1904hp
            // 1 Hp @ 1000rpm = 5.2521Nm
            if (rpm == 0)
            {
                return 1;
            }

            double torqueRequired = powerRequired / (rpm / 1000) * (1 / 0.1904);
            if (torqueRequired == 0)
            {
                torqueRequired = 0.1;
            }

            return this.CalculateThrottleByTorque(rpm, torqueRequired);
        }

        public virtual double CalculateThrottleByTorque(double rpm, double torque)
        {
            var torqueP = this.CalculateTorqueP(rpm, 1);
            if (torque > torqueP)
            {
                return 1;
            }

            var torqueN = this.CalculateTorqueN(rpm);
            if (torque < torqueN)
            {
                return 0;
            }

            var t = torque / (torqueP - torqueN);
            return t;
        }

        public virtual double CalculateTorqueN(double rpm)
        {
            var lastKey = 0.0;
            foreach (var r in this.Engine.Keys)
            {
                if (r > rpm && lastKey < rpm)
                {
                    var dutyCycle = (rpm - lastKey) / (r - lastKey);
                    return (this.Engine[r].N - this.Engine[lastKey].N) * dutyCycle + this.Engine[lastKey].N;
                }
                else
                {
                    lastKey = r;
                }
            }

            return 0;
        }

        public virtual double CalculateTorqueP(double rpm, double throttle)
        {
            var lastKey = 0.0;
            foreach (var r in this.Engine.Keys)
            {
                if (r > rpm && lastKey < rpm)
                {
                    var dutyCycle = (rpm - lastKey) / (r - lastKey);
                    return (this.Engine[r].P - this.Engine[lastKey].P) * dutyCycle + this.Engine[lastKey].P;
                }
                else
                {
                    lastKey = r;
                }
            }

            return 0;
        }

        public virtual IEnumerable<IniValueObject> ExportParameters()
        {
            List<IniValueObject> obj = new List<IniValueObject>();

            obj.Add(new IniValueObject(new[] { "Engine" }, "Idle", this.StallRpm.ToString()));
            obj.Add(new IniValueObject(new[] { "Engine" }, "Max", this.MaximumRpm.ToString()));
            foreach (var frame in this.Engine)
            {
                obj.Add(new IniValueObject(new[] { "Engine" }, "Power", string.Format("({0},{1},{2})", frame.Key, frame.Value.N, frame.Value.P)));
            }

            obj.Add(new IniValueObject(new[] { "Gearbox" }, "Gears", this.Gears.ToString()));
            obj.Add(new IniValueObject(new[] { "Gearbox" }, "GearR", this.GearReverse.ToString()));
            for (int g = 0; g < this.Gears; g++)
            {
                obj.Add(new IniValueObject(new[] { "Gearbox" }, "Gear", string.Format("({0},{1})", g, this.GearRatios[g])));
            }

            return obj;
        }

        public virtual bool GotDamage(float damage)
        {
            // gears might go kaput in other games
            return false;
        }

        public virtual void ResetParameters()
        {
            this.Engine = new Dictionary<double, GenericEngineData>();
            this.StallRpm = 900;
            this.MaximumRpm = 2500;
        }

        internal struct GenericEngineData
        {
            public double N;

            public double P;

            public GenericEngineData(double n, double p)
            {
                this.N = n;
                this.P = p;
            }
        }
    }
}