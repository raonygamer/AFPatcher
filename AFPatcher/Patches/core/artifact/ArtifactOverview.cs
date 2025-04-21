using System.Text.RegularExpressions;
using AFPatcher.Patching;
using AFPatcher.Utility;
using Patching.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.artifact.ArtifactOverview
    {
        [Patch("add_purified_artifacts_variable", "Add purified artifacts variable", ["update_fitness_of_artifact_in_update_function"])]
        public class AddFitnessOfArtifactVariable(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText =
                    $"private var {{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifiedArtifacts ]}}:Vector.<Artifact> = new Vector.<Artifact>();"
                        .ExpandTags(ctx.GlobalPatchContext)
                        .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+ArtifactOverview\s+extends\s+Sprite\s*()",
                    (info) => info.ScopeText.InsertTextAt(patchText, 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_purify_button_variable", "Add purify button variable", ["update_fitness_of_artifact_in_update_function"])]
        public class AddPurifyButtonVariable(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText =
                    $"private var {{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifyButton ]}}:Button;"
                        .ExpandTags(ctx.GlobalPatchContext)
                        .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+ArtifactOverview\s+extends\s+Sprite\s*()",
                    (info) => info.ScopeText.InsertTextAt(patchText, 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_save_stats_button_variable", "Add save stats button variable", ["update_fitness_of_artifact_in_update_function"])]
        public class AddSaveStatsButtonVariable(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText =
                    $"private var {{[ {ctx.PatchDescriptor.ClassName}.Variables.SaveStatsButton ]}}:Button;"
                        .ExpandTags(ctx.GlobalPatchContext)
                        .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+ArtifactOverview\s+extends\s+Sprite\s*()",
                    (info) => info.ScopeText.InsertTextAt(patchText, 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_fitness_input_variable", "Add fitness input variable", ["update_fitness_of_artifact_in_update_function"])]
        public class AddFitnessInputVariable(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText =
                    $"private var {{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessInput ]}}:InputText;"
                        .ExpandTags(ctx.GlobalPatchContext)
                        .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+ArtifactOverview\s+extends\s+Sprite\s*()",
                    (info) => info.ScopeText.InsertTextAt(patchText, 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_line_input_variable", "Add line input variable", ["update_fitness_of_artifact_in_update_function"])]
        public class AddLineInputVariable(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText =
                    $"private var {{[ {ctx.PatchDescriptor.ClassName}.Variables.LineInput ]}}:InputText;"
                        .ExpandTags(ctx.GlobalPatchContext)
                        .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+ArtifactOverview\s+extends\s+Sprite\s*()",
                    (info) => info.ScopeText.InsertTextAt(patchText, 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_strength_input_variable", "Add strength input variable", ["update_fitness_of_artifact_in_update_function"])]
        public class AddStrengthInputVariable(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText =
                    $"private var {{[ {ctx.PatchDescriptor.ClassName}.Variables.StrengthInput ]}}:InputText;"
                        .ExpandTags(ctx.GlobalPatchContext)
                        .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+ArtifactOverview\s+extends\s+Sprite\s*()",
                    (info) => info.ScopeText.InsertTextAt(patchText, 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_purification_components_to_drawComponents", "Add purification components to drawComponents", [
            "add_purified_artifacts_variable",
            "add_purify_button_variable",
            "add_save_stats_button_variable",
            "add_fitness_input_variable",
            "add_line_input_variable",
            "add_strength_input_variable"
        ])]
        public class AddPurificationComponentsToDrawComponents(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = ($@"
                    {{[ {ctx.PatchDescriptor.ClassName}.Variables.StrengthInput ]}} = new InputText(458,8 * 60,40,25);
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.StrengthInput ]}}.restrict = ""0-9"";
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.StrengthInput ]}}.maxChars = 3;
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.StrengthInput ]}}.text = ""OOF 1"";
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.StrengthInput ]}}.visible = true;
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.StrengthInput ]}}.isEnabled = true;
					addChild({{[ {ctx.PatchDescriptor.ClassName}.Variables.StrengthInput ]}});
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessInput ]}} = new InputText(458,453,40,25);
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessInput ]}}.restrict = ""0-9"";
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessInput ]}}.maxChars = 3;
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessInput ]}}.text = ""OOF 2"";
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessInput ]}}.visible = true;
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessInput ]}}.isEnabled = true;
					addChild({{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessInput ]}});
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.LineInput ]}} = new InputText(458,426,40,25);
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.LineInput ]}}.restrict = ""0-9"";
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.LineInput ]}}.maxChars = 3;
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.LineInput ]}}.text = ""OOF 3"";
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.LineInput ]}}.visible = true;
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.LineInput ]}}.isEnabled = true;
					addChild({{[ {ctx.PatchDescriptor.ClassName}.Variables.LineInput ]}});
					addChild(new TextBitmap(500,430,Localize.t(""Lines""),12));
					addChild(new TextBitmap(500,457,Localize.t(""Fitness""),12));
					addChild(new TextBitmap(500,484,Localize.t(""Level""),12));
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifyButton ]}} = new Button(function ():* {{}},""Purify!"",""positive"");
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifyButton ]}}.x = 380;
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifyButton ]}}.y = 8 * 60;
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.SaveStatsButton ]}} = new Button(function ():* {{}},""Save!"",""positive"");
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.SaveStatsButton ]}}.x = {{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifyButton ]}}.x;
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.SaveStatsButton ]}}.y = {{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifyButton ]}}.y - {{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifyButton ]}}.height - 10;
					addChild({{[ {ctx.PatchDescriptor.ClassName}.Variables.SaveStatsButton ]}});
					addChild({{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifyButton ]}});
                ")
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+ArtifactOverview\s+extends\s+Sprite\s*{.*?(?=(?:public|private|protected|internal)\s+function\s+drawComponents\s*\(.*?\)\s*:\s*\w+)()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
    }
}