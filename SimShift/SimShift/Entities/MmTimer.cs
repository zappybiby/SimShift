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
            Period = period;
        }

        private delegate void TimerEventHandler(int id, int msg, IntPtr user, int dw1, int dw2);

        public event EventHandler Tick;

        public bool Enabled
        {
            get
            {
                return id != 0;
            }
        }

        public uint Period
        {
            get
            {
                return period;
            }
            set
            {
                if (value <= 0) return;
                period = value;
            }
        }

        public void Start()
        {
            if (id != 0) return;
            if (Check() == false)
            {
                _rescueTimer = new Timer(Period);
                _rescueTimer.Elapsed += (o, s) => TimerHandler(0, 0, IntPtr.Zero, 0, 0);
                _rescueTimer.Start();
                usedMMTimer = false;
            }
            else
            {
                handler = new TimerEventHandler(TimerHandler);
                id = timeSetEvent(period, 1, handler, IntPtr.Zero, EVENT_TYPE);
                usedMMTimer = true;
            }
        }

        public void Stop()
        {
            if (usedMMTimer)
            {
                if (id == 0) return;
                timeKillEvent(id);
                timeEndPeriod(resolution);
                id = 0;
            }
            else
            {
                _rescueTimer.Stop();
                _rescueTimer = null;
            }
        }

        protected bool Check()
        {
            var err = timeGetDevCaps(ref mmCapabilities, (uint) Marshal.SizeOf(mmCapabilities));
            if (err == TIMERR_NOCANDO) return false;
            if (mmCapabilities.wPeriodMin > period || mmCapabilities.wPeriodMax < period) return false;
            resolution = mmCapabilities.wPeriodMin;
            err = timeBeginPeriod(resolution);
            return (err == TIMERR_NOERROR) ? true : false;
        }

        [DllImport("winmm.dll")]
        private static extern UInt32 timeBeginPeriod(UInt32 msec);

        [DllImport("winmm.dll")]
        private static extern UInt32 timeEndPeriod(UInt32 msec);

        [DllImport("winmm.dll")]
        private static extern UInt32 timeGetDevCaps(ref LPTIMECAPS ptc, UInt32 cbtc);

        [DllImport("winmm.dll")]
        private static extern UInt32 timeKillEvent(UInt32 id);

        [DllImport("winmm.dll")]
        private static extern UInt32 timeSetEvent(UInt32 delay, UInt32 resolution, TimerEventHandler handler, IntPtr user, UInt32 eventType);

        private void TimerHandler(int id, int msg, IntPtr user, int dw1, int dw2)
        {
            if (Tick != null) Tick(this, new EventArgs());
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct LPTIMECAPS
        {
            public uint wPeriodMin;

            public uint wPeriodMax;
        };
    }
}