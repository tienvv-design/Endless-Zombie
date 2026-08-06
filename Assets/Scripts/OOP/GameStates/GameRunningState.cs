using OOP.HFSMScripts;
using Unity.Entities;
using UnityEngine;

namespace OOP.GameStates
{
    public class GameRunningState : GameState
    {
        private SimulationSystemGroup _simulationSystemGroup;
        
        public GameRunningState(GameStateMachineRunner context, GameStateFactory factory) : base(context, factory) { }

        public override void Init()
        {
        }

        public override void EnterState()
        {
            // Debug.Log("Game is running!");
            PlayerInput.Instance.InputActions.Player.Enable();
            
            EnableMonoBehaviours<IGameRunning>(true);

            if (!AudioManager.Instance.IsPlaying(SoundLabel.InGameMusic))
            {
                AudioManager.Instance.Play(SoundLabel.InGameMusic);   
            }
            AudioManager.Instance.SetMuffled(false);
            
            World.DefaultGameObjectInjectionWorld.QuitUpdate = false;
            // SystemsUtils.SetSystemsEnabled(World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SimulationSystemGroup>(), true);
            
            CharacterXPManager.Instance.OnLevelUp += LevelUpCallback;
        }

        protected override void UpdateState()
        {
            CheckSwitchState();      
        }

        public override void FixedUpdateState()
        {
            // Debug.Log("Running state");
        }

        public override void ExitState()
        {
            PlayerInput.Instance.InputActions.Player.Disable();

            CharacterXPManager.Instance.OnLevelUp -= LevelUpCallback;
        }

        public override void CheckSwitchState()
        {
            if (PlayerInput.Instance.InputActions.Player.Pause.WasPerformedThisFrame())
            {
                SwitchState(_factory.GetGameState(GameStateType.PlayerPause));
            }
        }

        private void LevelUpCallback(int level)
        {
            SwitchState(_factory.GetGameState(GameStateType.LevelUp));
        }
    }
}