using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using SimShift.Services;
using SimShift.Utils;

namespace SimShift.Entities
{
    public class Profiles : IConfigurable
    {
        public List<Profile> Loaded = new List<Profile>();

        public List<string> Unloaded = new List<string>();

        public Profiles(string app, string car)
        {
            this.UniqueID = string.Format("{0}.{1}", app, car);
            this.MasterFile = string.Format("Settings/Profiles/{0}.{1}.Master.ini", app, car);
            this.PatternFile = string.Format("Settings/Profiles/{0}.{1}.{2}.ini", app, car, "{0}");

            if (File.Exists(this.MasterFile) == false)
            {
                Debug.WriteLine("Cannot find " + this.MasterFile + " - creating default Performance");

                this.ResetParameters();
                var performanceProfile = new Profile(this, "Performance");
                this.Loaded.Add(performanceProfile);
                var efficiencyProfile = new Profile(this, "Efficiency");
                this.Loaded.Add(efficiencyProfile);
                var economyProfile = new Profile(this, "Economy");
                this.Loaded.Add(economyProfile);

                Main.Store(this.ExportParameters(), this.MasterFile);
            }

            Main.Load(this, this.MasterFile);
        }

        public event EventHandler LoadedProfile;

        public IEnumerable<string> AcceptsConfigs => new[] { "Profiles" };

        public string Active { get; private set; }

        public string MasterFile { get; private set; }

        public string PatternFile { get; private set; }

        public string UniqueID { get; private set; }

        public void ApplyParameter(IniValueObject obj)
        {
            switch (obj.Key)
            {
                case "Load":
                    var p = new Profile(this, obj.ReadAsString());
                    if (p.Loaded == false)
                    {
                        this.Unloaded.Add(obj.ReadAsString());
                    }
                    else
                    {
                        this.Loaded.Add(p);
                    }

                    break;

                case "Unload":
                    this.Unloaded.Add(obj.ReadAsString());
                    break;
            }
        }

        public IEnumerable<IniValueObject> ExportParameters()
        {
            var obj = new List<IniValueObject>();

            foreach (var l in this.Loaded)
            {
                obj.Add(new IniValueObject(this.AcceptsConfigs, "Load", l.Name));
            }

            foreach (var l in this.Unloaded)
            {
                obj.Add(new IniValueObject(this.AcceptsConfigs, "Load", l));
            }

            return obj;
        }

        public void Load(string profile, float staticMass)
        {
            if (this.Loaded.Any(x => x.Name == profile))
            {
                Debug.WriteLine("Loading profile " + profile);
                this.Active = profile;
                this.Loaded.FirstOrDefault(x => x.Name == profile).Load(staticMass);

                if (this.LoadedProfile != null)
                {
                    this.LoadedProfile(this, new EventArgs());
                }
            }
        }

        public void ResetParameters()
        {
            this.Loaded.Clear();
            this.Unloaded.Clear();
        }
    }
}