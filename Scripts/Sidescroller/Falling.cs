using UnityEngine;

namespace Cookie.PlayerController
{
    public class Falling : SidescrollerState
    {
        public Falling(SidescrollerController host)
            : base(host) { }

        protected float HangTimer = 0;

        public override void OnEnter(SidescrollerState prev, object[] extraParams)
        {
            HangTimer = 0;

            if (extraParams.Length >= 1 && extraParams[0] is bool isApex && isApex)
            {
                HangTimer = Host.hangTime;
                Host.Velocity.x *= Host.jumpApexSpeedUp;
            }

            if (extraParams.Length >= 2 && extraParams[1] is bool jumpCut && jumpCut)
            {
                Host.Velocity.y -= Host.jumpReleaseImpulse;
            }
        }

        public override StateChangeData GetNextState()
        {
            if (Host.IsGrounded())
            {
                return Host.Grounded;
            }

            return StateChangeData.None;
        }

        public override void FixedUpdate()
        {
            HangTimer -= Time.fixedDeltaTime;

            Host.ApplyHorizontalMovement();
            Host.ApplyGravity();
            Host.PerformJumpCheck();
            Host.PerformCeilingCheck();
        }

        public override Rigidbody2D.SlideMovement ModifySlideMovement(
            Rigidbody2D.SlideMovement movement
        )
        {
            movement.surfaceAnchor = Vector2.zero;
            // setting surfaceUp to Vector2.zero will make sliding always occur, which lets you slide along the ceiling properly
            movement.surfaceUp = Vector2.zero;
            return movement;
        }

        public override float GetGravity()
        {
            if (HangTimer > 0)
                return Host.FallGravity * Host.hangGravityMult;

            return Host.FallGravity;
        }
    }
}
