using OOP.HFSMScripts;
using Unity.Entities;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace OOP.GameStates
{
    public class GamePausedState : GameState
    {
        public GamePausedState(GameStateMachineRunner context, GameStateFactory factory) : base(context, factory) { }

        public override void Init()
        {
        }

        public override void EnterState()
        {
            // Debug.Log("Entered paused state!");
            
            PlayerInput.Instance.InputActions.UI.Enable();
            
            AudioManager.Instance.SetMuffled(true);

            World world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                SimulationSystemGroup simulation = world.GetExistingSystemManaged<SimulationSystemGroup>();
                if (simulation != null) simulation.Enabled = false;
            }
            
            EnableMonoBehaviours<IGamePaused>();
        }

        protected override void UpdateState()
        {
        }

        public override void FixedUpdateState()
        {
        }

        public override void ExitState()
        {
            // Debug.Log("Exit paused state!");
            PlayerInput.Instance.InputActions.UI.Disable();
            World world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                SimulationSystemGroup simulation = world.GetExistingSystemManaged<SimulationSystemGroup>();
                if (simulation != null) simulation.Enabled = true;
            }
        }

        public override void CheckSwitchState()
        {
        }
    }
}
