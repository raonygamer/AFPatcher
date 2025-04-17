using System.Text.RegularExpressions;
using AFPatcher.Patching;
using AFPatcher.Utility;
using Patching.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.hud.components.chat.ChatInputText
    {
        [Patch("add_echo_command", "Add /echo command", [])]
        public class AddEchoCommand(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                /* Using tags
                 * {{[ EchoFormat ]}}
                 * {{[ EchoColor ]}}
                 * {{[ ServerVersion ]}}
                 * {{[ ClientVersion ]}}
                 */
                var echoMessage = string.Format(
                    $"{{[ EchoFormat ]}}".ExpandTags(ctx.GlobalPatchContext), 
                    $"{{[ EchoColor ]}}", 
                    $"{{[ ServerVersion ]}}", 
                    $"{{[ ClientVersion ]}}")
                    .ExpandTags(ctx.GlobalPatchContext);
                
                var patchText = ($@"
                    case ""echo"":
                        MessageLog.writeChatMsg(""death"",""Your client is: {echoMessage}"");
						break;
                    ")
                .ExpandTags(ctx.GlobalPatchContext).Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+ChatInputText\s+extends\s+Sprite\s*{.*?(?=private\s+function\s+sendMessage\s*\(.*?\)\s*:\s*\w+)()",
                    (info) => info.ScopeText.InsertTextAtGroupIndex(patchText, @"(?:.*)\s*=\s*parseCommand\((?:.*)\);\s*switch\((?:.*)\[0\]\)\s*{()"),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText));
                return new PatchResult(text);
            }
        }
        
        [Patch("add_recycle_command", "Add /rec,/recycle command", ["add_portable_recycle_function"])]
        public class AddRecycleCommand(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = ($@"
                    case ""rec"":
                    case ""recycle"":
                    case ""trash"":
                        MessageLog.writeChatMsg(""death"",""Opening portable recycle station."");
                        g.{{[ core.scene.Game.Functions.OpenPortableRecycle ]}}();
						break;
                    ")
                    .ExpandTags(ctx.GlobalPatchContext).Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+ChatInputText\s+extends\s+Sprite\s*{.*?(?=private\s+function\s+sendMessage\s*\(.*?\)\s*:\s*\w+)()",
                    (info) => info.ScopeText.InsertTextAtGroupIndex(patchText, @"(?:.*)\s*=\s*parseCommand\((?:.*)\);\s*switch\((?:.*)\[0\]\)\s*{()"),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText));
                return new PatchResult(text);
            }
        }
    }
}