using System;
using System.Collections.Generic;
using OOP.HFSMScripts;
using UnityEngine;
using Unity.Entities;

namespace OOP.GameStates
{
    public class GameStateMachineRunner : MonoBehaviour, IStateMachineRunner
    {
        private GameStateFactory _factory;
        private State _gameState;

        void Awake()
        {
            // A new GameScene must always start from a clean running presentation,
            // even when the previous scene was left from the paused GameOver state.
            Time.timeScale = 1f;
            if (World.DefaultGameObjectInjectionWorld != null)
            {
                World.DefaultGameObjectInjectionWorld.QuitUpdate = false;
                SimulationSystemGroup simulation = World.DefaultGameObjectInjectionWorld
                    .GetExistingSystemManaged<SimulationSystemGroup>();
                if (simulation != null) simulation.Enabled = true;
            }

            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour is IGameOver || behaviour is IGameLevelUp ||
                    behaviour is IGamePaused || behaviour is IGamePlayerPause)
                    behaviour.gameObject.SetActive(false);
            }

            _factory = new GameStateFactory(this);
            _factory.WireStates();

            _gameState = _factory.GetGameState(GameStateType.Running);
        }

        private void Start()
        {
            GoldWallet.Instance?.ResetRunReward();
            State.EnterStates(_gameState);

            GameObject mainCharacter = GameObject.FindGameObjectWithTag("Player");
            if (mainCharacter != null && mainCharacter.TryGetComponent(out CharacterHealthManager healthManager))
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
            GoldWallet.Instance?.BankRunReward();
            _gameState.SwitchState(_factory.GetGameState(GameStateType.GameOver));
        }

        private void OnDestroy()
        {
            GameObject mainCharacter = GameObject.FindGameObjectWithTag("Player");
            if (mainCharacter != null && mainCharacter.TryGetComponent(out CharacterHealthManager healthManager))
                healthManager.OnDeath -= PlayerDeathCallback;
        }

        public void OpenUpgradeShop()
        {
            _gameState.SwitchState(_factory.GetGameState(GameStateType.LevelUp));
        }
    }
}
