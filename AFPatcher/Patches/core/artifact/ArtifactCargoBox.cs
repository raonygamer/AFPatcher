using System.Text.RegularExpressions;
using AFPatcher.Patching;
using AFPatcher.Utility;
using Patching.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.artifact.ArtifactCargoBox
    {
        [Patch("add_fitness_of_artifact_on_artifact_tooltip", "Add fitness of artifact on artifact tooltip", ["update_fitness_of_artifact_in_update_function"])]
        public class AddFitnessOfArtifactOnArtifactTooltip(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                Scope.Modify(
                    text,
                    @"public\s+class\s+ArtifactCargoBox\s+extends\s+Sprite\s*{.*?(?=(?:public|private|protected|internal)\s+function\s+addToolTip\s*\(.*?\)\s*:\s*\w+)()",
                    (info) =>
                    {
                        var tooltipVariableNameMatch = Regex.Match(info.ScopeText, @"var\s+_loc(\d+)_:String\s+=\s+(?:this.)?a\.name\s+\+\s+""<br>"";");
                        if (!tooltipVariableNameMatch.Success)
                            throw new PatchFailedException(@"Failed to match regex for ""tooltip local number""!");
                        var tooltipVariable = $"_loc{tooltipVariableNameMatch.Groups[1].Value}_";
                        var textToInsert = $@"{tooltipVariable} += ""Fitness: "" + this.a.{{[ core.artifact.Artifact.Variables.FitnessOfArtifact ]}}.toFixed(2).replace("","", ""."") + ""<br>""".ExpandTags(ctx.GlobalPatchContext);
                        return info.ScopeText.InsertTextAtGroupIndex(
                            textToInsert,
                            @"_loc(?:\d+)_\s*\+=\s*Localize\.t\(""(?:[^""]*)""\)\.replace\(""\[level\]"",\s*a\.level\)\.replace\(""\[potential\]"",\s*a\.levelPotential\)\s*\+\s*""<br>"";()"
                        );
                    },
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
    }
}