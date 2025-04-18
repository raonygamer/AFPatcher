using System.Globalization;
using System.Text.RegularExpressions;
using AFPatcher.Patching;
using AFPatcher.Utility;
using Patching.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.hud.components.techTree.TechTree
    {
        [Patch("reduce_weapon_upgrade_animation_time", "Reduce weapon upgrade animation time", [])]
        public class ReduceWeaponUpgradeAnimationTime(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                if (!ctx.GlobalPatchContext.TryGetTag<double>("UpgradeAnimationTimeReduction", out var reduction))
                    throw new PatchFailedException("UpgradeAnimationTimeReduction tag could not be found");
                
                var text = ctx.Text;
                Scope.Modify(
                    text,
                    @"public\s+class\s+TechTree\s+extends\s+Sprite()",
                    (info) => Regex.Replace(info.ScopeText, @"TweenMax\.from\s*\(\s*([^,]*?)\s*,\s*([0-9.+\-eE]+)\s*,", $"TweenMax.from($1, $2 * {reduction.ToString("F", CultureInfo.InvariantCulture)},"),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText));
                return new PatchResult(text);
            }
        }
    }
}