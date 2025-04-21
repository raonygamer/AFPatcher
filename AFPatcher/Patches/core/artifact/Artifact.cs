using System.Text.RegularExpressions;
using AFPatcher.Patching;
using AFPatcher.Utility;
using Patching.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.artifact.Artifact
    {
        [Patch("add_fitness_of_artifact_variable", "Add fitness of artifact variable", ["initialize_fitness_of_line_in_constructor"])]
        public class AddFitnessOfArtifactVariable(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText =
                    $"public var {{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessOfArtifact ]}}:Number = 0.0;"
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Artifact\s*()",
                    (info) => info.ScopeText.InsertTextAt(patchText, 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_function_to_calculate_artifact_fitness", "Add function to calculate artifact fitness", ["add_fitness_of_artifact_variable"])]
        public class AddFunctionToCalculateArtifactFitness(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = (@$"
                    public function {{[ {ctx.PatchDescriptor.ClassName}.Functions.CalculateFitnessOfArtifact ]}}():Number {{
                        var fitness:Number = 0;
                        for (var i:int = 0; i < stats.length; i++) {{
                            fitness += stats[i].{{[ core.artifact.ArtifactStat.Variables.FitnessOfLine ]}};
                        }} 
						return fitness;
                    }}
                ")
	                .ExpandTags(ctx.GlobalPatchContext)
	                .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Artifact\s*()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("update_fitness_of_artifact_in_update_function", "Update fitness of artifact in update function", ["add_function_to_calculate_artifact_fitness"])]
        public class UpdateFitnessOfArtifactInUpdateFunction(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = (@$"
					this.{{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessOfArtifact ]}} = {{[ {ctx.PatchDescriptor.ClassName}.Functions.CalculateFitnessOfArtifact ]}}();
                ")
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Artifact\s*{.*?(?=(?:public|private|protected|internal)\s+function\s+update\s*\(.*?\)\s*:\s*\w+)()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("change_asc_ordering_to_unique_ordering", "Change ASC ordering to Unique ordering", ["add_fitness_of_artifact_variable"])]
        public class ChangeASCOrderingToUniqueOrdering(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = (@$"
					this.{{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessOfArtifact ]}} = {{[ {ctx.PatchDescriptor.ClassName}.Functions.CalculateFitnessOfArtifact ]}}();
                ")
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Artifact\s*{.*?(?=(?:public|private|protected|internal)\s+static\s+function\s+orderStatCountAsc\s*\(.*?\)\s*:\s*\w+)()",
                    (info) =>
                    {
                        var scopeText = Regex.Replace(info.ScopeText, @"var\s+_loc(\d*)_:Number\s+=\s+param(\d*)\.stats\.length;", "");
                        scopeText = Regex.Replace(scopeText, @"if\(_loc(\d*)_\s+>\s+_loc(\d*)_\)",
                            "if(!param1.isUnique && param2.isUnique)");
                        scopeText = Regex.Replace(scopeText, @"if\(_loc(\d*)_\s+<\s+_loc(\d*)_\)",
                            "if(param1.isUnique && !param2.isUnique)");
                        return scopeText;
                    },
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("change_desc_ordering_to_upgraded_ordering", "Change DESC ordering to Upgraded ordering", ["add_fitness_of_artifact_variable"])]
        public class ChangeDESCOrderingToUpgradedOrdering(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                Scope.Modify(
                    text,
                    @"public\s+class\s+Artifact\s*{.*?(?=(?:public|private|protected|internal)\s+static\s+function\s+orderStatCountDesc\s*\(.*?\)\s*:\s*\w+)()",
                    (info) => Regex.Replace(info.ScopeText, @"var\s+_loc(\d*)_:Number\s+=\s+param(\d*)\.stats\.length;", "var _loc$1_:Number = param$2.upgraded;"),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("change_low_ordering_to_fitness_ordering", "Change LOW ordering to Fitness ordering", ["add_fitness_of_artifact_variable"])]
        public class ChangeLOWOrderingToFitnessOrdering(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                Scope.Modify(
                    text,
                    @"public\s+class\s+Artifact\s*{.*?(?=(?:public|private|protected|internal)\s+static\s+function\s+orderStatCountDesc\s*\(.*?\)\s*:\s*\w+)()",
                    (info) =>
                    {
                        var scopeText = Regex.Replace(info.ScopeText,
                            @"var\s+_loc(\d*)_:Number\s+=\s+param(\d*)\.level;",
                            $"var _loc$1_:Number = param$2.{{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessOfArtifact ]}};");
                        scopeText = Regex.Replace(scopeText, @"if\(_loc(\d*)_\s+>\s+_loc(\d*)_\)", @"if(_loc$1_ >_ _loc$2_)");
                        scopeText = Regex.Replace(scopeText, @"if\(_loc(\d*)_\s+<\s+_loc(\d*)_\)", @"if(_loc$1_ > _loc$2_)");
                        scopeText = Regex.Replace(scopeText, @"if\(_loc(\d*)_\s+>_\s+_loc(\d*)_\)", @"if(_loc$1_ < _loc$2_)");
                        return scopeText;
                    },
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
    }
}