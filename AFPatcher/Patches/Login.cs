using AFPatcher.Patching;
using AFPatcher.Utility;
using Patching.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace Login
    {
        [Patch("add_client_version_and_credits_on_login_screen", "Add client version and credits on login screen", [])]
        public class AddClientVersionAndCreditsOnLoginScreen(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = (@$"
                    var developerCreditString:String = """";
                    for (var devName:String in Game.{{[ core.scene.Game.Variables.ClientDevelopers ]}}) {{
                        developerCreditString += ""  - "" + devName + ""\n"";
                    }}

                    var infoText:TextField = new TextField(0, 0, """", new TextFormat(""DAIDRR"", 12, 0xffffff));
				    infoText.x = 10;
				    infoText.y = 10;
				    infoText.autoSize = starling.text.TextFieldAutoSize.BOTH_DIRECTIONS;
				    infoText.format.horizontalAlign = starling.utils.Align.LEFT;
				    infoText.text = ""QoLAF (Server {{[ ServerVersion ]}} / {{[ ClientVersion ]}})\nContains modifications from:\n"" + developerCreditString;
				    addChild(infoText);
                ")
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Login\s+extends\s+Sprite\s*{.*?(?=(?:public|private|protected|internal)\s+function\s+init\s*\(.*?\)\s*:\s*\w+)()",
                    (info) => info.ScopeText.InsertTextAtGroupIndex(patchText, @"addChild\((?:this.)?logoContainer\);()if\(!RymdenRunt\.isDesktop\)"),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
    }
}