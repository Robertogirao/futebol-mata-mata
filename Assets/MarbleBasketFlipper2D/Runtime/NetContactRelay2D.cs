using UnityEngine;

namespace MarbleBasketFlipper2D
{
    /// <summary>Moves the basket net in response to a scored ball contacting its sides.</summary>
    public sealed class NetContactRelay2D : MonoBehaviour
    {
        internal FlipperArena2D arena;
        internal int points;
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (arena == null || collision == null) return;
            var body = collision.rigidbody;
            if (body == null && collision.collider != null) body = collision.collider.attachedRigidbody;
            if (body != null) arena.RecordNetContact(points, body);
        }
    }
}
