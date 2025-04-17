using System.Formats.Asn1;
using System.Text.RegularExpressions;
using AFPatcher.Patching;
using AFPatcher.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.unit.Unit 
    {
        [Patch("fix_unit_render_distance_for_zoom", "Fix Unit render distance for zoom", ["add_zoom_factor_variable"])]
        public class FixUnitRenderDistanceForZoom(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                return new PatchResult(GlobalPatches.FixDistanceToCameraForZoom(
                    ctx.Text,
                    @"public\s+class\s+Unit\s+extends\s+GameObject{.*?(?=(?:public|private|protected|internal)\s+function\s+updateIsNear\s*\(.*?\)\s*:\s*\w+)()",
                    ctx
                ));
            }
        }
    }
}