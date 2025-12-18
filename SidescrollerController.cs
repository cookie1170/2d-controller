using UnityEngine;
using UnityEngine.InputSystem;
#if COOKIE_UTILS
using CookieUtils.Debugging;
#endif

namespace Cookie.PlayerController
{
    public class SidescrollerController : Controller2D
    {
        [SerializeField]
        protected InputActionReference moveAction;

        [SerializeField]
        protected InputActionReference jumpAction;

        [Header("Parameters")]
        [SerializeField]
        protected float gravity = 9.8f;

        [SerializeField]
        protected float moveSpeed = 10f;

        [SerializeField]
        protected float jumpVel = 10f;

        [SerializeField]
        private float groundedThreshold = 0.2f;

        protected virtual void PerformJump()
        {
            if (IsGrounded())
                Velocity.y = jumpVel;
        }

        protected virtual void FixedUpdate()
        {
            bool isGrounded = IsGrounded();
            float moveDir = moveAction.action.ReadValue<float>();
            Velocity.x = moveSpeed * moveDir;

            if (!isGrounded)
            {
                Velocity.y -= GetGravity() * Time.fixedDeltaTime;
            }
            else if (Velocity.y < 0)
            {
                Velocity.y = 0;
            }

            Move(Velocity.y < 0 ? new Vector2(Velocity.x, 0) : Velocity);
        }

        private bool IsGrounded()
        {
            if (!LastResult.surfaceHit)
                return false;

            Vector2 lastNormal = LastResult.surfaceHit.normal;
            float dot = Vector2.Dot(lastNormal, Vector2.up);

            return dot >= groundedThreshold;
        }

        private float GetGravity()
        {
            return gravity;
        }

        protected override Rigidbody2D.SlideMovement GetSlideMovement()
        {
            return new Rigidbody2D.SlideMovement()
            {
                gravity = Velocity.y < 0 ? new Vector2(0, Velocity.y) : Vector2.zero, // cursed shenanigans because gravity should be a separate vector
                surfaceAnchor = Velocity.y <= 0 ? Vector2.down : Vector2.zero, // can't jump without it
            };
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            PerformJump();
        }

        protected virtual void OnEnable()
        {
            jumpAction.action.Enable();
            moveAction.action.Enable();
            jumpAction.action.performed += OnJump;
        }

        protected virtual void OnDisable()
        {
            jumpAction.action.performed -= OnJump;
        }

#if COOKIE_UTILS
        public override void SetUpDebugUI(IDebugUI_BuilderProvider provider)
        {
            IDebugUI_Builder builder = provider.GetFor(this);
            builder.BoolField("Grounded", () => IsGrounded());
            DrawControllerInfo(builder);
        }
#endif
    }
}
