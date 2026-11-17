using System;

namespace MarbleBasketFlipper2D
{
    public static class FlipperRules2D
    {
        public const float LowY = -.65f, LowLeft = 3.05f, LowRight = 4.25f;
        public const float HighY = 1.45f, HighLeft = 4.45f, HighRight = 5.65f;

        public static int PointsCrossed(Point2D before, Point2D after, double radius)
        {
            if (BasketMath2D.CrossedHoop(before, after, HighY, HighLeft, HighRight, radius)) return 3;
            if (BasketMath2D.CrossedHoop(before, after, LowY, LowLeft, LowRight, radius)) return 2;
            return 0;
        }

        // Small, slow offsets; both baskets begin at their original positions.
        public static Point2D BasketOffset(double elapsed, int points)
        {
            double direction = points == 3 ? -1 : 1;
            return new Point2D(direction * .18 * Math.Sin(elapsed * .65),
                direction * .12 * Math.Sin(elapsed * .47));
        }

        public static int PointsCrossedMoving(Point2D before, Point2D after, double radius,
            Point2D previousLow, Point2D currentLow, Point2D previousHigh, Point2D currentHigh)
        {
            // Compare the ball and each hoop in the hoop's moving coordinate system.
            if (BasketMath2D.CrossedHoop(before - previousHigh, after - currentHigh,
                HighY, HighLeft, HighRight, radius)) return 3;
            if (BasketMath2D.CrossedHoop(before - previousLow, after - currentLow,
                LowY, LowLeft, LowRight, radius)) return 2;
            return 0;
        }
        public static bool Eliminate(bool scored, bool stuckInChute, bool eliminationEnabled)
            => !scored && !stuckInChute && eliminationEnabled;
    }
}
