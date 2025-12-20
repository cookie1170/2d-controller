using UnityEngine;
#if COOKIE_UTILS
using CookieUtils.Debugging;
#endif

namespace Cookie.PlayerController
{
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class Controller2D : MonoBehaviour
#if COOKIE_UTILS
            , IDebugDrawer
#endif
    {
        [Header("References")]
        [SerializeField, Tooltip("The Rigidbody2D this controller should use")]
        protected Rigidbody2D rigidBody;

        /// <summary>
        /// The velocity with which the controller moves
        /// </summary>
        protected Vector2 Velocity;

        /// <summary>
        /// The result of the last slide
        /// </summary>
        protected Rigidbody2D.SlideResults LastResult;

        protected virtual void Awake()
        {
            Init();
        }

        protected virtual void OnValidate()
        {
            Init();
        }

        /// <summary>
        /// Should be overridden to return the slide movement configuration
        /// </summary>
        /// <returns>The Rigidbody2D.SlideMovement struct for configuration</returns>
        /// <seealso cref="Rigidbody2D.SlideMovement"/>
        protected abstract Rigidbody2D.SlideMovement GetSlideMovement();

        /// <summary>
        /// Moves the controller by the specified velocity using Rigidbody2D.Slide()
        /// </summary>
        /// <param name="velocity">
        /// The velocity to move by
        /// </param>
        protected virtual void Move(Vector2 velocity, float? deltaTime = null)
        {
            Rigidbody2D.SlideResults results = rigidBody.Slide(
                velocity,
                deltaTime ?? Time.deltaTime,
                GetSlideMovement()
            );

            LastResult = results;
        }

        /// <summary>
        /// Called in Awake and OnValidate, should be used for things like GetComponent calls and precomputing values
        /// </summary>
        protected virtual void Init()
        {
            if (!rigidBody)
                rigidBody = GetComponent<Rigidbody2D>();

            if (!rigidBody)
                return;

            rigidBody.bodyType = RigidbodyType2D.Kinematic;
            rigidBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            rigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

#if COOKIE_UTILS
        public virtual void SetUpDebugUI(IDebugUI_BuilderProvider provider)
        {
            IDebugUI_Builder builder = provider.GetFor(this);
            DrawControllerInfo(builder);
        }

        protected void DrawControllerInfo(IDebugUI_Builder builder)
        {
            builder.Vector2Field("Velocity", () => Velocity, (newVel) => Velocity = newVel);

            IDebugUI_Group lastResult = builder.FoldoutGroup("Last result");
            lastResult.IntField("Iterations", () => LastResult.iterationsUsed);
            lastResult.Vector2Field(
                "Remaining velocity",
                () => LastResult.remainingVelocity / Time.fixedDeltaTime
            );
            IDebugUI_If ifSurfaceHit = lastResult.IfGroup(() => LastResult.surfaceHit);
            ifSurfaceHit.Vector2Field("Normal", () => LastResult.surfaceHit.normal);
            IDebugUI_If ifSlideHit = ifSurfaceHit.ElseGroup().IfGroup(() => LastResult.slideHit);
            ifSlideHit.Vector2Field("Normal", () => LastResult.slideHit.normal);
        }
#endif
    }
}
