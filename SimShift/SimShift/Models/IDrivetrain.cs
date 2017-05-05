using System.Linq;

using SimShift.Entities;
using SimShift.Services;

namespace SimShift.Models
{
    public interface IDrivetrain : IConfigurable
    {
        bool Calibrated { get; set; }

        string File { get; set; }

        double[] GearRatios { get; set; }

        int Gears { get; set; }

        double MaximumRpm { get; set; }

        double StallRpm { get; set; }

        double CalculateFuelConsumption(double rpm, double throttle);

        double CalculateMaxPower();

        double CalculatePower(double rpm, double throttle);

        double CalculateRpmForSpeed(int idealGear, float speed);

        double CalculateSpeedForRpm(int gear, float rpm);

        double CalculateThrottleByPower(double rpm, double powerRequired);

        double CalculateThrottleByTorque(double rpm, double torque);

        double CalculateTorqueN(double rpm);

        double CalculateTorqueP(double rpm, double throttle);

        bool GotDamage(float damage);
    }
}