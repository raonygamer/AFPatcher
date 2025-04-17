﻿using AFPatcher.Patching;
using AFPatcher.Utility;
using Patching.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.scene.Game
    {
        [Patch("add_zoom_factor_variable", "Add zoom factor variable", [], 1)]
        public class AddZoomFactorVariable(string id, string name, string[] dependencies, int priority) 
            : PatchBase(id, name, dependencies, priority)
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

        [Patch("add_portable_recycle_function", "Add portable recycle function", [])]
        public class AddPortableRecycleFunction(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = (@$"
                    public function {{[ {ctx.PatchDescriptor.ClassName}.Functions.OpenPortableRecycle ]}}(): void {{
                        var root:Body = bodyManager.getRoot();
                        root.name = ""Portable recycle"";
                        fadeIntoState(new LandedRecycle(this, root));
                    }}")
                .ExpandTags(ctx.GlobalPatchContext).Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Game\s+extends\s+SceneBase()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
    }
}