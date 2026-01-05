namespace Cookie.PlayerController
{
    public class Grounded : SidescrollerState
    {
        public Grounded(SidescrollerController host)
            : base(host) { }

        public override StateChangeData GetNextState()
        {
            if (!Host.IsGrounded())
            {
                return Host.Falling;
            }

            return StateChangeData.None;
        }

        public override void FixedUpdate()
        {
            Host.Velocity.y = 0;
            Host.CoyoteTimer = Host.coyoteTime;
            Host.ApplyHorizontalMovement();
            Host.PerformJumpCheck();
        }

        public override float GetGravity() => 0;
    }
}
