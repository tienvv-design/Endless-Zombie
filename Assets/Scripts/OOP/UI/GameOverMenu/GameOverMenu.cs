using UnityEngine;

public class GameOverMenu : MonoBehaviour, IGameOver
{
    public void OnStateEnable()
    {
        gameObject.SetActive(true);
    }

    public void OnStateDisable()
    {
        gameObject.SetActive(false);
    }
}
