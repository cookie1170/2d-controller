using UnityEngine;

namespace Cookie.PlayerController
{
    public class Jumping : SidescrollerState
    {
        public Jumping(SidescrollerController host)
            : base(host) { }

        public override void OnEnter(SidescrollerState prev, object[] extraParams)
        {
            Host.BufferTimer = 0;
            Host.CoyoteTimer = 0;
            Host.TimeSinceJump = 0;
            Host.Velocity.y = Host.JumpVelocity;
        }

        public override StateChangeData GetNextState()
        {
            if (Host.Velocity.y <= 0)
            {
                return new(Host.Falling, true);
            }

            if (Host.TimeSinceJump >= Host.minJumpDuration && !Host.jumpAction.action.IsPressed())
            {
                return new(Host.Falling, false, true);
            }

            if (Host.IsGrounded())
            {
                return Host.Grounded;
            }

            return StateChangeData.None;
        }

        public override void FixedUpdate()
        {
            Host.ApplyHorizontalMovement();
            Host.ApplyGravity();
            Host.PerformCeilingCheck();
        }

        public override Rigidbody2D.SlideMovement ModifySlideMovement(
            Rigidbody2D.SlideMovement movement
        )
        {
            movement.surfaceAnchor = Vector2.zero;
            // setting surfaceUp to Vector2.zero will make sliding always occur, which lets you slide along the walls properly
            movement.surfaceUp = Vector2.zero;
            return movement;
        }

        public override float GetGravity() => Host.JumpGravity;
    }
}
