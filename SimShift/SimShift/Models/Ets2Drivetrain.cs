﻿using System;
using System.Collections.Generic;
using System.Linq;

using SimShift.Utils;

namespace SimShift.Models
{
    public class Ets2Drivetrain : GenericDrivetrain
    {
        private int damagedGears;

        public override int Gears
        {
            get
            {
                return base.Gears - damagedGears;
            }
        }

        public double MaximumPower { get; private set; }

        public double MaximumTorque { get; private set; }

        private double Ets2Torque { get; set; }

        public override void ApplyParameter(IniValueObject obj)
        {
            base.ApplyParameter(obj);
            switch (obj.Key)
            {
                case "Ets2Engine":
                    Ets2Torque = obj.ReadAsFloat();
                    break;
            }
        }

        public override double CalculateFuelConsumption(double rpm, double throttle)
        {
            // Rough linearisation
            // 2100Nm=11.548
            // 3500Nm=14.94
            // from formula fuel=14.94*exp(0.0009*rpm)
            var amplitudeForEngine = 6.46 + 1 / 412.72 * Ets2Torque;

            // This curve is not compensated for absolute values
            // The relative meaning is , however, correct, but comparisions between trucks is not possible!
            var amplitude = amplitudeForEngine * Math.Exp(rpm * 0.000918876); // consumption @ 100%
            throttle *= 100;
            var linearity = -8.0753 * Math.Pow(throttle, 6) * Math.Pow(10, -12) + 2.691 * Math.Pow(throttle, 5) * Math.Pow(10, -9) - 0.349616 * Math.Pow(throttle, 4) * Math.Pow(10, -6) + 23.577 * Math.Pow(throttle, 3) * Math.Pow(10, -6) - 0.918283 * Math.Pow(10, -3) * Math.Pow(throttle, 2) + 0.027293 * throttle + 0.0019368;

            var fuel = amplitude * linearity * 1.22;
            fuel -= 0.25;
            if (fuel < 0) fuel = 0;
            return fuel;
        }

        public override double CalculatePower(double rpm, double throttle)
        {
            var torque = CalculateTorqueP(rpm, throttle);
            return torque * (rpm / 1000) / (1 / 0.1904) * 0.75f;
        }

        public override double CalculateThrottleByTorque(double rpm, double torque)
        {
            var negativeTorque = CalculateTorqueN(rpm);
            var positiveTorque = CalculateTorqueP(rpm, 1) - negativeTorque;

            return (torque - negativeTorque) / positiveTorque;
        }

        public override double CalculateTorqueN(double rpm)
        {
            var negativeTorqueNormalized = 1.7504 // 0
                                           - 7.0542 / Math.Pow(10, 3) * rpm // 1
                                           + 9.1425 / Math.Pow(10, 6) * rpm * rpm // 2
                                           - 4.1157 / Math.Pow(10, 9) * rpm * rpm * rpm // 3
                                           + 0.6036 / Math.Pow(10, 12) * rpm * rpm * rpm * rpm // 4
                ; // -0.2338 / Math.Pow(10, 15) * rpm * rpm * rpm * rpm * rpm; // 5

            // 906.051 was measured with Scania 730 hp engine.
            // This produces 3500Nm.
            // The brake torque is proportional to the ETS2 engine torque
            var negativeTorqueAbsolute = negativeTorqueNormalized * (906.051 / 3500.0 * Ets2Torque);
            negativeTorqueAbsolute *= -1; // ;)
            return negativeTorqueAbsolute;
        }

        public override double CalculateTorqueP(double rpm, double throttle)
        {
            double negativeTorque = CalculateTorqueN(rpm);

            var positiveTorqueNormalized = -0.3789 + rpm * 0.0022716 - rpm * rpm * 0.0011134 / 1000 + rpm * rpm * rpm * 0.1372 / 1000000000;

            var positiveTorqueAbs = positiveTorqueNormalized * Ets2Torque;

            return positiveTorqueAbs * throttle + negativeTorque * (1 - throttle);
        }

        public override IEnumerable<IniValueObject> ExportParameters()
        {
            List<IniValueObject> obj = base.ExportParameters().ToList();
            obj.Add(new IniValueObject(base.AcceptsConfigs, "Ets2Engine", Ets2Torque.ToString("0.0")));
            return obj;
        }

        public override bool GotDamage(float damage)
        {
            var wasDamaged = damagedGears;
            damagedGears = (int) Math.Floor(damage * Gears);
            return wasDamaged != damagedGears;
        }

        public override void ResetParameters()
        { }
    }
}