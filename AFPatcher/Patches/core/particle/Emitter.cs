using System.Formats.Asn1;
using System.Text.RegularExpressions;
using AFPatcher.Patching;
using AFPatcher.Utility;
using Patching.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.particle.Emitter 
    {
        [Patch("fix_emitter_render_distance_for_zoom", "Fix Emitter render distance for zoom", ["add_zoom_factor_variable"])]
        public class FixEmitterRenderDistanceForZoom(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                Scope.Modify(
                    text,
                    @"public\s+class\s+Emitter{.*?(?=(?:public|private|protected|internal)\s+function\s+updateOnScreen\s*\(.*?\)\s*:\s*\w+)()",
                    (info) => Regex.Replace(
                        info.ScopeText,
                        @"nextDistanceCalculation\s*=\s*(.*?);",
                        $"nextDistanceCalculation = ($1) / g.{{[ core.scene.Game.Variables.ZoomFactor ]}};".ExpandTags(ctx.GlobalPatchContext)),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText));
                return new PatchResult(text);
            }
        }
    }
}