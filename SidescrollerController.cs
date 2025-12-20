using System;
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
        [Tooltip("The input action used for movement. Should be an axis type")]
        protected InputActionReference moveAction;

        [SerializeField]
        [Tooltip("The input action used for jumping")]
        protected InputActionReference jumpAction;

        [Header("Parameters")]
        [Header("Jump")]
        [SerializeField]
        [Tooltip("The duration you can still jump for after you leave the ground")]
        protected float coyoteTime = 0.15f;

        [SerializeField]
        [Tooltip("The time you can press jump before touching the ground for it to still count")]
        protected float jumpBuffer = 0.2f;

        [SerializeField]
        [Tooltip("The height reached at the peak of the jump")]
        protected float jumpHeight = 3f;

        [SerializeField]
        [Tooltip("The time the jump takes to reach the peak")]
        protected float peakTime = 0.35f;

        [SerializeField]
        [Tooltip("The time the jump takes to fall from the peak")]
        protected float fallTime = 0.25f;

        [SerializeField]
        [Tooltip("Whether or not the player can release space to end the jump early")]
        protected bool variableJumpHeight = true;

        [SerializeField]
        [Tooltip("The minimum duration of the jump when variable jump height is enabled")]
        protected float minJumpDuration = 0.05f;

        [Header("Movement")]
        [SerializeField]
        [Tooltip("The maximum speed reached when moving sideways")]
        protected float topSpeed = 10f;

        [SerializeField]
        [Tooltip("The time taken to reach top speed")]
        protected float accelTime = 0.2f;

        [SerializeField]
        [Tooltip("The time taken to stop from top speed")]
        protected float stopTime = 0.2f;

        [SerializeField]
        [Tooltip("The multiplier to the acceleration when turning around")]
        protected float turnaroundMult = 1.5f;

        [SerializeField]
        [Tooltip("The multiplier to the acceleration when in the air")]
        protected float airAccelMult = 0.8f;

        [SerializeField]
        [Tooltip("The angle threshold for the collision normal to register as ground")]
        protected float groundedAngle = 70;

        [SerializeField]
        [Tooltip(
            "The angle threshold for the collision normal to count as a ceiling. Used for resetting the y velocity when hitting your head on the ceiling"
        )]
        protected float ceilingAngle = 40f;

        [SerializeField]
        [Tooltip("The angle above which movement due to gravity will slip")]
        protected float gravitySlipAngle = 60f;

        protected float JumpVelocity;
        protected float JumpGravity;
        protected float FallGravity;
        protected float Decel;
        protected float Accel;
        protected float GroundedThreshold;
        protected float CeilingThreshold;

        protected float TimeSinceJump = 0;
        protected float CoyoteTimer = 0;
        protected float BufferTimer = 0;

        protected virtual void PerformJump()
        {
            // if we don't reset the timers, you're just gonna keep jumping until they run out
            CoyoteTimer = 0;
            BufferTimer = 0;
            TimeSinceJump = 0;
            Velocity.y = JumpVelocity;
        }

        protected virtual void FixedUpdate()
        {
            TimeSinceJump += Time.fixedDeltaTime;
            BufferTimer -= Time.fixedDeltaTime;

            if (IsGrounded())
            {
                CoyoteTimer = coyoteTime;

                if (Velocity.y < 0)
                    Velocity.y = 0;
            }
            else
            {
                Velocity.y -= GetGravity() * Time.fixedDeltaTime;
                CoyoteTimer -= Time.fixedDeltaTime;
            }

            //  we're checking against timers to provide some leniency to the player when walking off a platform or when jumping right before landing
            if (BufferTimer > 0 && CoyoteTimer > 0)
                PerformJump();

            float moveDir = moveAction.action.ReadValue<float>();
            float accel = GetAccelValue(moveDir);

            Velocity.x = Mathf.MoveTowards(
                Velocity.x,
                moveDir * topSpeed,
                accel * Time.fixedDeltaTime
            );

            Move(GetVelocityVector());

            /*
            we want to reset the velocity when hitting a ceiling because Rigidbody2D.Slide() doesn't modify the velocity we give it
            however, this isn't a full fix because colliding with walls and angled ceilings still doesn't reset your velocity
            TODO: Implement a proper fix
            */
            if (IsCeilingCollision())
                Velocity.y = 0;
        }

        /// <summary>
        /// Gets the amount of horizontal acceleration that should be applied based on the move direction
        /// </summary>
        /// <param name="moveDir">The target move direction</param>
        /// <returns>The amount of acceleration (in meters / second²)</returns>
        protected float GetAccelValue(float moveDir)
        {
            float airMult = IsGrounded() ? 1 : airAccelMult;

            if (Mathf.Approximately(moveDir, 0))
            {
                return Decel * airMult;
            }

            // use the sign of moveDir to compare because it's not always -1 or 1 with analogue input
            if (!Mathf.Approximately(Mathf.Sign(Velocity.x), Mathf.Sign(moveDir)))
            {
                return Accel * turnaroundMult * airMult;
            }

            return Accel * airMult;
        }

        /// <summary>
        /// Checks whether the controller is grounded based on the collision normal
        /// </summary>
        /// <returns>True if the controller is grounded</returns>
        protected bool IsGrounded()
        {
            if (!LastResult.surfaceHit)
                return false;

            Vector2 lastNormal = LastResult.surfaceHit.normal;
            float dot = Vector2.Dot(lastNormal, Vector2.up);

            return dot >= GroundedThreshold;
        }

        /// <summary>
        /// Checks whether the last collision was against a ceiling
        /// </summary>
        /// <returns>True if the collision normal is almost pointing down</returns>
        protected bool IsCeilingCollision()
        {
            float dot = Vector2.Dot(LastResult.slideHit.normal, Vector2.down);
            bool isCeiling = LastResult.slideHit && dot >= CeilingThreshold;
            return isCeiling;
        }

        /// <summary>
        /// Gets the gravity acceleration that should be used, taking into account variable jump height and different gravity for falling and jumping
        /// </summary>
        /// <returns>The gravity acceleration (in meters / second²)</returns>
        protected float GetGravity()
        {
            if (variableJumpHeight)
            {
                bool isJumping = jumpAction.action.IsPressed() || TimeSinceJump < minJumpDuration;
                return (Velocity.y >= 0 && isJumping) ? JumpGravity : FallGravity;
            }
            else
            {
                return Velocity.y >= 0 ? JumpGravity : FallGravity;
            }
        }

        protected override Rigidbody2D.SlideMovement GetSlideMovement()
        {
            return new Rigidbody2D.SlideMovement()
            {
                gravity = GetGravityVector(),
                surfaceAnchor =
                    Mathf.Approximately(Velocity.y, 0) && IsGrounded()
                        ? Vector2.down
                        : Vector2.zero, // can't jump with it being set to Vector2.down because you're permanently stuck to the floor
                gravitySlipAngle = gravitySlipAngle,
                surfaceUp = Vector2.zero, // setting surfaceUp to Vector2.zero will make sliding always occur, which lets you slide along the ceiling properly
            };
        }

        /*
        we need this distinction because Rigidbody2D.Slide() requires them to be distinct for slipping to work properly
        Rigidbody2D.SlideMovement.gravity is a bit of a misnomer, as it's not the gravitational acceleration,
        rather just the movement caused by gravity
        
        > The reason that gravity is separated from the provided velocity
        > is that it has a different behaviour in that it can produce slippage
        > on surfaces where the angle is higher than Rigidbody2D.SlideMovement.gravitySlipAngle.
        see: https://docs.unity3d.com/ScriptReference/Rigidbody2D.SlideMovement-gravity.html

        NOTE: this only accounts for gravity pointing downwards, which is usually the case, but if it's not it should be modified!
        */

        /// <summary>
        /// The velocity vector is any movement that is not due to gravity (aka not moving down)
        /// </summary>
        /// <returns>The velocity vector</returns>
        protected Vector2 GetVelocityVector()
        {
            return Velocity.y < 0 ? new Vector2(Velocity.x, 0) : Velocity;
        }

        /// <summary>
        /// The gravity vector is any movement due to gravity (aka moving down)
        /// </summary>
        /// <returns>The gravity vector</returns>
        protected Vector2 GetGravityVector()
        {
            return Velocity.y < 0 ? new Vector2(0, Velocity.y) : Vector2.zero;
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            BufferTimer = jumpBuffer;
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

        protected override void Init()
        {
            base.Init();
            JumpVelocity = 2.0f * jumpHeight / peakTime;
            JumpGravity = 2.0f * jumpHeight / (peakTime * peakTime);
            FallGravity = 2.0f * jumpHeight / (fallTime * fallTime);
            Decel = topSpeed / stopTime;
            Accel = topSpeed / accelTime;

            /*
            a dot product is equal to a product of the magnitudes times the cosine of the angle between two vectors
            since both our Vector2.down/up vectors and the collision normal are normalized,
            we just need to check the dot product against the cosine of the angles
            */

            GroundedThreshold = Mathf.Cos(groundedAngle * Mathf.Deg2Rad);
            CeilingThreshold = Mathf.Cos(ceilingAngle * Mathf.Deg2Rad);
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
