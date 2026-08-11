// Monobehaviours that apply these interfaces will be active only in the corresponding states.

public interface IGameState
{
    public void OnStateEnable();
    public void OnStateDisable();
}

public interface IGameRunning : IGameState {}

public interface IGamePaused : IGameState {}
    
public interface IGameLevelUp : IGameState{}

public interface IGameOver : IGameState{}

public interface IGameWin : IGameState{}

public interface IGamePlayerPause : IGameState{}
