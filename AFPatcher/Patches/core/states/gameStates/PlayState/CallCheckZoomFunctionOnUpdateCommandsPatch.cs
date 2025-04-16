using System.Formats.Asn1;
using System.Text.RegularExpressions;
using AFPatcher.Patching;
using AFPatcher.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.states.gameStates.PlayState
    {
        [Patch("call_check_zoom_function_on_update_commands", "Call check zoom function on updateCommands function", ["add_check_zoom_function"], 1)]
        public class CallCheckZoomFunctionOnUpdateCommandsPatch(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                Scope.Modify(
                    text,
                    @"public\s+class\s+PlayState\s+extends\s+GameState{.*?(?=public\s+function\s+updateCommands\s*\(.*?\)\s*:\s*\w+)()",
                    (info) => info.ScopeText.InsertTextAtGroupIndex($"{{[ {ctx.PatchDescriptor.ClassName}.Functions.CheckZoomFactor ]}}();".ExpandTags(ctx.GlobalPatchContext), @"(?:this.)?checkAccelerate\(\);()if\(!_loc\d+_\.isTeleporting\s*&&\s*!_loc\d+_\.usingBoost\)"),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText));
                return new PatchResult(text);
            }
        }
    }
}