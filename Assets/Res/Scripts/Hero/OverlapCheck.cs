using UnityEngine;

namespace Res.Scripts.Hero
{
    public class OverlapCheck: MonoBehaviour
    {

        [Header("Layers")] public LayerMask groundLayer;

        [Space] public bool onGround;
        public bool onWall;
        public bool onRightWall;
        public bool onLeftWall;
        public int wallSide;

        [Space] [Header("Collision")] public float collisionRadius = 0.25f;
        public Vector2 bottomOffset, rightOffset, leftOffset;

        // Update is called once per frame
        void Update()
        {
            onGround = Physics2D.OverlapCircle((Vector2)transform.position + bottomOffset, collisionRadius, groundLayer);
            onRightWall = Physics2D.OverlapCircle((Vector2)transform.position + rightOffset, collisionRadius, groundLayer);
            onLeftWall = Physics2D.OverlapCircle((Vector2)transform.position + leftOffset, collisionRadius, groundLayer);
            onWall = onRightWall || onLeftWall;
            wallSide = onRightWall ? -1 : 1;
        }

        void OnDrawGizmos()
        {
            var rawColor = Gizmos.color;
            Gizmos.color = Color.red;

            var positions = new Vector2[] { bottomOffset, rightOffset, leftOffset };

            Gizmos.DrawWireSphere((Vector2)transform.position + bottomOffset, collisionRadius);
            Gizmos.DrawWireSphere((Vector2)transform.position + rightOffset, collisionRadius);
            Gizmos.DrawWireSphere((Vector2)transform.position + leftOffset, collisionRadius);
            Gizmos.color = rawColor;
        }
    }
}