using System.Text.RegularExpressions;
using AFPatcher.Patching;
using AFPatcher.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.hud.components.chat.MessageLog
    {
        [Patch("add_echo_response", "Add echo. response message", [])]
        public class AddEchoResponse(string id, string name, string[] dependencies, int priority) : PatchBase(id, name, dependencies, priority)
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
                    if (param2 == ""echo."" && g.me.id != param3) {{
                        if (param1 == ""global"" || param1 == ""private"") {{
                            g.sendToServiceRoom(""chatMsg"", ""private"", param4, ""{echoMessage}"");
                        }}
                        else {{
                            g.sendToServiceRoom(""chatMsg"", ""local"", ""{echoMessage}"");
                        }}
                    }}")
                .ExpandTags(ctx.GlobalPatchContext).Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+MessageLog\s+extends\s+DisplayObjectContainer{.*?(?=public\s+static\s+function\s+writeChatMsg\s*\(.*?\)\s*:\s*\w+)()",
                    (info) => info.ScopeText.InsertTextAtGroupIndex(patchText, @"()if\s*\(g\.solarSystem\.type\s*==\s*""pvp dom""\s*&&\s*param\d+\s*==\s*""local""\)"),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText));
                return new PatchResult(text);
            }
        }
    }
}