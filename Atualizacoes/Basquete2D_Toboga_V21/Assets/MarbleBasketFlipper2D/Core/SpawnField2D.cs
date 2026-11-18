using System;
using System.Collections.Generic;

namespace MarbleBasketFlipper2D
{
    /// <summary>Independent positioning inside the funnel, including a sweeping return emitter.</summary>
    public static class SpawnField2D
    {
        public const double TopY = 4.14, NeckY = 2.62;
        public const double LeftTop = -7.55, RightTop = -4.80;
        public const double LeftNeck = -6.24, RightNeck = -5.50;
        public const double MinSpawnY = 3.00, MaxSpawnY = 3.97;
        public const double DescentLeanDegrees = 16;

        private const double FallToNeck = TopY - NeckY;
        private const double RightSlope = (RightTop - RightNeck) / FallToNeck;
        private static readonly double NeckSlope = Math.Tan(DescentLeanDegrees * Math.PI / 180);
        private static readonly double InletSlope = 2 * (LeftNeck - LeftTop) / FallToNeck - NeckSlope;
        private static readonly double InletCurvature = (NeckSlope - InletSlope) / FallToNeck;

        public static double LeftGuideX(double y)
        {
            double fall = TopY - y;
            return LeftTop + fall * (InletSlope + .5 * InletCurvature * fall);
        }

        public static void Bounds(double y, double radius, out double left, out double right)
        {
            double fall = TopY - y;
            double leftSlope = InletSlope + InletCurvature * fall;
            double clearance = radius + .07;
            left = LeftGuideX(y) + clearance * Math.Sqrt(1 + leftSlope * leftSlope);
            right = RightTop - fall * RightSlope - clearance * Math.Sqrt(1 + RightSlope * RightSlope);
        }
        public static bool Contains(Point2D p, double radius)
        {
            if (p.y < MinSpawnY || p.y > MaxSpawnY) return false;
            Bounds(p.y, radius, out double left, out double right);
            return p.x >= left && p.x <= right;
        }
        public static Point2D[] InitialPositions(int count, double radius, Random random)
        {
            if (count < 0 || count > 12 || radius <= 0 || radius > .17001 || random == null)
                throw new ArgumentException("Use até 12 bolinhas com raio compatível com o funil.");
            double minimum = 2 * radius + .01, minimumSquared = minimum * minimum;
            for (int restart = 0; restart < 80; restart++)
            {
                var positions = new List<Point2D>(count);
                for (int i = 0; i < count; i++)
                {
                    bool found = false;
                    for (int attempt = 0; attempt < 140; attempt++)
                    {
                        double y = MinSpawnY + random.NextDouble() * (MaxSpawnY - MinSpawnY);
                        Bounds(y, radius, out double left, out double right);
                        if (right <= left) continue;
                        var candidate = new Point2D(left + random.NextDouble() * (right - left), y);
                        bool free = true;
                        foreach (var p in positions)
                        {
                            double dx = p.x - candidate.x, dy = p.y - candidate.y;
                            if (dx * dx + dy * dy < minimumSquared) { free = false; break; }
                        }
                        if (!free) continue;
                        positions.Add(candidate); found = true; break;
                    }
                    if (!found) break;
                }
                if (positions.Count == count) return positions.ToArray();
            }
            // Pack spare positions using the same walls and separation as the random start.
            var fallback = new List<Point2D>(count);
            double spacing = 2 * radius + .015;
            for (double y = MaxSpawnY; y >= MinSpawnY && fallback.Count < count; y -= spacing)
            {
                Bounds(y, radius, out double left, out double right);
                int slots = 1 + (int)Math.Floor((right - left) / spacing);
                double first = (left + right - (slots - 1) * spacing) * .5;
                for (int i = 0; i < slots; i++) fallback.Add(new Point2D(first + i * spacing, y));
            }
            for (int i = fallback.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1); var temp = fallback[i]; fallback[i] = fallback[j]; fallback[j] = temp;
            }
            return fallback.GetRange(0, count).ToArray();
        }
        public static double EmitterX(double elapsed, double speed)
            => (LeftNeck + RightNeck) * .5 + (RightTop - LeftTop) * .31 * Math.Sin(elapsed * speed);

        public static Point2D Candidate(Random random, double radius, double elapsed, double speed, int attempt)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            const double minReturnY = 3.33;
            double y = minReturnY + random.NextDouble() * (MaxSpawnY - minReturnY);
            Bounds(y, radius, out double left, out double right);
            double spread = Math.Min(1.3, .12 + Math.Max(0, attempt) * .035);
            double x = EmitterX(elapsed, speed) + (random.NextDouble() * 2 - 1) * spread;
            return new Point2D(Math.Max(left, Math.Min(right, x)), y);
        }
    }
}
