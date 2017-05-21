using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;

using Ets2SdkClient;

using SimShift.Entities;

using SimTelemetry.Domain.Memory;

namespace SimShift.Data
{
    using SimShift.Data.Common;

    using MemoryReader = MemoryReader;

    public class Ets2DataMiner : IDataMiner
    {
        public List<Ets2Car> Cars = new List<Ets2Car>();

        /*** MyTelemetry data source & update control ***/
        private readonly MmTimer _telemetryUpdater = new MmTimer(10);

        private Process _ap;

        private MemoryProvider miner;

        private bool sdkBusy = false;

        private Ets2SdkTelemetry sdktel;

        private Socket server;

        public Ets2DataMiner()
        {
            this.server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            this.sdktel = new Ets2SdkTelemetry(10);
            this.sdktel.Data += (data, timestamp) =>
                {
                    this.MyTelemetry = data;

                    var veh = data.TruckId.StartsWith("vehicle.") ? data.TruckId.Substring(8) : data.TruckId;
                    var newData = new GenericDataDefinition(veh, data.Time / 1000000.0f, data.Paused, data.Drivetrain.Gear, data.Drivetrain.GearsForward, data.Drivetrain.EngineRpm, data.Drivetrain.Fuel, data.Controls.GameThrottle, data.Controls.GameBrake, data.Drivetrain.Speed);
                    this.Telemetry = newData;
                    if (this.DataReceived != null)
                    {
                        this.DataReceived(this, new EventArgs());
                    }
                };
            this.sdktel.Data += this.sdktel_Data;
        }

        public Process ActiveProcess
        {
            get => this._ap;

            set
            {
                this._ap = value;
                if (this.miner == null && this._ap != null)
                {
                    this.AdvancedMiner();
                }
            }
        }

        public string Application => "eurotrucks2";

        public EventHandler DataReceived { get; set; }

        public bool EnableWeirdAntistall => true;

        public bool IsActive { get; set; }

        public Ets2Telemetry MyTelemetry { get; private set; }

        public string Name => "Euro Truck Simulator 2";

        public bool RunEvent { get; set; }

        public bool Running { get; set; }

        public bool SelectManually => false;

        public bool SupportsCar => true;

        public IDataDefinition Telemetry { get; private set; }

        public bool TransmissionSupportsRanges => true;

        public void EvtStart()
        {
            this._telemetryUpdater.Start();
        }

        public void EvtStop()
        {
            this._telemetryUpdater.Stop();
        }

        public void Write<T>(TelemetryChannel cameraHorizon, T i)
        {
            // Not supported
        }

        private void AdvancedMiner()
        {
            var reader = new MemoryReader();
            reader.Open(this.ActiveProcess, true);
            this.miner = new MemoryProvider(reader);

            var scanner = new MemorySignatureScanner(reader);
            scanner.Enable();
            var staticAddr = scanner.Scan<int>(MemoryRegionType.READWRITE, "75E98B0D????????5F5E");
            var staticOffset = scanner.Scan<byte>(MemoryRegionType.READWRITE, "578B7E??8D04973BF8742F");
            var ptr1Offset = 0;
            var spdOffset = scanner.Scan<byte>(MemoryRegionType.READWRITE, "DEC9D947??DECADEC1D955FC");
            var cxOffset = scanner.Scan<byte>(MemoryRegionType.READWRITE, "F30F5C4E??F30F59C0F30F59");
            var cyOffset = cxOffset + 4; // scanner.Scan<byte>(MemoryRegionType.READWRITE, "5F8B0A890E8B4A??894EXX8B4AXX894EXX");
            var czOffset = cxOffset + 8; // scanner.Scan<byte>(MemoryRegionType.READWRITE, "8B4A08??894EXXD9420CD95E0C");
            scanner.Disable();

            var carsPool = new MemoryPool("Cars", MemoryAddress.StaticAbsolute, staticAddr, new[] { 0, staticOffset }, 64 * 4);

            this.miner.Add(carsPool);

            for (int k = 0; k < 64; k++)
            {
                var carPool = new MemoryPool("Car " + k, MemoryAddress.Dynamic, carsPool, k * 4, 512);
                carPool.Add(new MemoryField<float>("Speed", MemoryAddress.Dynamic, carPool, spdOffset, 4));
                carPool.Add(new MemoryField<float>("CoordinateX", MemoryAddress.Dynamic, carPool, cxOffset, 4));
                carPool.Add(new MemoryField<float>("CoordinateY", MemoryAddress.Dynamic, carPool, cyOffset, 4));
                carPool.Add(new MemoryField<float>("CoordinateZ", MemoryAddress.Dynamic, carPool, czOffset, 4));

                this.miner.Add(carPool);

                this.Cars.Add(new Ets2Car { ID = k });
            }
        }

        private void sdktel_Data(Ets2Telemetry data, bool newTimestamp)
        {
            if (this.sdkBusy)
            {
                return;
            }

            this.sdkBusy = true;
            try
            {
                if (this.miner != null)
                {
                    this.miner.Refresh();
                    for (int k = 0; k < 64; k++)
                    {
                        var carPool = this.miner.Get("Car " + k);
                        if (carPool == null)
                        {
                            continue;
                        }

                        var car = this.Cars.FirstOrDefault(x => x.ID == k);
                        if (car == null)
                        {
                            continue;
                        }

                        car.Speed = carPool.ReadAs<float>("Speed");
                        car.X = carPool.ReadAs<float>("CoordinateX");
                        car.Y = carPool.ReadAs<float>("CoordinateY");
                        car.Z = carPool.ReadAs<float>("CoordinateZ");
                    }
                }

                var ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345);
                var r = (data.Drivetrain.EngineRpm - 300) / (2500 - 300);
                if (data.Drivetrain.EngineRpm < 300)
                {
                    r = -1;
                }

                var s = ((int) (r * 10000)).ToString() + "," + ((int) (data.Controls.GameThrottle * 1000)).ToString() + "," + (data.Paused ? 1 : 0);
                var sb = Encoding.ASCII.GetBytes(s);
                var dgram = Encoding.ASCII.GetBytes(s);

                this.server.SendTo(dgram, ep);
            }
            catch
            { }
            this.sdkBusy = false;
        }
    }

    public class Ets2Car
    {
        public PointF[] Box;

        public float Distance;

        public float Heading;

        public int ID;

        public float Length;

        public float Speed;

        public bool Tracked;

        public float TTI;

        public float X;

        public float Y;

        public float Z;

        private float lastX = 0.0f;

        private float lastY = 0.0f;

        public bool Valid
        {
            get
            {
                // if (Box == null || Math.Abs(Speed) > 200 || Math.Abs(X) > 1E7 || Math.Abs(Z) > 1E7 || float.IsNaN(X) || float.IsNaN(Z) || float.IsInfinity(X) || float.IsInfinity(Z)) return false;
                if (Math.Abs(this.Speed) > 200 || Math.Abs(this.X) > 1E7 || Math.Abs(this.Z) > 1E7 || float.IsNaN(this.X) || float.IsNaN(this.Z) || float.IsInfinity(this.X) || float.IsInfinity(this.Z))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public void Tick()
        {
            var dx = this.X - this.lastX;
            var dy = this.Z - this.lastY;
            if (Math.Abs(dx) >= 0.02f || Math.Abs(dy) >= 0.02f)
            {
                this.Heading = (float) (Math.PI - Math.Atan2(dy, dx));
                Console.WriteLine("hg" + this.Heading);
            }

            // Rotated polygon
            var carL = 12.0f;
            var carW = 3.0f;
            var hg = -Heading;

            this.Box = new[]
            {
                new PointF(
                            this.X + carL / 2 * (float) Math.Cos(hg) - carW / 2 * (float) Math.Sin(hg),
                            this.Z + carL / 2 * (float) Math.Sin(hg) + carW / 2 * (float) Math.Cos(hg)),
                new PointF(
                            this.X - carL / 2 * (float) Math.Cos(hg) - carW / 2 * (float) Math.Sin(hg),
                            this.Z - carL / 2 * (float) Math.Sin(hg) + carW / 2 * (float) Math.Cos(hg)),
                new PointF(
                            this.X - carL / 2 * (float) Math.Cos(hg) + carW / 2 * (float) Math.Sin(hg),
                            this.Z - carL / 2 * (float) Math.Sin(hg) - carW / 2 * (float) Math.Cos(hg)),
                new PointF(
                            this.X + carL / 2 * (float) Math.Cos(hg) + carW / 2 * (float) Math.Sin(hg),
                            this.Z + carL / 2 * (float) Math.Sin(hg) - carW / 2 * (float) Math.Cos(hg)),
            };

            this.lastX = this.X;
            this.lastY = this.Z;
        }
    }
}