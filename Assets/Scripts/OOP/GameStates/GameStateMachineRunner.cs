using System;
using System.Collections.Generic;
using OOP.HFSMScripts;
using UnityEngine;

namespace OOP.GameStates
{
    public class GameStateMachineRunner : MonoBehaviour, IStateMachineRunner
    {
        private GameStateFactory _factory;
        private State _gameState;

        void Awake()
        {
            _factory = new GameStateFactory(this);
            _factory.WireStates();

            _gameState = _factory.GetGameState(GameStateType.Running);
        }

        private void Start()
        {
            State.EnterStates(_gameState);

            GameObject mainCharacter = GameObject.FindGameObjectWithTag("Player");
            if (mainCharacter.TryGetComponent(out CharacterHealthManager healthManager))
            {
                healthManager.OnDeath += PlayerDeathCallback;
            }
        }

        private void Update()
        {
            State.UpdateStates(_gameState);
        }

        private void FixedUpdate()
        {
            State.FixedUpdateStates(_gameState);
        }

        public void SetRunnerState(State state)
        {
            _gameState = state;
        }

        public void PlayerDeathCallback()
        {
            _gameState.SwitchState(_factory.GetGameState(GameStateType.GameOver));
        }
    }
}