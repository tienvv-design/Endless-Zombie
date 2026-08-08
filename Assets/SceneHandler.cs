using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneHandler : MonoBehaviour
{
    public void LoadSceneCallback(string name)
    {
        // Never change scenes while the ECS world is stopped. SubScene unloading is
        // processed by the player loop; leaving it stopped retains old singleton entities.
        World world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated)
        {
            world.QuitUpdate = false;
            SimulationSystemGroup simulation = world.GetExistingSystemManaged<SimulationSystemGroup>();
            if (simulation != null)
                simulation.Enabled = true;
        }

        if (name == "GameScene")
            WaveSpawnLifecycle.ResetStage();
        
        if (name == "MainMenu")
        {
            GoldWallet.Instance?.BankRunReward();
            PlayerInput.Instance?.InputActions.UI.Disable();
            PlayerInput.Instance?.InputActions.Player.Enable();
            name = "GameScene";
        }
        
        SceneManager.LoadScene(name);
    }
}
