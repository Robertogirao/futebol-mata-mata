using System;
using NUnit.Framework;

namespace MarbleBasketFlipper2D.Tests
{
    public sealed class FlipperMotion2DTests
    {
        [Test] public void LeverMovesBothWaysThroughTheWiderArc()
        {
            Assert.That(FlipperMotion2D.Angle(0, 1), Is.EqualTo(-8).Within(1e-9));
            double min = 100, max = -100; bool rising = false, falling = false;
            double previous = FlipperMotion2D.Angle(0, 1);
            for (int i = 1; i <= 200; i++)
            {
                double angle = FlipperMotion2D.Angle(i * .01, 1);
                min = Math.Min(min, angle); max = Math.Max(max, angle);
                rising |= angle > previous + .01; falling |= angle < previous - .01;
                previous = angle;
            }
            Assert.That(min, Is.LessThan(-7.9)); Assert.That(max, Is.GreaterThan(69.9));
            Assert.That(rising && falling, Is.True);
        }
    }
}
