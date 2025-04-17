using AFPatcher.Patching;
using AFPatcher.Utility;
using Patching.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.ship.EnemyShip
    {
        [Patch("disable_enemy_cloaking", "Disable enemy cloaking", [])]
        public class DisableEnemyCloaking(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                Scope.Modify(
                    text,
                    @"public\s+class\s+EnemyShip\s+extends\s+Ship{.*?(?=(?:public|private|protected|internal)\s+function\s+cloakStart\s*\(.*?\)\s*:\s*\w+)()",
                    _ => "{}",
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                Scope.Modify(
                    text,
                    @"public\s+class\s+EnemyShip\s+extends\s+Ship{.*?(?=(?:public|private|protected|internal)\s+function\s+cloakEnd\s*\(.*?\)\s*:\s*\w+)()",
                    _ => "{}",
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
    }
}