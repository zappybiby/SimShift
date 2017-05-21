using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using SimShift.Services;
using SimShift.Utils;

namespace SimShift.Entities
{
    public class Profile : IConfigurable
    {
        public bool Loaded = false;

        private float TrailerMass = 0;

        public Profile(Profiles car, string file)
        {
            this.Name = file;
            this.Car = car;

            var phFile = string.Format(car.PatternFile, file);

            if (!File.Exists(phFile))
            {
                Debug.WriteLine("Cannot find file " + phFile + " - creating defaults");
                this.Antistall = "easy";
                this.CruiseControl = "easy";
                switch (file)
                {
                    case "Performance":
                        this.ShiftCurve = "Performance.5kmh.slow";
                        break;
                    case "Efficiency":
                        this.ShiftCurve = "Efficiency.5kmh.slow";
                        break;
                    case "Economy":
                        this.ShiftCurve = "Economy.5kmh.slow";
                        break;
                }
                this.SpeedLimiter = "nolimit";
                this.TractionControl = "notc";

                this.ShiftPattern = new List<ConfigurableShiftPattern>();
                this.ShiftPattern.Add(new ConfigurableShiftPattern("up_1thr", "normal"));
                this.ShiftPattern.Add(new ConfigurableShiftPattern("up_0thr", "normal"));
                this.ShiftPattern.Add(new ConfigurableShiftPattern("down_1thr", "normal"));
                this.ShiftPattern.Add(new ConfigurableShiftPattern("down_0thr", "normal"));

                var iniExport = this.ExportParameters();
                Main.Store(iniExport, phFile);

                this.Loaded = true;
            }
            else
            {
                this.Loaded = true;
            }

            Main.Load(this, phFile);
        }

        public IEnumerable<string> AcceptsConfigs => new[] { "Profiles" };

        public string Antistall { get; private set; }

        public string CruiseControl { get; private set; }

        public string Name { get; private set; }

        public string ShiftCurve { get; private set; }

        public List<ConfigurableShiftPattern> ShiftPattern { get; private set; }

        public string SpeedLimiter { get; private set; }

        public string TractionControl { get; private set; }

        private Profiles Car { get; set; }

        public void ApplyParameter(IniValueObject obj)
        {
            switch (obj.Key)
            {
                case "Antistall":
                    this.Antistall = obj.ReadAsString();
                    break;
                case "CruiseControl":
                    this.CruiseControl = obj.ReadAsString();
                    break;
                case "ShiftCurve":
                    this.ShiftCurve = obj.ReadAsString();
                    break;
                case "SpeedLimiter":
                    this.SpeedLimiter = obj.ReadAsString();
                    break;
                case "TractionControl":
                    this.TractionControl = obj.ReadAsString();
                    break;
                case "ShiftPattern":
                    var file = obj.ReadAsString(2);
                    var region = obj.ReadAsString(0).ToLower() + "_" + obj.ReadAsString(1) + "thr";
                    this.ShiftPattern.Add(new ConfigurableShiftPattern(region, file));
                    break;
            }
        }

        public IEnumerable<IniValueObject> ExportParameters()
        {
            List<IniValueObject> obj = new List<IniValueObject>();
            obj.Add(new IniValueObject(this.AcceptsConfigs, "Antistall", this.Antistall));
            obj.Add(new IniValueObject(this.AcceptsConfigs, "CruiseControl", this.CruiseControl));
            obj.Add(new IniValueObject(this.AcceptsConfigs, "ShiftCurve", this.ShiftCurve));
            obj.Add(new IniValueObject(this.AcceptsConfigs, "SpeedLimiter", this.SpeedLimiter));
            obj.Add(new IniValueObject(this.AcceptsConfigs, "TractionControl", this.TractionControl));
            foreach (var s in this.ShiftPattern)
            {
                if (s.Region.IndexOf("_") < 0)
                {
                    continue;
                }

                var part = s.Region.Substring(0, s.Region.IndexOf("_"));
                var thr = s.Region.Substring(s.Region.IndexOf("_") + 1);
                obj.Add(new IniValueObject(this.AcceptsConfigs, "ShiftPattern", string.Format("({0},{1},{2})", part, thr, s.File)));
            }

            return obj;
        }

        public void Load(float staticMass)
        {
            Main.Transmission.StaticMass = staticMass;
            this.TrailerMass = staticMass;

            Main.Load(Main.Antistall, "Settings/Antistall/" + this.Antistall + ".ini");
            Main.Load(Main.CruiseControl, "Settings/CruiseControl/" + this.CruiseControl + ".ini");

            Main.Drivetrain.File = "Settings/Drivetrain/" + this.Car.UniqueID + ".ini";
            Main.Drivetrain.Calibrated = Main.Load(Main.Drivetrain, "Settings/Drivetrain/" + this.Car.UniqueID + ".ini");

            Main.Load(Main.Transmission, "Settings/ShiftCurve/" + this.ShiftCurve + ".ini");
            Main.Load(Main.Speedlimiter, "Settings/SpeedLimiter/" + this.SpeedLimiter + ".ini");

            Main.TractionControl.File = this.TractionControl;
            Main.Load(Main.TractionControl, "Settings/TractionControl/" + this.TractionControl + ".ini");
        }

        public void ResetParameters()
        {
            this.ShiftPattern = new List<ConfigurableShiftPattern>();
        }
    }
}