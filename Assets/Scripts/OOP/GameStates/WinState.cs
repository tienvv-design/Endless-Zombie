using UnityEngine;

namespace OOP.GameStates
{
    public sealed class WinState : GameState
    {
        public WinState(GameStateMachineRunner context, GameStateFactory factory) : base(context, factory) { }

        public override void Init()
        {
            SetSuperState(_factory.GetGameState(GameStateType.Paused));
        }

        public override void EnterState()
        {
            EnableMonoBehaviours<IGameWin>(false);
        }

        protected override void UpdateState() { }
        public override void FixedUpdateState() { }
        public override void ExitState() { }
        public override void CheckSwitchState() { }
    }
}
