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
        [Tooltip("The input action used for movement. Should be an axis type")]
        public InputActionReference moveAction;

        [Tooltip("The input action used for jumping")]
        public InputActionReference jumpAction;

        [Header("Parameters")]
        [Header("Jump")]
        [Tooltip("The duration you can still jump for after you leave the ground")]
        public float coyoteTime = 0.15f;

        [Tooltip("The time you can press jump before touching the ground for it to still count")]
        public float jumpBuffer = 0.2f;

        [Tooltip("The height reached at the peak of the jump")]
        public float jumpHeight = 3f;

        [Tooltip("The time the jump takes to reach the peak")]
        public float peakTime = 0.35f;

        [Tooltip("The time the jump takes to fall from the peak")]
        public float fallTime = 0.25f;

        [Tooltip("The hang time applied at the jump's apex")]
        public float hangTime = 0.2f;

        [Tooltip("The gravity multiplier during hang time")]
        public float hangGravityMult = 0.75f;

        [Tooltip("The multiplier applied to the horizontal velocity when reaching the jump apex")]
        public float jumpApexSpeedUp = 1.25f;

        [Tooltip("Whether or not the player can release space to end the jump early")]
        public bool variableJumpHeight = true;

        [Tooltip("The minimum duration of the jump when variable jump height is enabled")]
        public float minJumpDuration = 0.05f;

        [Tooltip("The downwards impulse when releasing jump if variable jump height is enabled")]
        public float jumpReleaseImpulse = 4f;

        [Header("Movement")]
        [Tooltip("The maximum speed reached when moving sideways")]
        public float topSpeed = 10f;

        [Tooltip("The time taken to reach top speed")]
        public float accelTime = 0.2f;

        [Tooltip("The time taken to stop from top speed")]
        public float stopTime = 0.2f;

        [Tooltip("The multiplier to the acceleration when turning around")]
        public float turnaroundMult = 1.5f;

        [Tooltip("The multiplier to the acceleration when in the air")]
        public float airAccelMult = 0.8f;

        [Tooltip("The angle threshold for the collision normal to register as ground")]
        public float groundedAngle = 70f;

        [Tooltip(
            "The angle threshold for the collision normal to count as a ceiling. Used for resetting the y velocity when hitting your head on the ceiling"
        )]
        public float ceilingAngle = 60f;

        [Tooltip("The angle above which movement due to gravity will slip")]
        public float gravitySlipAngle = 60f;

        [NonSerialized]
        public float JumpVelocity;

        [NonSerialized]
        public float JumpGravity;

        [NonSerialized]
        public float FallGravity;

        [NonSerialized]
        public float Decel;

        [NonSerialized]
        public float Accel;

        [NonSerialized]
        public float GroundedThreshold;

        [NonSerialized]
        public float CeilingThreshold;

        [NonSerialized]
        public float TimeSinceJump = 0f;

        [NonSerialized]
        public float CoyoteTimer = 0f;

        [NonSerialized]
        public float BufferTimer = 0f;

        protected SidescrollerState state;

        public Grounded Grounded;
        public Jumping Jumping;
        public Falling Falling;

        protected virtual void FixedUpdate()
        {
            TimeSinceJump += Time.fixedDeltaTime;
            BufferTimer -= Time.fixedDeltaTime;

            StateChangeData data = state.GetNextState();

            if (data.NextState != null)
                ChangeState(data.NextState, data.ExtraParams);

            state.FixedUpdate();

            Move(GetVelocityVector());
        }

        /// <summary>
        /// Performs a check for a ceiling collision and pushes you away if it is one
        /// </summary>
        /*
        we want to push the player away when hitting a ceiling because Rigidbody2D.Slide() doesn't modify the velocity we give it
        however, this isn't a full fix because colliding with walls and stuff still doesn't reset your velocity and you also get pushed away weirdly
        TODO: Implement a proper fix
        */
        public void PerformCeilingCheck()
        {
            if (IsCeilingCollision(out Vector2 normal, out _))
            {
                // this cursed abomination seems to sorta work? idk
                Vector2 pushAway = normal * Vector2.Dot(-Velocity, normal);
                Velocity += pushAway;
                ChangeState(Falling);
            }
        }

        /// <summary>
        /// Applies horizontal movement based on the player input for the controller
        /// </summary>
        public virtual void ApplyHorizontalMovement()
        {
            float moveDir = moveAction.action.ReadValue<float>();
            float accel = GetAccelValue(moveDir);

            Velocity.x = Mathf.MoveTowards(
                Velocity.x,
                moveDir * topSpeed,
                accel * Time.fixedDeltaTime
            );
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

        protected override Rigidbody2D.SlideMovement GetSlideMovement()
        {
            return state.ModifySlideMovement(
                new Rigidbody2D.SlideMovement()
                {
                    gravity = GetGravityVector(),
                    gravitySlipAngle = gravitySlipAngle,
                }
            );
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

        /// <summary>
        /// Changes the state
        /// </summary>
        /// <param name="newState">The state to change to</param>
        protected virtual void ChangeState(SidescrollerState newState, params object[] extraParams)
        {
            state?.OnExit(newState);
            newState.OnEnter(state, extraParams);
            state = newState;
        }

        protected virtual void OnEnable()
        {
            if (!jumpAction.action.enabled)
                jumpAction.action.Enable();

            if (!moveAction.action.enabled)
                moveAction.action.Enable();

            jumpAction.action.performed += OnJump;
            SnapToGround();
            if (Grounded != null)
                state = Grounded;
        }

        /// <summary>
        /// Applies gravity to the controller
        /// </summary>
        public virtual void ApplyGravity()
        {
            Velocity.y -= GetGravity() * Time.fixedDeltaTime;
        }

        /// <summary>
        /// Casts a ray straight downwards and snaps the controller to the hit position
        /// </summary>
        public virtual void SnapToGround()
        {
            ContactFilter2D filter = new() { useTriggers = false };
            var results = new RaycastHit2D[1];
            if (rigidBody.Cast(Vector2.down, filter, results) > 0)
            {
                RaycastHit2D result = results[0];
                rigidBody.position = transform.position + Vector3.down * result.distance;
            }
        }

        /// <summary>
        /// Gets the gravity acceleration that should be used, taking into account variable jump height and different gravity for falling and jumping
        /// </summary>
        /// <returns>The gravity acceleration (in meters / second²)</returns>
        protected float GetGravity()
        {
            return state.GetGravity();
        }

        /// <summary>
        /// Checks whether the controller is grounded based on the collision normal
        /// </summary>
        /// <returns>True if the controller is grounded</returns>
        public bool IsGrounded()
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
        public bool IsCeilingCollision(out Vector2 normal, out float dot)
        {
            return IsCeilingHit(LastResult.slideHit, out normal, out dot);
        }

        /// <summary>
        /// Checks whether <c>hit</c> is a ceiling hit
        /// </summary>
        /// <param name="hit">The hit to check</param>
        /// <returns>True if it's a ceiling hit</returns>
        private bool IsCeilingHit(RaycastHit2D hit, out Vector2 normal, out float dot)
        {
            normal = Vector2.zero;
            dot = -1f;

            if (!hit)
                return false;

            normal = hit.normal.normalized;
            dot = Vector2.Dot(normal, Vector2.down);
            bool isCeiling = dot >= CeilingThreshold;
            return isCeiling;
        }

        /// <summary>
        /// Performs a check against <c>BufferTimer</c> and <c>CoyoteTimer</c> and changes the state to <c>jumping</c> if they're both active
        /// </summary>
        public bool PerformJumpCheck()
        {
            if (BufferTimer > 0 && CoyoteTimer > 0)
            {
                ChangeState(Jumping);
                return true;
            }

            return false;
        }

        protected override void Awake()
        {
            base.Awake();
            Grounded = new(this);
            Jumping = new(this);
            Falling = new(this);
            state = Grounded;
            state.OnEnter(null, null);
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
            builder.StringField("State", () => state.GetType().Name);
            DrawControllerInfo(builder);
            builder.Button("Snap To Ground", SnapToGround);
        }
#endif
    }
}
