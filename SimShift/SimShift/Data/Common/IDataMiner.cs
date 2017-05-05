using System;
using System.Diagnostics;

using SimShift.Services;

namespace SimShift.Data.Common
{
    public interface IDataMiner
    {
        Process ActiveProcess { get; set; }

        string Application { get; }

        EventHandler DataReceived { get; set; }

        bool EnableWeirdAntistall { get; }

        bool IsActive { get; set; }

        string Name { get; }

        bool RunEvent { get; set; }

        bool Running { get; set; }

        bool SelectManually { get; }

        bool SupportsCar { get; }

        IDataDefinition Telemetry { get; }

        bool TransmissionSupportsRanges { get; }

        void EvtStart();

        void EvtStop();

        void Write<T>(TelemetryChannel cameraHorizon, T i);
    }
}