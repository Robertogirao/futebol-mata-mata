using NUnit.Framework;

namespace MarbleBasketFlipper2D.Tests
{
    public sealed class FlipperRules2DTests
    {
        [Test] public void TheLowerHoopScoresTwoAndTheUpperScoresThree()
        {
            Assert.That(FlipperRules2D.PointsCrossed(new Point2D(3.65, -.4), new Point2D(3.65, -.9), .17), Is.EqualTo(2));
            Assert.That(FlipperRules2D.PointsCrossed(new Point2D(5.05, 1.8), new Point2D(5.05, 1.1), .17), Is.EqualTo(3));
        }
        [Test] public void SideHitsAndUpwardCrossingsDoNotScore()
        {
            Assert.That(FlipperRules2D.PointsCrossed(new Point2D(3.65, -.9), new Point2D(3.65, -.4), .17), Is.Zero);
            Assert.That(FlipperRules2D.PointsCrossed(new Point2D(3.10, -.4), new Point2D(3.10, -.9), .17), Is.Zero);
            Assert.That(FlipperRules2D.PointsCrossed(new Point2D(5.62, 1.8), new Point2D(5.62, 1.1), .17), Is.Zero);
        }
        [Test] public void MovedHoopScoresAtItsCurrentPosition()
        {
            var low = new Point2D(0, 0); var high = new Point2D(.2, .12);
            // This crossing is inside the shifted hoop, but outside the original one.
            Assert.That(FlipperRules2D.PointsCrossedMoving(new Point2D(5.63, 1.65), new Point2D(5.63, 1.50), .17,
                low, low, high, high), Is.EqualTo(3));
            Assert.That(FlipperRules2D.PointsCrossed(new Point2D(5.63, 1.65), new Point2D(5.63, 1.50), .17), Is.Zero);
            // Crossing where the hoop used to be must not produce a phantom point.
            Assert.That(FlipperRules2D.PointsCrossedMoving(new Point2D(4.63, 1.65), new Point2D(4.63, 1.50), .17,
                low, low, high, high), Is.Zero);
            Assert.That(FlipperRules2D.PointsCrossedMoving(new Point2D(5.10, 1.5), new Point2D(5.10, 1.7), .17,
                low, low, high, high), Is.Zero);
        }
        [Test] public void HoopMovementRemainsGentleAndMovesInBothDirections()
        {
            bool positiveX = false, negativeX = false, positiveY = false, negativeY = false;
            var start = FlipperRules2D.BasketOffset(0, 2);
            Assert.That(start.x, Is.Zero); Assert.That(start.y, Is.Zero);
            for (int i = 0; i < 1000; i++)
            {
                var low = FlipperRules2D.BasketOffset(i * .1, 2);
                var high = FlipperRules2D.BasketOffset(i * .1, 3);
                Assert.That(System.Math.Abs(low.x), Is.LessThanOrEqualTo(.18));
                Assert.That(System.Math.Abs(low.y), Is.LessThanOrEqualTo(.12));
                Assert.That(high.x, Is.EqualTo(-low.x).Within(1e-10));
                Assert.That(high.y, Is.EqualTo(-low.y).Within(1e-10));
                positiveX |= low.x > .1; negativeX |= low.x < -.1;
                positiveY |= low.y > .08; negativeY |= low.y < -.08;
            }
            Assert.That(positiveX && negativeX && positiveY && negativeY, Is.True);
        }
        [Test] public void LavaRuleEliminatesOnlyMissesAndNeverAStuckOrScoringBall()
        {
            Assert.That(FlipperRules2D.Eliminate(false, false, true), Is.True);
            Assert.That(FlipperRules2D.Eliminate(false, false, false), Is.False);
            Assert.That(FlipperRules2D.Eliminate(true, false, true), Is.False);
            Assert.That(FlipperRules2D.Eliminate(false, true, true), Is.False);
        }
        [Test] public void EachCalibratedShotArrivesDescendingAtItsIntendedHoop()
        {
            var origin = new Point2D(-4.69, -2.08);
            foreach (var target in new[] { new Point2D(3.65, -.65), new Point2D(5.05, 1.45) })
            {
                double time = target.y > 0 ? 1.55 : 1.60;
                var velocity = BasketMath2D.ShotVelocity(origin, target, 9.81, time);
                Assert.That(origin.x + velocity.x * time, Is.EqualTo(target.x).Within(1e-8));
                Assert.That(origin.y + velocity.y * time - .5 * 9.81 * time * time, Is.EqualTo(target.y).Within(1e-8));
                Assert.That(velocity.y - 9.81 * time, Is.LessThan(0));
            }
        }
    }
}
