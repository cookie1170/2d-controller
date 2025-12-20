using UnityEngine;
using UnityEngine.InputSystem;

namespace Cookie.PlayerController
{
    public class TopdownController : Controller2D
    {
        [SerializeField]
        [Tooltip("The input action used for movement. Should be a Vector2 type")]
        protected InputActionReference moveAction;

        [Header("Parameters")]
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

        protected float Decel;
        protected float Accel;

        protected virtual void FixedUpdate()
        {
            Vector2 moveDir = moveAction.action.ReadValue<Vector2>();
            float accel = GetAccelValue(moveDir);

            Velocity = Vector2.MoveTowards(
                Velocity,
                moveDir * topSpeed,
                accel * Time.fixedDeltaTime
            );

            Move(Velocity);
        }

        protected override Rigidbody2D.SlideMovement GetSlideMovement()
        {
            return new Rigidbody2D.SlideMovement()
            {
                gravity = Vector2.zero,
                surfaceUp = Vector2.zero, // since this is topdown, we want to always slideds
                surfaceAnchor = Vector2.zero, // we don't want any snapping
            };
        }

        /// <summary>
        /// Gets the amount of acceleration that should be applied based on the move direction
        /// </summary>
        /// <param name="moveDir">The target move direction</param>
        /// <returns>The amount of acceleration (in meters / second²)</returns>
        protected float GetAccelValue(Vector2 moveDir)
        {
            if (moveDir.sqrMagnitude < 0.0001f)
            {
                return Decel;
            }

            if (!Mathf.Approximately(Vector2.Dot(Velocity, moveDir), 1))
            {
                return Accel * turnaroundMult;
            }

            return Accel;
        }

        protected override void Init()
        {
            base.Init();
            Decel = topSpeed / stopTime;
            Accel = topSpeed / accelTime;
        }

        protected virtual void OnEnable()
        {
            if (!moveAction.action.enabled)
                moveAction.action.Enable();
        }
    }
}
