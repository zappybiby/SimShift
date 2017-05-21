using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using SimShift.Data;
using SimShift.Data.Common;
using SimShift.Entities;

namespace SimShift.Services
{
    public class ControlChain
    {
        private string ActiveSimulator;

        private List<JoyControls> Axis = new List<JoyControls>();

        private Dictionary<JoyControls, Dictionary<string, double>> axisProgression = new Dictionary<JoyControls, Dictionary<string, double>>();

        private List<JoyControls> Buttons = new List<JoyControls>();

        private List<IControlChainObj> chain = new List<IControlChainObj>();

        public ControlChain()
        {
            this.chain.Add(new ThrottleMapping());
            this.chain.Add(Main.LaneAssistance);
            this.chain.Add(Main.ACC);
            this.chain.Add(Main.Speedlimiter);

            // chain.Add(Main.CruiseControl);
            if (Main.VST)
            {
                this.chain.Add(Main.VariableSpeedControl);
            }
            else
            {
                this.chain.Add(Main.Transmission);
            }

            this.chain.Add(Main.Antistall);
            this.chain.Add(Main.TractionControl);

            // chain.Add(Main.LaunchControl);
            this.chain.Add(Main.ProfileSwitcher);
            this.chain.Add(Main.DrivetrainCalibrator);
            this.chain.Add(Main.CameraHorizon);
            this.chain.Add(new EarlyClutch());
            this.chain.Add(new WheelTorqueLimiter());

            // chain.Add(new Ets2PowerMeter());
            this.chain.Add(new Dashboard());

            // chain.Add(Main.TransmissionCalibrator);
            this.Axis.Add(JoyControls.Steering);
            this.Axis.Add(JoyControls.Throttle);
            this.Axis.Add(JoyControls.Brake);
            this.Axis.Add(JoyControls.Clutch);
            this.Axis.Add(JoyControls.CameraHorizon);

            this.Buttons.Add(JoyControls.Gear1);
            this.Buttons.Add(JoyControls.Gear2);
            this.Buttons.Add(JoyControls.Gear3);
            this.Buttons.Add(JoyControls.Gear4);
            this.Buttons.Add(JoyControls.Gear5);
            this.Buttons.Add(JoyControls.Gear6);
            this.Buttons.Add(JoyControls.Gear7);
            this.Buttons.Add(JoyControls.Gear8);
            this.Buttons.Add(JoyControls.GearR);
            this.Buttons.Add(JoyControls.GearRange1);
            this.Buttons.Add(JoyControls.GearRange2);
            this.Buttons.Add(JoyControls.GearUp);
            this.Buttons.Add(JoyControls.GearDown);
            this.Buttons.Add(JoyControls.CruiseControlMaintain);
            this.Buttons.Add(JoyControls.CruiseControlUp);
            this.Buttons.Add(JoyControls.CruiseControlDown);
            this.Buttons.Add(JoyControls.CruiseControlOnOff);

            foreach (var a in this.Axis)
            {
                this.axisProgression.Add(a, new Dictionary<string, double>());
                foreach (var m in this.chain)
                {
                    if (m != null)
                    {
                        this.axisProgression[a].Add(m.GetType().Name, 0);
                    }
                }
            }
        }

        public Dictionary<JoyControls, Dictionary<string, double>> AxisProgression => this.axisProgression;

        public IEnumerable<IControlChainObj> Chain => this.chain;

        public void Tick(IDataMiner data)
        {
            // We take all controller input
            var buttonValues = this.Buttons.ToDictionary(c => c, Main.GetButtonIn);
            var axisValues = this.Axis.ToDictionary(c => c, Main.GetAxisIn);

            foreach (var obj in this.chain)
            {
                obj.TickTelemetry(data);
            }

            // Put it serially through each control block
            // Each time a block requires a control, it receives the current value of that control
            foreach (var obj in this.chain.Where(this.FilterSimulators))
            {
                buttonValues = buttonValues.ToDictionary(c => c.Key, k => obj.Requires(k.Key) ? obj.GetButton(k.Key, k.Value) : k.Value);
                axisValues = axisValues.ToDictionary(c => c.Key, k => obj.Requires(k.Key) ? obj.GetAxis(k.Key, k.Value) : k.Value);

                foreach (var kvp in axisValues)
                {
                    this.axisProgression[kvp.Key][obj.GetType().Name] = kvp.Value;
                }

                obj.TickControls();
            }

            // And then put them onto our own controller.
            foreach (var b in buttonValues)
            {
                Main.SetButtonOut(b.Key, b.Value);
            }

            foreach (var b in axisValues)
            {
                var v = b.Value;
                if (v > 1)
                {
                    v = 1;
                }

                if (v < 0)
                {
                    v = 0;
                }

                Main.SetAxisOut(b.Key, v);
            }
        }

        private bool FilterSimulators(IControlChainObj arg)
        {
            if (arg.SimulatorsOnly.Any())
            {
                if (!arg.SimulatorsOnly.Contains(this.ActiveSimulator))
                {
                    return false;
                }
            }

            if (arg.SimulatorsBan.Any())
            {
                if (arg.SimulatorsBan.Contains(this.ActiveSimulator))
                {
                    return false;
                }
            }

            return arg.Enabled;
        }
    }
}