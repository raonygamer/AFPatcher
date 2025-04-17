using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using AFPatcher.Patching;
using AFPatcher.Utility;

namespace AFPatcher.Patches;

public class GlobalPatches
{
    public static string FixDistanceToCameraForZoom(string text, [StringSyntax("Regex")] string scopeFindingPattern, PatchContext ctx)
    {
        Scope.Modify(
            text,
            scopeFindingPattern,
            (info) => Regex.Replace(
                info.ScopeText,
                @"if\s*\(distanceToCamera\s*<\s*_loc(\d+)_\)",
                $"if (distanceToCamera * g.{{[ core.scene.Game.Variables.ZoomFactor ]}} < _loc$1_)"
                    .ExpandTags(ctx.GlobalPatchContext)),
            (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText));
        return text;
    }
}