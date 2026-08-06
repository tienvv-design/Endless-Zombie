
using Unity.Entities;

public class SystemsUtils
{
    public static void SetSystemsEnabled(SimulationSystemGroup systemGroup, bool enabled)
    {
        systemGroup.Enabled = true;
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<InitializationSystemGroup>().Enabled = enabled;
    }
}
    