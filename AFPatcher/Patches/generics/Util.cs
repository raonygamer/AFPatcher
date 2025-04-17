using AFPatcher.Patching;
using AFPatcher.Utility;
using Patching.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace generics.Util
    {
        [Patch("add_object_has_value_function", "Add function for checking if object has value", [])]
        public class AddObjectHasValueFunction(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = (@$"
                    public static function {{[ {ctx.PatchDescriptor.ClassName}.Functions.HasValue ]}}(obj:Object, value:*): Boolean {{
                        for each (var val:* in obj) {{
                            if (val === value) {{
                                return true;
                            }}
                        }}
                        return false;
                    }}")
                    .ExpandTags(ctx.GlobalPatchContext).Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Util()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText));
                return new PatchResult(text);
            }
        }
    }
}