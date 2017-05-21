using System;
using System.Runtime.InteropServices;
using System.Timers;

namespace SimShift.Entities
{
    public class MmTimer
    {
        private const int EVENT_TYPE = TIME_PERIODIC;

        private const int TIME_PERIODIC = 1;

        private const int TIMERR_BASE = 96;

        private const int TIMERR_NOCANDO = TIMERR_BASE + 1;

        private const int TIMERR_NOERROR = 0;

        protected LPTIMECAPS mmCapabilities = new LPTIMECAPS();

        private Timer _rescueTimer;

        private TimerEventHandler handler;

        private uint id = 0;

        private uint period = 1;

        private uint resolution = 1;

        private bool usedMMTimer = false;

        public MmTimer(uint period)
        {
            this.Period = period;
        }

        private delegate void TimerEventHandler(int id, int msg, IntPtr user, int dw1, int dw2);

        public event EventHandler Tick;

        public bool Enabled => this.id != 0;

        public uint Period
        {
            get => this.period;

            set
            {
                if (value <= 0)
                {
                    return;
                }

                this.period = value;
            }
        }

        public void Start()
        {
            if (this.id != 0)
            {
                return;
            }

            if (this.Check() == false)
            {
                this._rescueTimer = new Timer(this.Period);
                this._rescueTimer.Elapsed += (o, s) => this.TimerHandler(0, 0, IntPtr.Zero, 0, 0);
                this._rescueTimer.Start();
                this.usedMMTimer = false;
            }
            else
            {
                this.handler = new TimerEventHandler(this.TimerHandler);
                this.id = timeSetEvent(this.period, 1, this.handler, IntPtr.Zero, EVENT_TYPE);
                this.usedMMTimer = true;
            }
        }

        public void Stop()
        {
            if (this.usedMMTimer)
            {
                if (this.id == 0)
                {
                    return;
                }

                timeKillEvent(this.id);
                timeEndPeriod(this.resolution);
                this.id = 0;
            }
            else
            {
                this._rescueTimer.Stop();
                this._rescueTimer = null;
            }
        }

        protected bool Check()
        {
            var err = timeGetDevCaps(ref this.mmCapabilities, (uint) Marshal.SizeOf(this.mmCapabilities));
            if (err == TIMERR_NOCANDO)
            {
                return false;
            }

            if (this.mmCapabilities.wPeriodMin > this.period || this.mmCapabilities.wPeriodMax < this.period)
            {
                return false;
            }

            this.resolution = this.mmCapabilities.wPeriodMin;
            err = timeBeginPeriod(this.resolution);
            return (err == TIMERR_NOERROR) ? true : false;
        }

        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint msec);

        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint msec);

        [DllImport("winmm.dll")]
        private static extern uint timeGetDevCaps(ref LPTIMECAPS ptc, uint cbtc);

        [DllImport("winmm.dll")]
        private static extern uint timeKillEvent(uint id);

        [DllImport("winmm.dll")]
        private static extern uint timeSetEvent(uint delay, uint resolution, TimerEventHandler handler, IntPtr user, uint eventType);

        private void TimerHandler(int id, int msg, IntPtr user, int dw1, int dw2)
        {
            if (this.Tick != null)
            {
                this.Tick(this, new EventArgs());
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct LPTIMECAPS
        {
            public uint wPeriodMin;

            public uint wPeriodMax;
        };
    }
}