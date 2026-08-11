using System;
using System.Collections.Generic;
using OOP.HFSMScripts;
using UnityEngine;

namespace OOP.GameStates
{
    public enum GameStateType
    {
        Running,
        Paused,
        PlayerPause,
        LevelUp,
        GameOver,
        Win,
    }
    
    public class GameStateFactory
    {
        private Dictionary<GameStateType, State> _gameStates; 
        
        public GameStateFactory(GameStateMachineRunner stateMachineRunner)
        {
            _gameStates = new Dictionary<GameStateType, State>();

            foreach (var type in Enum.GetValues(typeof(GameStateType)))
            {
                switch (type)
                {
                    case GameStateType.Running:
                        _gameStates.Add(GameStateType.Running, new GameRunningState(stateMachineRunner, this));
                        break;
                    case GameStateType.Paused:
                        _gameStates.Add(GameStateType.Paused, new GamePausedState(stateMachineRunner, this));
                        break;
                    case GameStateType.PlayerPause:
                        _gameStates.Add(GameStateType.PlayerPause, new GamePlayerPausedState(stateMachineRunner, this));
                        break;
                    case GameStateType.LevelUp:
                        _gameStates.Add(GameStateType.LevelUp, new LevelUpState(stateMachineRunner, this));
                        break;
                    case GameStateType.GameOver:
                        _gameStates.Add(GameStateType.GameOver, new GameOverState(stateMachineRunner, this));
                        break;
                    case GameStateType.Win:
                        _gameStates.Add(GameStateType.Win, new WinState(stateMachineRunner, this));
                        break;
                    default:
                        break;
                }
            }
            
        }

        public State GetGameState(GameStateType stateType)
        {
            _gameStates.TryGetValue(stateType, out State gameState);
            if (gameState == null)
            {
                return null;
            }

            return gameState;
        }

        public void WireStates()
        {
            foreach (GameStateType type in Enum.GetValues(typeof(GameStateType)))
            {
                GetGameState(type)?.Init();
            }
        }
        
    }
}
