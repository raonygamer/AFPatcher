using System.Text.RegularExpressions;
using AFPatcher.Patching;
using AFPatcher.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.states.gameStates.PlayState
    {
        [Patch("fix_camera_zoom_focus_calls_for_zoom", "Fix camera zoom focus calls for zoom", ["add_zoom_factor_variable"])]
        public class FixCameraZoomFocusCallsForZoomPatch(string id, string name, string[] dependencies) : PatchBase(id, name, dependencies)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                Scope.Modify(
                    text,
                    @"public\s+class\s+PlayState\s+extends\s+GameState()",
                    (info) => Regex.Replace(info.ScopeText, @"g\.camera\.zoomFocus\(\s*([\d.]+)\s*,\s*([\d.]+)\s*\);", $"g.camera.zoomFocus($1 * g.{{[ core.scene.Game.Variables.ZoomFactor ]}}, $2);".ExpandTags(ctx.GlobalPatchContext)),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText));
                return new PatchResult(text);
            }
        }
    }
}