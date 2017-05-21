using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;

namespace SimShift.Controllers
{
    public delegate void JoystickButtonPress(JoystickInput joystickDevice, int button, bool state);

    public delegate void JoystickButtonEvent(JoystickInput joystickDevice, int button);

    public class JoystickInput
    {
        public JoystickButtonEvent Press;

        public JoystickButtonEvent Release;

        public JoystickButtonPress State;

        private readonly double[] _axisState = new double[0];

        private readonly bool[] _buttonState = new bool[0];

        private JOYINFOEX _joyInfo;

        private JoystickInputDevice dev;

        private Timer joystickUpdate;

        private int pov;

        public JoystickInput(JoystickInputDevice dev)
        {
            this.dev = dev;

            this.joystickUpdate = new Timer();
            this.joystickUpdate.Interval = 10;
            this.joystickUpdate.Elapsed += this.JoystickUpdateTick;
            this.joystickUpdate.Start();

            this._axisState = new double[6];
            for (int i = 0; i < 6; i++)
            {
                this._axisState[i] = 0;
            }

            this._buttonState = new bool[32];
            for (int i = 0; i < 32; i++)
            {
                this._buttonState[i] = false;
            }

            this._joyInfo.dwSize = Marshal.SizeOf(this._joyInfo);
            this._joyInfo.dwFlags = JoystickFlags.JOY_RETURNALL;
        }

        public Dictionary<int, string> AxisNames => this.dev.AxisNames;

        public double GetAxis(int id)
        {
            return id < this._axisState.Length ? this._axisState[id] : 0;
        }

        public bool GetButton(int id)
        {
            return id < this._buttonState.Length && this._buttonState[id];
        }

        public bool GetPov(int i)
        {
            // 0 = left, 1 = top, 2 = right, 3 = bottom
            int thruthtable = 0;

            // bit 1 = left
            // bit 2 = top
            // bit 3 = right
            // bit 4 = bottom
            switch (this.pov)
            {
                case 0xFFFF:
                    thruthtable = 0x00;
                    break;

                case 27000:
                    thruthtable = 0x01;
                    break;

                case 31500:
                    thruthtable = 0x03;
                    break;

                case 0:
                    thruthtable = 0x02;
                    break;

                case 4500:
                    thruthtable = 0x06;
                    break;

                case 9000:
                    thruthtable = 0x04;
                    break;

                case 13500:
                    thruthtable = 0x0C;
                    break;

                case 18000:
                    thruthtable = 0x08;
                    break;

                case 22500:
                    thruthtable = 0x09;
                    break;
            }
            return (thruthtable & (1 << i)) != 0;
        }

        private void JoystickUpdateTick(object sender, EventArgs e)
        {
            JoystickMethods.joyGetPosEx(this.dev.id, out this._joyInfo);

            this._axisState[0] = this._joyInfo.dwXpos;
            this._axisState[1] = this._joyInfo.dwYpos;
            this._axisState[2] = this._joyInfo.dwZpos;
            this._axisState[3] = this._joyInfo.dwRpos;
            this._axisState[4] = this._joyInfo.dwUpos;
            this._axisState[5] = this._joyInfo.dwVpos;

            this.pov = this._joyInfo.dwPOV;

            // Take all button inputs.
            for (int i = 0; i < 32; i++)
            {
                var bitmask = this._joyInfo.dwButtons & ((int) Math.Pow(2, i));
                if (bitmask != 0)
                {
                    // Pressed
                    if (!this._buttonState[i])
                    {
                        // EVENT press
                        if (this.State != null)
                        {
                            this.State(this, i, true);
                        }

                        if (this.Press != null)
                        {
                            this.Press(this, i);
                        }
                    }

                    this._buttonState[i] = true;
                }
                else
                {
                    if (this._buttonState[i])
                    {
                        // EVENT release
                        if (this.State != null)
                        {
                            this.State(this, i, false);
                        }

                        if (this.Release != null)
                        {
                            this.Release(this, i);
                        }
                    }

                    this._buttonState[i] = false;
                }
            }
        }
    }
}