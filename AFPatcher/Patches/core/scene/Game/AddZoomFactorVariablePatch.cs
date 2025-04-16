using AFPatcher.Patching;
using AFPatcher.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.scene.Game
    {
        [Patch("add_zoom_factor_variable", "Add zoom factor variable", [], 2)]
        public class AddZoomFactorVariablePatch(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = 
                    $"public var {{[ {ctx.PatchDescriptor.ClassName}.Variables.ZoomFactor ]}}:Number = 1.0;"
                .ExpandTags(ctx.GlobalPatchContext).Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Game\s+extends\s+SceneBase()",
                    (info) => info.ScopeText.InsertTextAt(patchText, 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
        
                return new PatchResult(text);
            }
        }
    }
}