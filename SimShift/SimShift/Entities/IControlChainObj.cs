using System.Collections.Generic;

using SimShift.Data.Common;
using SimShift.Services;

namespace SimShift.Entities
{
    public interface IControlChainObj
    {
        bool Active { get; }

        bool Enabled { get; }

        IEnumerable<string> SimulatorsBan { get; }

        IEnumerable<string> SimulatorsOnly { get; }

        double GetAxis(JoyControls c, double val);

        bool GetButton(JoyControls c, bool val);

        bool Requires(JoyControls c);

        void TickControls();

        void TickTelemetry(IDataMiner data);
    }
}