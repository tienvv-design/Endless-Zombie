
using Unity.Entities;

public class SystemsUtils
{
    public static void SetSystemsEnabled(SimulationSystemGroup systemGroup, bool enabled)
    {
        if (systemGroup != null)
            systemGroup.Enabled = enabled;
    }
}
    
