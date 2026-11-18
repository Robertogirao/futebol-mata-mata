using System;

namespace MarbleBasketFlipper2D
{
    /// <summary>Continuous movement of the mechanical ramp; collisions are handled by Physics2D.</summary>
    public static class FlipperMotion2D
    {
        public const double MinimumAngle = -8, MaximumAngle = 70;

        public static double Angle(double elapsed, double cyclesPerSecond)
        {
            double center = (MinimumAngle + MaximumAngle) * .5;
            double halfRange = (MaximumAngle - MinimumAngle) * .5;
            // A small, smooth phase drift prevents every swing from looking identical.
            double phase = 2 * Math.PI * cyclesPerSecond * elapsed + .45 * Math.Sin(1.4 * elapsed);
            return center - halfRange * Math.Cos(phase);
        }

    }
}
