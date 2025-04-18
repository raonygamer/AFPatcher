using System.Formats.Asn1;
using System.Text.RegularExpressions;
using AFPatcher.Patching;
using AFPatcher.Utility;
using Patching.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace camerafocus.StarlingCameraFocus
    {
        [Patch("remove_screen_shake", "Remove screen shake", [])]
        public class RemoveScreenShake(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                Scope.Modify(
                    text,
                    @"public\s+final\s+class\s+StarlingCameraFocus\s*{.*?(?=(?:public|private|protected|internal)\s+function\s+shake\s*\(.*?\)\s*:\s*\w+)()",
                    _ => "{}",
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
    }
}