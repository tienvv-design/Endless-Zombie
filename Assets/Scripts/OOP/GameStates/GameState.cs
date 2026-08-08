using OOP.HFSMScripts;
using UnityEngine;

namespace OOP.GameStates
{
    public abstract class GameState : State
    {
        protected new GameStateMachineRunner _context;
        protected GameStateFactory _factory;

        public static void EnableMonoBehaviours<T>( bool disableOthers = true) where T : IGameState
        {
            foreach (var monoBehaviour in GameObject.FindObjectsByType<MonoBehaviour>
                         (FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
            {
                if (monoBehaviour is T)
                {
                    (monoBehaviour as IGameState).OnStateEnable();
                }
                else if(disableOthers && monoBehaviour is IGameState)
                {
                    (monoBehaviour as IGameState).OnStateDisable();
                }
            }
        }
        
        public GameState(GameStateMachineRunner context, GameStateFactory factory) : base(context)
        {
            _context = context;
            _factory = factory;
        }
    }
}
