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
    public partial class ucJoystickChannel : UserControl
    {
        private JoyControls ctrl;

        private int index;

        private bool isAxis;

        private bool isJoystickInput;

        public ucJoystickChannel(JoyControls c, bool inout)
        {
            this.InitializeComponent();
            this.lblControl.Text = c.ToString();
            this.pbVal.Value = 0;
            this.Input = inout;
            this.ctrl = c;
        }

        public ucJoystickChannel(bool axis, int i)
        {
            this.InitializeComponent();
            this.isAxis = axis;
            this.index = i;
            this.isJoystickInput = true;

            this.lblControl.Text = (axis ? "Axis" : "Button") + " " + i;
            this.pbVal.Value = 0;
            this.Input = true;
        }

        public bool Input { get; private set; }

        public void Tick()
        {
            int output = 0;

            if (this.isJoystickInput)
            {
                if (this.isAxis)
                {
                    output = (int) (100 * Main.Controller.GetAxis(this.index) / 0x7FFF);
                }
                else if (this.index >= 20)
                {
                    output = Main.Controller.GetPov(this.index - 20) ? 100 : 0;
                }
                else
                {
                    output = Main.Controller.GetButton(this.index) ? 100 : 0;
                }
            }
            else
            {
                var axisValue = this.Input ? Main.GetAxisIn(this.ctrl) : Main.GetAxisOut(this.ctrl);
                var buttonValue = this.Input ? Main.GetButtonIn(this.ctrl) : Main.GetButtonOut(this.ctrl);

                output = (int) Math.Max(axisValue * 100, buttonValue ? 100 : 0);
            }

            if (double.IsNaN(output))
            {
                output = 0;
            }

            if (output > 99)
            {
                output = 99;
            }

            if (output < 0)
            {
                output = 0;
            }

            this.pbVal.Value = (int) output + 1;
            this.pbVal.Value = (int) output;
        }
    }
}