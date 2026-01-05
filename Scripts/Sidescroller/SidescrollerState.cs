using UnityEngine;

namespace Cookie.PlayerController
{
    public abstract class SidescrollerState
    {
        protected SidescrollerController Host;

        protected SidescrollerState(SidescrollerController host)
        {
            Host = host;
        }

        /// <summary>
        /// Called right before State.FixedUpdate
        /// </summary>
        /// <returns>The <c>StateChangeData</c> struct representing the next state or none</returns>
        public abstract StateChangeData GetNextState();
        public abstract void FixedUpdate();
        public abstract float GetGravity();

        public virtual void OnEnter(SidescrollerState prev, object[] extraParams) { }

        public virtual void OnExit(SidescrollerState next) { }

        public virtual Rigidbody2D.SlideMovement ModifySlideMovement(
            Rigidbody2D.SlideMovement movement
        ) => movement;
    }

    public struct StateChangeData
    {
        public static readonly StateChangeData None = new(null);

        public SidescrollerState NextState;
        public object[] ExtraParams;

        public static implicit operator StateChangeData(SidescrollerState state)
        {
            return new StateChangeData(state);
        }

        public StateChangeData(SidescrollerState nextState, params object[] extraParams)
        {
            NextState = nextState;
            ExtraParams = extraParams;
        }
    }
}
