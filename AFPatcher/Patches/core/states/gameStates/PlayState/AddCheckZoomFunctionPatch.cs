using AFPatcher.Patching;
using AFPatcher.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.states.gameStates.PlayState
    {
        [Patch("add_check_zoom_function", "Add function for checking zoom factor", ["add_zoom_factor_variable"])]
        public class AddCheckZoomFunctionPatch(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                /* Using variables from core.scene.Game
                 * {{[ core.scene.Game.Variables.ZoomFactor ]}}
                 */
                var patchText = (
                    $"public function {{[ {ctx.PatchDescriptor.ClassName}.Functions.CheckZoomFactor ]}}(): void {{" +
                    $"    if (input.isKeyDown(74)) {{" +
                    $"        g.{{[ core.scene.Game.Variables.ZoomFactor ]}} *= 0.98;" +
                    $"        g.camera.zoomFocus(1 * g.{{[ core.scene.Game.Variables.ZoomFactor ]}}, 4);" +
                    $"    }}" +
                    $"    if (input.isKeyDown(75)) {{" +
                    $"        g.{{[ core.scene.Game.Variables.ZoomFactor ]}} /= 0.98;" +
                    $"        g.camera.zoomFocus(1 * g.{{[ core.scene.Game.Variables.ZoomFactor ]}}, 4);" +
                    $"        if (input.isKeyDown(74)) {{" +
                    $"            g.{{[ core.scene.Game.Variables.ZoomFactor ]}} = 1.0;" +
                    $"            g.camera.zoomFocus(1 * g.{{[ core.scene.Game.Variables.ZoomFactor ]}}, 4);" +
                    $"        }}" +
                    $"    }}" +
                    $"}}")
                .ExpandTags(ctx.GlobalPatchContext).Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+PlayState\s+extends\s+GameState()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText));
                return new PatchResult(text);
            }
        }
    }
}