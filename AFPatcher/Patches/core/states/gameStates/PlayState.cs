using System.Formats.Asn1;
using System.Text.RegularExpressions;
using AFPatcher.Patching;
using AFPatcher.Utility;
using Patching.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.states.gameStates.PlayState
    {
        [Patch("call_check_zoom_function_on_update_commands", "Call check zoom function on updateCommands function", ["add_check_zoom_function"])]
        public class AddCheckZoomCallOnUpdateCommands(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = ($@"
                    {{[ {ctx.PatchDescriptor.ClassName}.Functions.CheckZoomFactor ]}}();
                ")
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+PlayState\s+extends\s+GameState{.*?(?=public\s+function\s+updateCommands\s*\(.*?\)\s*:\s*\w+)()",
                    (info) => info.ScopeText.InsertTextAtGroupIndex(patchText, @"()if\(!_loc\d+_\.isTeleporting\s*&&\s*!_loc\d+_\.usingBoost\)"),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText));
                return new PatchResult(text);
            }
        }
        
        [Patch("call_check_custom_commands_function_on_update_commands", "Call check custom commands function on updateCommands function", ["add_check_custom_commands_function"])]
        public class AddCheckCustomCommandsCallOnUpdateCommands(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = ($@"
                    {{[ {ctx.PatchDescriptor.ClassName}.Functions.CheckCustomCommands ]}}();
                ")
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+PlayState\s+extends\s+GameState{.*?(?=public\s+function\s+updateCommands\s*\(.*?\)\s*:\s*\w+)()",
                    (info) => info.ScopeText.InsertTextAtGroupIndex(patchText, @"()if\(!_loc\d+_\.isTeleporting\s*&&\s*!_loc\d+_\.usingBoost\)"),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText));
                return new PatchResult(text);
            }
        }
        
        [Patch("add_check_zoom_function", "Add function for checking zoom factor", ["add_zoom_factor_variable"])]
        public class AddCheckZoomFunction(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
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
        
        [Patch("add_check_custom_commands_function", "Add function for checking custom commands", [])]
        public class AddCheckCustomCommandsFunction(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = ($@"
                    public function {{[ {ctx.PatchDescriptor.ClassName}.Functions.CheckCustomCommands ]}}(): void {{
                        if (input.isKeyDown(17) && input.isKeyPressed(82)) {{
                            core.hud.components.chat.MessageLog.writeChatMsg(""death"",""Reloading system."");
                            starling.core.Starling.juggler.delayCall(g.reload, 0.25);
                        }}
                    }}
                ")
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+PlayState\s+extends\s+GameState()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText));
                return new PatchResult(text);
            }
        }
        
        [Patch("fix_camera_zoom_focus_calls_for_zoom", "Fix camera zoom focus calls for zoom", ["add_zoom_factor_variable"], 1)]
        public class FixCameraZoomFocusCallsForZoom(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
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