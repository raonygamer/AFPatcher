using AFPatcher.Patching;
using AFPatcher.Utility;
using Patching.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.artifact.ArtifactStat
    {
        [Patch("add_fitness_of_line_variable", "Add fitness of line variable", [])]
        public class AddFitnessOfLineVariable(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText =
                    $"public var {{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessOfLine ]}}:Number = 0.0;"
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+ArtifactStat\s*()",
                    (info) => info.ScopeText.InsertTextAt(patchText, 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_function_to_calculate_line_fitness", "Add function to calculate line fitness", ["add_fitness_of_line_variable"])]
        public class AddFunctionToCalculateLineFitness(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = (@$"
                    public function {{[ {ctx.PatchDescriptor.ClassName}.Functions.CalculateFitnessOfLine ]}}():Number {{
                        var lineValue:Number = this.value;
						switch(this.type) {{
							case ""healthAdd"":
							case ""healthAdd2"":
							case ""healthAdd3"":
								return 0.002264 * lineValue;
							case ""healthMulti"":
								return 0.5832 * lineValue;
							case ""armorAdd"":
							case ""armorAdd2"":
							case ""armorAdd3"":
								return 0.15375 * lineValue;
							case ""armorMulti"":
								return 0.333 * lineValue;
							case ""corrosiveAdd"":
							case ""corrosiveAdd2"":
							case ""corrosiveAdd3"":
								return 0.3388 * lineValue;
							case ""corrosiveMulti"":
								return 0.83325 * lineValue;
							case ""energyAdd"":
							case ""energyAdd2"":
							case ""energyAdd3"":
								return 0.324 * lineValue;
							case ""energyMulti"":
								return 0.83325 * lineValue;
							case ""kineticAdd"":
							case ""kineticAdd2"":
							case ""kineticAdd3"":
								return 0.3132 * lineValue;
							case ""kineticMulti"":
								return 0.79992 * lineValue;
							case ""shieldAdd"":
							case ""shieldAdd2"":
							case ""shieldAdd3"":
								return 0.001925 * lineValue;
							case ""shieldMulti"":
								return 0.54675 * lineValue;
							case ""shieldRegen"":
								return 0.48 * lineValue;
							case ""corrosiveResist"":
							case ""energyResist"":
							case ""kineticResist"":
								return 0.425 * lineValue;
							case ""allResist"":
								return 1.1 * lineValue;
							case ""allAdd"":
							case ""allAdd2"":
							case ""allAdd3"":
								return 0.23904 * lineValue;
							case ""allMulti"":
								return 2.36925 * lineValue;
							case ""dotDamage"":
								return 5 * lineValue;
							case ""dotDuration"":
								return 2.5 * lineValue;
							case ""directDamage"":
								return 0.5 * lineValue;
							case ""speed"":
							case ""speed2"":
							case ""speed3"":
								return 0.88 * lineValue;
							case ""refire"":
							case ""refire2"":
							case ""refire3"":
								return 0.529584 * lineValue;
							case ""convHp"":
							case ""convShield"":
								if(1000 <= lineValue) {{
									return 43;
								}}
								return 0.0425 * lineValue;
								break;
							case ""powerReg"":
							case ""powerReg2"":
							case ""powerReg3"":
								return 0.24 * lineValue;
							case ""powerMax"":
								return 0.3333 * lineValue;
							case ""cooldown"":
							case ""cooldown2"":
							case ""cooldown3"":
								return 0.6666 * lineValue;
							case ""increaseRecyleRate"":
								return 1.1 * lineValue;
							case ""damageReduction"":
								return 1.5 * lineValue;
							case ""damageReductionWithLowHealth"":
							case ""damageReductionWithLowShield"":
								return 1.4 * lineValue;
							case ""healthRegenAdd"":
								return 0.1 * lineValue;
							case ""shieldVamp"":
							case ""healthVamp"":
								return lineValue;
							case ""kineticChanceToPenetrateShield"":
								return 0.1 * lineValue * 0.5;
							case ""energyChanceToShieldOverload"":
							case ""corrosiveChanceToIgnite"":
								return 0.5 * lineValue;
							case ""beamAndMissileDoesBonusDamage"":
								return 0.5 * lineValue;
							case ""recycleCatalyst"":
								return lineValue;
							case ""velocityCore"":
								return 50;
							case ""slowDown"":
								return 80;
							case ""damageReductionUnique"":
								return 50;
							case ""damageReductionWithLowHealthUnique"":
							case ""damageReductionWithLowShieldUnique"":
								return 35;
							case ""overmind"":
								return 30;
							case ""upgrade"":
								return 20;
							case ""lucaniteCore"":
								return 1.1 * lineValue;
							case ""mantisCore"":
								return 0.8 * lineValue;
							case ""thermofangCore"":
								return lineValue;
							case ""reduceKineticResistance"":
							case ""reduceCorrosiveResistance"":
							case ""reduceEnergyResistance"":
								return lineValue;
							case ""crownOfXhersix"":
								return 10 * lineValue;
							case ""veilOfYhgvis"":
								return 50;
							case ""fistOfZharix"":
								return 10 * lineValue;
							case ""bloodlineSurge"":
								return 50;
							case ""dotDamageUnique"":
								return 1.1 * lineValue;
							case ""directDamageUnique"":
								return 0.5 * lineValue;
							case ""reflectDamageUnique"":
								return 40;
							default:
								return 0;
						}}
                    }}
                ")
	                .ExpandTags(ctx.GlobalPatchContext)
	                .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+ArtifactStat\s*()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("initialize_fitness_of_line_in_constructor", "Initialize fitness of line in constructor", ["add_function_to_calculate_line_fitness"])]
        public class InitializeFitnessOfLineInConstructor(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = (@$"
					this.{{[ {ctx.PatchDescriptor.ClassName}.Variables.FitnessOfLine ]}} = {{[ {ctx.PatchDescriptor.ClassName}.Functions.CalculateFitnessOfLine ]}}();
                ")
	                .ExpandTags(ctx.GlobalPatchContext)
	                .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+ArtifactStat\s*{.*?(?=(?:public|private|protected|internal)\s+function\s+ArtifactStat\s*\(.*?\)\s*)()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
    }
}