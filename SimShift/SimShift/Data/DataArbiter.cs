using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;

using SimShift.Data.Common;

namespace SimShift.Data
{
    public class DataArbiter
    {
        public IDataMiner Active = new Ets2DataMiner(); // { get; private set; }

        public int verbose = 0;

        private readonly List<IDataMiner> miners = new List<IDataMiner>();

        private Timer _checkApplications;

        private bool caBusy = false;

        private string lastCar;

        public DataArbiter()
        {
            this.AutoMode = true;

            this.miners.Add(new Ets2DataMiner());

            // miners.Add(new Tdu2DataMiner());
            this.miners.ForEach(
                app =>
                    {
                        app.DataReceived += (s, e) =>
                            {
                                if (app == this.Active)
                                {
                                    if (this.verbose > 0)
                                    {
                                        Debug.WriteLine(string.Format("[Data] Spd: {0:000.0}kmh Gear: {1} RPM: {2:0000}rpm Throttle: {3:0.000}", app.Telemetry.Speed, app.Telemetry.Gear, app.Telemetry.EngineRpm, app.Telemetry.Throttle));
                                    }

                                    this.Telemetry = app.Telemetry;

                                    if (this.lastCar != this.Telemetry.Car && this.CarChanged != null && app.SupportsCar)
                                    {
                                        this.lastCar = this.Telemetry.Car;
                                        Debug.WriteLine("New car:" + this.Telemetry.Car);
                                        this.CarChanged(s, e);
                                    }

                                    if (!app.SupportsCar)
                                    {
                                        this.Telemetry.Car = this.ManualCar;
                                    }

                                    if (this.DataReceived != null)
                                    {
                                        this.DataReceived(s, e);
                                    }

                                    this.lastCar = this.Telemetry.Car;
                                }
                            };
                    });

            this._checkApplications = new Timer();
            this._checkApplications.Interval = 1000;
            this._checkApplications.Elapsed += this._checkApplications_Elapsed;
        }

        public event EventHandler AppActive;

        public event EventHandler AppInactive;

        public event EventHandler CarChanged;

        public event EventHandler DataReceived;

        public bool AutoMode { get; private set; }

        public string ManualCar { get; private set; }

        public IEnumerable<IDataMiner> Miners => this.miners;

        public IDataDefinition Telemetry { get; private set; }

        public void AutoSelectApp()
        {
            this.AutoMode = true;
            if (this.Active != null && this.Active.IsActive)
            {
                if (this.AppInactive != null)
                {
                    this.AppInactive(this, new EventArgs());
                }
            }

            this.Active = null;
        }

        public void ChangeCar(string newCar)
        {
            this.ManualCar = newCar;

            Debug.WriteLine("New car:" + this.Telemetry.Car);
            this.CarChanged(this, new EventArgs());
        }

        public void ManualSelectApp(IDataMiner app)
        {
            this.AutoMode = false;
            if (this.miners.Contains(app))
            {
                if (this.Active != null && this.Active.IsActive)
                {
                    if (this.AppInactive != null)
                    {
                        this.AppInactive(this, new EventArgs());
                    }
                }

                this.Active = app;
            }
        }

        public void Run()
        {
            this._checkApplications.Start();
        }

        void _checkApplications_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.caBusy)
            {
                return;
            }

            this.caBusy = true;

            var prcsList = Process.GetProcesses();

            // Do it for the manual selected sim
            if (!this.AutoMode)
            {
                var app = this.Active;
                if (app == null)
                {
                    this.caBusy = false;
                    return;
                }

                // Search for the process
                bool wasRuning = app.Running;
                app.Running = prcsList.Any(x => x.ProcessName.ToLower() == app.Application.ToLower());
                app.RunEvent = app.Running != wasRuning;
                app.ActiveProcess = prcsList.FirstOrDefault(x => x.ProcessName.ToLower() == app.Application.ToLower());

                if (app.RunEvent)
                {
                    if (app.Running)
                    {
                        app.EvtStart();
                        if (this.AppActive != null)
                        {
                            this.AppActive(this, new EventArgs());
                        }
                        else
                        {
                            app.EvtStop();
                            if (this.AppInactive != null)
                            {
                                this.AppInactive(this, new EventArgs());
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var app in this.miners.Where(x => !x.SelectManually))
                {
                    bool wasRuning = app.Running;
                    app.Running = prcsList.Any(x => x.ProcessName.ToLower() == app.Application.ToLower());
                    app.RunEvent = app.Running != wasRuning;
                    app.ActiveProcess = prcsList.FirstOrDefault(x => x.ProcessName.ToLower() == app.Application.ToLower());

                    if (app.RunEvent && app.IsActive && app.Running == false)
                    {
                        app.EvtStop();
                        if (this.AppInactive != null)
                        {
                            this.AppInactive(this, new EventArgs());
                        }
                    }

                    app.IsActive = false;
                }

                if (this.miners.Where(x => !x.SelectManually).Any(x => x.Running))
                {
                    // Conflict?
                    this.Active = this.miners.Count(x => x.Running) != 1 ? null : this.miners.Where(x => !x.SelectManually).FirstOrDefault(x => x.Running);
                    if (this.Active != null)
                    {
                        this.Active.IsActive = true;

                        // TODO: This seems buggy way..
                        if (this.Active.RunEvent)
                        {
                            this.Active.EvtStart();
                            if (this.AppActive != null)
                            {
                                this.AppActive(this, new EventArgs());
                            }
                        }
                    }
                }
            }

            this.caBusy = false;
        }
    }
}