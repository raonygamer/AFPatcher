using System.Formats.Asn1;
using System.Text.RegularExpressions;
using AFPatcher.Patching;
using AFPatcher.Utility;
using Patching.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.drops.Drop 
    {
        [Patch("fix_drop_render_distance_for_zoom", "Fix Drop render distance for zoom", ["add_zoom_factor_variable"])]
        public class FixDropRenderDistanceForZoom(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                return new PatchResult(GlobalPatches.FixDistanceToCameraForZoom(
                    ctx.Text,
                    @"public\s+class\s+Drop\s+extends\s+GameObject{.*?(?=(?:public|private|protected|internal)\s+function\s+updateIsNear\s*\(.*?\)\s*:\s*\w+)()",
                    ctx
                ));
            }
        }
        
        [Patch("increase_unique_artifact_crate_size", "Increase unique artifact crate size", [])]
        public class IncreaseUniqueArtifactCrateSize(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = ($@"
                    for each (var e:core.particle.Emitter in effect) {{
						e.startSize *= 1.5;
						e.finishSize *= 1.5;
						scaleX *= 1.5;
                        scaleY *= 1.5;
					}}
                ").Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Drop\s+extends\s+GameObject{.*?(?=(?:public|private|protected|internal)\s+function\s+addToCanvasForReal\s*\(.*?\)\s*:\s*\w+)()",
                    (info) => info.ScopeText.InsertTextAtGroupIndex(patchText, @"effect\[\d+\]\.startColor\s*=\s*[^""]+;()effect\[\d+\]\.finishColor\s*=\s*[^""]+;"),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText));
                return new PatchResult(text);
            }
        }
    }
}