using OOP.HFSMScripts;
using Unity.Entities;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace OOP.GameStates
{
    public class GamePausedState : GameState
    {
        private float _timeScaleBeforePause = 1f;

        public GamePausedState(GameStateMachineRunner context, GameStateFactory factory) : base(context, factory) { }

        public override void Init()
        {
        }

        public override void EnterState()
        {
            // Debug.Log("Entered paused state!");

            // Disabling the ECS simulation alone lets the world's clock advance.
            // When it is enabled again, fixed-step systems can catch up and make
            // enemies appear to jump forward. Freeze Unity's scaled clock too.
            if (Time.timeScale > 0f)
                _timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;

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

            Time.timeScale = Mathf.Max(0.01f, _timeScaleBeforePause);
        }

        public override void CheckSwitchState()
        {
        }
    }
}
