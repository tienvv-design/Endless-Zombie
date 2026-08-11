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
        private bool _gameplayStarted;
        private bool _gameOverPending;
        private bool _gameOverTriggered;
        private bool _winTriggered;
        private CharacterHealthManager _healthManager;

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
                if (behaviour is IGameOver || behaviour is IGameWin || behaviour is IGameLevelUp ||
                    behaviour is IGamePaused || behaviour is IGamePlayerPause)
                    behaviour.gameObject.SetActive(false);
            }

            WinMenu.EnsureExists();

            _factory = new GameStateFactory(this);
            _factory.WireStates();

            _gameState = _factory.GetGameState(GameStateType.Running);
            BindPlayerHealth();
            WaveSpawnLifecycle.StageCompleted += HandleStageCompleted;
        }

        private void Start()
        {
            BindPlayerHealth();
        }

        private void Update()
        {
            if (_gameplayStarted && !_gameOverTriggered &&
                (_gameOverPending || (_healthManager != null && _healthManager.IsDead)))
                TriggerGameOver();
            if (_gameplayStarted)
                State.UpdateStates(_gameState);
        }

        private void FixedUpdate()
        {
            if (_gameplayStarted)
                State.FixedUpdateStates(_gameState);
        }

        public void BeginGameplay()
        {
            if (_gameplayStarted)
                return;

            _gameplayStarted = true;
            _gameOverPending = false;
            _gameOverTriggered = false;
            _winTriggered = false;
            BindPlayerHealth();
            _healthManager?.ApplyMetaProgression();
            GoldWallet.Instance?.ResetRunReward();
            WaveSpawnLifecycle.BeginStage();
            State.EnterStates(_gameState);
        }

        public void SetRunnerState(State state)
        {
            _gameState = state;
        }

        public void PlayerDeathCallback()
        {
            _gameOverPending = true;
        }

        private void TriggerGameOver()
        {
            if (_gameOverTriggered || _winTriggered) return;
            _gameOverTriggered = true;
            _gameOverPending = false;
            WaveSpawnLifecycle.StopStage();
            GoldWallet.Instance?.BankRunReward();
            _gameState.SwitchState(_factory.GetGameState(GameStateType.GameOver));
        }

        private void HandleStageCompleted()
        {
            if (!_gameplayStarted || _gameOverTriggered || _winTriggered)
                return;

            _winTriggered = true;
            _gameOverPending = false;
            WaveSpawnLifecycle.StopStage();
            GoldWallet.Instance?.BankRunReward();
            _gameState.SwitchState(_factory.GetGameState(GameStateType.Win));
        }

        private void BindPlayerHealth()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null || !player.TryGetComponent(out CharacterHealthManager healthManager))
                return;
            if (_healthManager == healthManager) return;
            if (_healthManager != null)
                _healthManager.OnDeath -= PlayerDeathCallback;
            _healthManager = healthManager;
            _healthManager.OnDeath -= PlayerDeathCallback;
            _healthManager.OnDeath += PlayerDeathCallback;
        }

        private void OnDestroy()
        {
            WaveSpawnLifecycle.StageCompleted -= HandleStageCompleted;
            if (_healthManager != null)
                _healthManager.OnDeath -= PlayerDeathCallback;
        }

        public void OpenUpgradeShop()
        {
            _gameState.SwitchState(_factory.GetGameState(GameStateType.LevelUp));
        }
    }
}
