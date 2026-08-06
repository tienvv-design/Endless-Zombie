using UnityEngine;

namespace OOP.GameStates
{
    public class LevelUpState : GameState
    {
        public LevelUpState(GameStateMachineRunner context, GameStateFactory factory) : base(context, factory)
        {
        }

        public override void Init()
        {
            SetSuperState(_factory.GetGameState(GameStateType.Paused));
        }

        public override void EnterState()
        {
            // Debug.Log("Entered Level up state!");
            Time.timeScale = 0f;
            EnableMonoBehaviours<IGameLevelUp>(false);
            
            AudioManager.Instance.Play(SoundLabel.LevelUpSound);

            LevelUpManager.Instance.OnUpgradeApplied += OnUpgradeAppliedCallback;
        }

        protected override void UpdateState()
        {
            
        }

        public override void FixedUpdateState()
        {
            
        }

        public override void ExitState()
        {
            LevelUpManager.Instance.OnUpgradeApplied -= OnUpgradeAppliedCallback;
            Time.timeScale = 1f;
        }

        public override void CheckSwitchState()
        {
            
        }

        public void OnUpgradeAppliedCallback(CharUpgrade upgrade)
        {
            SwitchState(_factory.GetGameState(GameStateType.Running));
        }
    }
}
