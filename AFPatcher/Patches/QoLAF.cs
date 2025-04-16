using System.Reflection;
using AFPatcher.Patching;

namespace AFPatcher.Patches;

public class QoLAF
{
    public static GlobalPatchContext GetGlobalPatchContext()
    {
        return new GlobalPatchContext(new Dictionary<string, object>
        {
            { 
                "core.scene.Game.Variables", 
                new Dictionary<string, string>
                {
                    { "ZoomFactor", "zoomFactor" }
                } 
            },
            { 
                "core.states.gameStates.PlayState.Functions", 
                new Dictionary<string, string>
                {
                    { "CheckZoomFactor", "checkZoomFactor" }
                } 
            }
        });
    }
    
    public static PatchDescriptor[] GetPatchDescriptors()
    {
        return [
            new PatchDescriptor("core.scene.Game", []),
            new PatchDescriptor("core.states.gameStates.PlayState", [])
        ];
    }
}