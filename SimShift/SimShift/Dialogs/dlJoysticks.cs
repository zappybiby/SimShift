using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SimShift.Entities;
using SimShift.Services;

namespace SimShift.Dialogs
{
    public partial class dlJoysticks : Form
    {
        public List<ucJoystickChannel> controlsIn = new List<ucJoystickChannel>();

        public List<ucJoystickChannel> controlsOut = new List<ucJoystickChannel>();

        public List<ucJoystickChannel> joysticks = new List<ucJoystickChannel>();

        private Timer _mCalibrateButton;

        private Timer _mUpdateJoysticks;

        int buttonId = 0;

        public dlJoysticks()
        {
            this.InitializeComponent();

            this._mUpdateJoysticks = new Timer();
            this._mUpdateJoysticks.Interval = 25;
            this._mUpdateJoysticks.Tick += this._mUpdateJoysticks_Tick;
            this._mUpdateJoysticks.Start();

            Main.Setup();

            for (int c_ = 0; c_ < (int) JoyControls.NUM_OF_CONTROLS; c_++)
            {
                JoyControls c = (JoyControls) c_;

                var ucIn = new ucJoystickChannel(c, true);
                var ucOut = new ucJoystickChannel(c, false);

                ucIn.Location = new Point(3, 23 + ucIn.Height * c_);
                ucOut.Location = new Point(3, 23 + ucOut.Height * c_);

                this.controlsIn.Add(ucIn);
                this.controlsOut.Add(ucOut);

                this.gbIn.Controls.Add(ucIn);
                this.gbOut.Controls.Add(ucOut);

                // add to combobox
                this.cbControl.Items.Add(((int) c).ToString() + ", " + c.ToString());
            }

            var a = 0;
            for (a = 0; a < 8; a++)
            {
                var uc = new ucJoystickChannel(true, a);
                uc.Location = new Point(3, 23 + uc.Height * a);

                this.joysticks.Add(uc);
                this.gbController.Controls.Add(uc);
            }

            for (int b = 0; b < 32; b++)
            {
                var uc = new ucJoystickChannel(false, b);
                uc.Location = new Point(3, 23 + uc.Height * (a + b));

                this.joysticks.Add(uc);
                this.gbController.Controls.Add(uc);
            }
        }

        private void _mUpdateJoysticks_Tick(object sender, EventArgs e)
        {
            foreach (var c in this.controlsIn)
            {
                c.Tick();
            }

            foreach (var c in this.controlsOut)
            {
                c.Tick();
            }

            foreach (var c in this.joysticks)
            {
                c.Tick();
            }
        }

        private void btDoCal_Click(object sender, EventArgs e)
        {
            if (this._mCalibrateButton == null)
            {
                try
                {
                    this.buttonId = int.Parse(this.cbControl.SelectedItem.ToString().Split(",".ToCharArray()).FirstOrDefault());
                }
                catch (Exception)
                {
                    MessageBox.Show("Cannot parse button");
                }

                if (Main.Running)
                {
                    MessageBox.Show("This will stop main service");
                    Main.Stop();
                    for (int i = 0; i < (int) JoyControls.NUM_OF_CONTROLS; i++)
                    {
                        Main.SetButtonOut((JoyControls) i, false);
                    }
                }

                int buttonState = 0;
                this._mCalibrateButton = new Timer();
                this._mCalibrateButton.Interval = 1500;
                this._mCalibrateButton.Tick += (o, args) =>
                    {
                        if (buttonState == 0)
                        {
                            buttonState = 1;
                            Main.SetAxisOut((JoyControls) this.buttonId, 1);
                            Main.SetButtonOut((JoyControls) this.buttonId, true);
                        }
                        else if (buttonState == 1)
                        {
                            buttonState = 2;
                            Main.SetAxisOut((JoyControls) this.buttonId, 0.5);
                            Main.SetButtonOut((JoyControls) this.buttonId, true);
                        }
                        else
                        {
                            buttonState = 0;
                            Main.SetAxisOut((JoyControls) this.buttonId, 0);
                            Main.SetButtonOut((JoyControls) this.buttonId, false);
                        }
                    };
                this._mCalibrateButton.Start();

                this.btDoCal.Text = "Stop calibration";
            }
            else
            {
                this._mCalibrateButton.Stop();
                this._mCalibrateButton = null;

                Main.SetAxisOut((JoyControls) this.buttonId, 0);
                Main.SetButtonOut((JoyControls) this.buttonId, false);

                this.btDoCal.Text = "Toggle for calibration";
            }
        }
    }
}