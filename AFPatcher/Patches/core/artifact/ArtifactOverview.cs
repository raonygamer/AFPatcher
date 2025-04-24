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
            "add_strength_input_variable",
            "add_function_to_save_purify_settings",
            "add_function_to_purify_artifacts"
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
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.StrengthInput ]}}.text = g.{{[ core.scene.Game.Variables.CurrentStrength ]}};
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.StrengthInput ]}}.visible = true;
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.StrengthInput ]}}.isEnabled = true;
					addChild({{[ {ctx.PatchDescriptor.ClassName}.Variables.StrengthInput ]}});
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessInput ]}} = new InputText(458,453,40,25);
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessInput ]}}.restrict = ""0-9"";
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessInput ]}}.maxChars = 3;
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessInput ]}}.text = g.{{[ core.scene.Game.Variables.CurrentFitness ]}};
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessInput ]}}.visible = true;
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessInput ]}}.isEnabled = true;
					addChild({{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessInput ]}});
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.LineInput ]}} = new InputText(458,426,40,25);
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.LineInput ]}}.restrict = ""0-9"";
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.LineInput ]}}.maxChars = 3;
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.LineInput ]}}.text = g.{{[ core.scene.Game.Variables.CurrentLines ]}};
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.LineInput ]}}.visible = true;
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.LineInput ]}}.isEnabled = true;
					addChild({{[ {ctx.PatchDescriptor.ClassName}.Variables.LineInput ]}});
					addChild(new TextBitmap(500,430,Localize.t(""Lines""),12));
					addChild(new TextBitmap(500,457,Localize.t(""Fitness""),12));
					addChild(new TextBitmap(500,484,Localize.t(""Level""),12));
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifyButton ]}} = new Button({{[ {ctx.PatchDescriptor.ClassName}.Functions.PurifyArtifacts ]}},""Purify!"",""positive"");
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifyButton ]}}.x = 380;
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifyButton ]}}.y = 8 * 60;
					{{[ {ctx.PatchDescriptor.ClassName}.Variables.SaveStatsButton ]}} = new Button({{[ {ctx.PatchDescriptor.ClassName}.Functions.SavePurifySettings ]}},""Save!"",""positive"");
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
        
        [Patch("add_function_to_save_purify_settings", "Add function to save purify settings", [
            "initialize_shared_object_on_constructor",
            "add_current_fitness_variable",
            "add_current_lines_variable",
            "add_current_strength_variable"
        ])]
        public class AddFunctionToSavePurifySettings(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = ($@"
                    private function {{[ {ctx.PatchDescriptor.ClassName}.Functions.SavePurifySettings ]}}(e:TouchEvent = null): void {{
						g.{{[ core.scene.Game.Variables.CurrentFitness ]}} = int({{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessInput ]}}.text);
						g.{{[ core.scene.Game.Variables.CurrentStrength ]}} = int({{[ {ctx.PatchDescriptor.ClassName}.Variables.StrengthInput ]}}.text);
						g.{{[ core.scene.Game.Variables.CurrentLines ]}} = int({{[ {ctx.PatchDescriptor.ClassName}.Variables.LineInput ]}}.text);
						g.{{[ core.scene.Game.Functions.SaveSharedObject ]}}();
                        {{[ {ctx.PatchDescriptor.ClassName}.Variables.SaveStatsButton ]}}.enabled = true;
					}}
                ")
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+ArtifactOverview\s+extends\s+Sprite\s*()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_function_to_purify_artifacts", "Add function to purify artifacts", [
            "initialize_shared_object_on_constructor",
            "add_current_fitness_variable",
            "add_current_lines_variable",
            "add_current_strength_variable",
            "add_purified_artifacts_variable",
            "add_fitness_of_artifact_variable",
            "add_function_to_send_purify_recycle"
        ])]
        public class AddFunctionToPurifyArtifacts(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = ($@"
                    private function {{[ {ctx.PatchDescriptor.ClassName}.Functions.PurifyArtifacts ]}}(e:TouchEvent = null): void {{
						{{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifiedArtifacts ]}}.splice(0,{{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifiedArtifacts ]}}.length);
						var count:int = 0;
						for each(var cargoBox in cargoBoxes)
						{{
							if(count == 40)
							{{
								break;
							}}
							if(cargoBox.a != null)
							{{
								if(!cargoBox.a.revealed)
								{{
									if(cargoBox.a.stats.length < g.{{[ core.scene.Game.Variables.CurrentLines ]}} || cargoBox.a.{{[ core.artifact.Artifact.Variables.FitnessOfArtifact ]}} < g.{{[ core.scene.Game.Variables.CurrentFitness ]}} || cargoBox.a.level < g.{{[ core.scene.Game.Variables.CurrentStrength ]}})
									{{
										if(cargoBox.a.name != ""Recycle Generator"")
										{{
											cargoBox.setSelectedForRecycle();
											{{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifiedArtifacts ]}}.push(cargoBox.a);
											count++;
										}}
									}}
								}}
							}}
						}}
						{{[ {ctx.PatchDescriptor.ClassName}.Functions.SendPurifyRecycle ]}}();
					}}
                ")
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+ArtifactOverview\s+extends\s+Sprite\s*()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_function_to_send_purify_recycle", "Add function to send purify recycle", [
            "add_function_to_receive_purification_rpc"
        ])]
        public class AddFunctionToSendPurifyRecycle(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = ($@"
                    private function {{[ {ctx.PatchDescriptor.ClassName}.Functions.SendPurifyRecycle ]}}(): void {{
						if ({{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifiedArtifacts ]}}.length == 0)
						{{
							g.showErrorDialog(""No artifacts to recycle."");
							{{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifyButton ]}}.enabled = true;
							return;
						}}

						if (g.myCargo.isFull)
						{{
							g.showErrorDialog(Localize.t(""Your cargo compressor is overloaded!""));
							{{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifyButton ]}}.enabled = true;
							return;
						}}

						var recycleMessage:Message = g.createMessage(""bulkRecycle"");
						for each (var art in purifiedArts)
						{{
							recycleMessage.add(art.id);
						}}

                        g.rpcMessage(recycleMessage, {{[ {ctx.PatchDescriptor.ClassName}.Functions.OnPurifyMessage ]}});
					}}
                ")
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                
                Scope.Modify(
                    text,
                    @"public\s+class\s+ArtifactOverview\s+extends\s+Sprite\s*()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_function_to_receive_purification_rpc", "Add function to receive purification RPC", [
            "add_purified_artifacts_variable",
            "add_fitness_of_artifact_variable",
            "add_purify_button_variable"
        ])]
        public class AddFunctionToReceivePurificationRPC(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = ($@"
                    private function {{[ {ctx.PatchDescriptor.ClassName}.Functions.OnPurifyMessage ]}}(message:Message): void {{
						var i:int = 0;
						var success:Boolean = message.getBoolean(0);
						if (!success)
						{{
							g.showErrorDialog(message.getString(1));
							{{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifyButton ]}}.enabled = true;
							return;
						}}
						while (i < {{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifiedArtifacts ]}}.length)
						{{
							var art:Artifact = {{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifiedArtifacts ]}}[i];
							p.artifactCount -= 1;
							for each (var cargoBox in cargoBoxes)
							{{
								if (cargoBox.a == art)
								{{
									cargoBox.setEmpty();
									break;
								}}
							}}
							j = 0;
							while (j < p.artifacts.length)
							{{
								if (art == p.artifacts[j])
								{{
									p.artifacts.splice(j,1);
									break;
								}}
								j++;
							}}
							i++;
						}}
						if (p.artifactCount < p.artifactLimit)
						{{
							g.hud.hideArtifactLimitText();
						}}
						i = 1;
						while (i < message.length)
						{{
							var key:String = message.getString(i);
							var count:int = message.getInt(i + 1);
							g.myCargo.addItem(""Commodities"",key,count);
							i += 2;
						}}
						{{[ {ctx.PatchDescriptor.ClassName}.Variables.PurifyButton ]}}.enabled = true;
					}}
                ")
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+ArtifactOverview\s+extends\s+Sprite\s*()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
    }
}