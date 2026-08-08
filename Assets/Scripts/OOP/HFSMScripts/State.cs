using UnityEngine;

namespace OOP.HFSMScripts
{
    public abstract class State 
    {
        protected IStateMachineRunner _context;
        protected State m_SubState;
        protected State m_SuperState;

        public State(IStateMachineRunner context) => _context = context;
        
        public State(){}

        public State SubState => m_SubState;
        
        public abstract void Init();
        public abstract void EnterState();

        protected abstract void UpdateState();
        
        public abstract void FixedUpdateState();

        public abstract void ExitState();

        public abstract void CheckSwitchState();
        
        public static void UpdateStates(State state){ // This function allows for a chained multi-substate architecture by calling update of every substate of supdates.
            if (state.m_SuperState != null)
            {
                UpdateStates(state.m_SuperState);
            }
            
            // Debug.Log("Updating state: " + state.GetType());
            state.UpdateState();
        }

        public static void FixedUpdateStates(State state){
            if (state.m_SuperState != null)
            {
                FixedUpdateStates(state.m_SuperState);
            }
            
            state.FixedUpdateState();
        }
        
        public static void EnterStates(State state)
        {
            if (state.m_SuperState != null)
            {
                EnterStates(state.m_SuperState);
            }
            state.EnterState();
        }

        public static void ExitStates(State state){
            if (state.m_SuperState != null)
            {
                ExitStates(state.m_SuperState);
            }
            
            state.ExitState();
        }

        public void SwitchState(State newState)
        {
            ExitStates(this);
            EnterStates(newState);
            
            _context.SetRunnerState(newState);
        }

        protected void SetSuperState(State superState){
            m_SuperState = superState;
            superState.m_SubState = this;
        }
    }
}
