using AFPatcher.Patching;
using AFPatcher.Utility;
using Patching.Utility;

// ReSharper disable CheckNamespace
namespace AFPatcher.Patches
{
    namespace core.scene.Game
    {
        [Patch("add_zoom_factor_variable", "Add zoom factor variable", [])]
        public class AddZoomFactorVariable(string id, string name, string[] dependencies, int priority) 
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = 
                    $"public var {{[ {ctx.PatchDescriptor.ClassName}.Variables.ZoomFactor ]}}:Number = 1.0;"
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Game\s+extends\s+SceneBase()",
                    (info) => info.ScopeText.InsertTextAt(patchText, 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_current_fitness_variable", "Add current fitness variable", [])]
        public class AddCurrentFitnessVariable(string id, string name, string[] dependencies, int priority) 
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = 
                    $"public var {{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentFitness ]}}:int = 110;"
                        .ExpandTags(ctx.GlobalPatchContext)
                        .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Game\s+extends\s+SceneBase()",
                    (info) => info.ScopeText.InsertTextAt(patchText, 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_current_lines_variable", "Add current lines variable", [])]
        public class AddCurrentLinesVariable(string id, string name, string[] dependencies, int priority) 
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = 
                    $"public var {{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentLines ]}}:int = 0;"
                        .ExpandTags(ctx.GlobalPatchContext)
                        .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Game\s+extends\s+SceneBase()",
                    (info) => info.ScopeText.InsertTextAt(patchText, 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_current_strength_variable", "Add current strength variable", [])]
        public class AddCurrentStrengthVariable(string id, string name, string[] dependencies, int priority) 
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = 
                    $"public var {{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentStrength ]}}:int = 90;"
                        .ExpandTags(ctx.GlobalPatchContext)
                        .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Game\s+extends\s+SceneBase()",
                    (info) => info.ScopeText.InsertTextAt(patchText, 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_shared_object_variable", "Add shared object variable", [])]
        public class AddSharedObjectVariable(string id, string name, string[] dependencies, int priority) 
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = 
                    $"public var {{[ {ctx.PatchDescriptor.ClassName}.Variables.SharedObject ]}}:flash.net.SharedObject;"
                        .ExpandTags(ctx.GlobalPatchContext)
                        .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Game\s+extends\s+SceneBase()",
                    (info) => info.ScopeText.InsertTextAt(patchText, 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("initialize_shared_object_on_constructor", "Initialize shared object on constructor", ["add_shared_object_variable"])]
        public class InitializeSharedObjectOnConstructor(string id, string name, string[] dependencies, int priority) 
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = (@$"
                    this.{{[ {ctx.PatchDescriptor.ClassName}.Variables.SharedObject ]}} = flash.net.SharedObject.getLocal(""customStorage"");
                ")
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Game\s+extends\s+SceneBase\s*{.*?(?=(?:public|private|protected|internal)\s+function\s+Game\s*\(.*?\)\s*)()",
                    (info) => info.ScopeText.InsertTextAt(patchText, 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_function_to_load_shared_object", "Add function to load shared object", [
            "initialize_shared_object_on_constructor",
            "add_current_fitness_variable",
            "add_current_lines_variable",
            "add_current_strength_variable"
        ])]
        public class AddFunctionToLoadSharedObject(string id, string name, string[] dependencies, int priority) 
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = ($@"
                    private function {{[ {ctx.PatchDescriptor.ClassName}.Functions.LoadSharedObject ]}}(): void {{
						this.{{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentFitness ]}} = {{[ {ctx.PatchDescriptor.ClassName}.Variables.SharedObject ]}}.data.{{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentFitness ]}} == null ? 110 : {{[ {ctx.PatchDescriptor.ClassName}.Variables.SharedObject ]}}.data.{{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentFitness ]}};
						this.{{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentLines ]}} = {{[ {ctx.PatchDescriptor.ClassName}.Variables.SharedObject ]}}.data.{{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentLines ]}} == null ? 0 : {{[ {ctx.PatchDescriptor.ClassName}.Variables.SharedObject ]}}.data.{{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentLines ]}};
						this.{{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentStrength ]}} = {{[ {ctx.PatchDescriptor.ClassName}.Variables.SharedObject ]}}.data.{{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentStrength ]}} == null ? 90 : {{[ {ctx.PatchDescriptor.ClassName}.Variables.SharedObject ]}}.data.{{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentStrength ]}};
					}}
                ")
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Game\s+extends\s+SceneBase()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_function_to_save_shared_object", "Add function to save shared object", [
            "initialize_shared_object_on_constructor",
            "add_current_fitness_variable",
            "add_current_lines_variable",
            "add_current_strength_variable"
        ])]
        public class AddFunctionToSaveSharedObject(string id, string name, string[] dependencies, int priority) 
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = ($@"
                    public function {{[ {ctx.PatchDescriptor.ClassName}.Functions.SaveSharedObject ]}}(): void {{
						{{[ {ctx.PatchDescriptor.ClassName}.Variables.SharedObject ]}}.data.{{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentFitness ]}} = {{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentFitness ]}};
						{{[ {ctx.PatchDescriptor.ClassName}.Variables.SharedObject ]}}.data.{{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentLines ]}} = {{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentLines ]}};
						{{[ {ctx.PatchDescriptor.ClassName}.Variables.SharedObject ]}}.data.{{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentStrength ]}} = {{[ {ctx.PatchDescriptor.ClassName}.Variables.CurrentStrength ]}};
						{{[ {ctx.PatchDescriptor.ClassName}.Variables.SharedObject ]}}.flush();
					}}
                ")
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Game\s+extends\s+SceneBase()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_server_client_time_variable", "Add server client time variable", [])]
        public class AddServerClientTimeVariable(string id, string name, string[] dependencies, int priority) 
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = 
                    $"public var {{[ {ctx.PatchDescriptor.ClassName}.Variables.ServerClientTime ]}}:Number = 0.0;"
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Game\s+extends\s+SceneBase()",
                    (info) => info.ScopeText.InsertTextAt(patchText, 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("update_server_client_time_variable_in_tickUpdate", "Update server client time variable in tickUpdate", ["add_server_client_time_variable"])]
        public class UpdateServerClientTimeVariableInTickUpdate(string id, string name, string[] dependencies, int priority) 
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = 
                    $"{{[ {ctx.PatchDescriptor.ClassName}.Variables.ServerClientTime ]}} = time;"
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Game\s+extends\s+SceneBase{.*?(?=(?:public|private|protected|internal)\s+function\s+tickUpdate\s*\(.*?\)\s*:\s*\w+)()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_enter_frame_function", "Add enter frame function", ["add_server_client_time_variable", "add_object_has_value_function"])]
        public class AddEnterFrameFunction(string id, string name, string[] dependencies, int priority) 
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = @$"
                    public function {{[ {ctx.PatchDescriptor.ClassName}.Functions.EnterFrame ]}}(e:starling.events.EnterFrameEvent): void {{
                        {{[ {ctx.PatchDescriptor.ClassName}.Variables.ServerClientTime ]}} += e.passedTime;
                        var colorRainbow:Number = {{[ {ctx.PatchDescriptor.ClassName}.Variables.ServerClientTime ]}} / 1000 % (Math.PI * 2);
                        if (playerManager != null) {{
                            var players:Vector.<core.player.Player> = playerManager.players;
                            for each (var player:core.player.Player in players) {{
                                if (!generics.Util.{{[ generics.Util.Functions.HasValue ]}}({{[ {ctx.PatchDescriptor.ClassName}.Variables.ClientDevelopers ]}}, player.id) ||
                                    player.ship == null ||
                                    player.ship.engine.thrustEmitters == null ||
                                    player.ship.engine.idleThrustEmitters == null)
                                    continue;
                                for each (var em:core.particle.Emitter in player.ship.engine.idleThrustEmitters) {{
                                    em.startColor = 0xff0000;
                                    em.finishColor = 0xff0000;
                                    em.changeHue(colorRainbow);
                                }}

                                for each (var em:core.particle.Emitter in player.ship.engine.thrustEmitters) {{
                                    em.startColor = 0xff0000;
                                    em.finishColor = 0xff0000;
                                    em.changeHue(colorRainbow);
                                }}
                            }}
                        }}
                    }}"
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Game\s+extends\s+SceneBase()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_enter_frame_function_as_event_listener", "Add event frame function as event listener", ["add_enter_frame_function"])]
        public class AddEventFrameFunctionAsEventListener(string id, string name, string[] dependencies, int priority) 
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = 
                    @$"addEventListener(""enterFrame"", {{[ {ctx.PatchDescriptor.ClassName}.Functions.EnterFrame ]}});"
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Game\s+extends\s+SceneBase{.*?(?=(?:public|private|protected|internal)\s+function\s+Game\s*\(.*?\)\s*)()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_client_developers_object_variable", "Add client developers object variable", [])]
        public class AddClientDevelopersObjectVariable(string id, string name, string[] dependencies, int priority) 
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                if (!ctx.GlobalPatchContext.TryGetTag<Dictionary<string, string>>("Developers", out var developers))
                    throw new PatchFailedException("Couldn't find Developers tag in GlobalPatchContext.");
                
                var patchText = (@$"
                    public static const {{[ {ctx.PatchDescriptor.ClassName}.Variables.ClientDevelopers ]}}:Object = {{
                        {string.Join(", ", developers!.Select(kvp => $@"""{kvp.Key}"": ""{kvp.Value}"""))}
                    }};"
                ).ExpandTags(ctx.GlobalPatchContext).Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Game\s+extends\s+SceneBase()",
                    (info) => info.ScopeText.InsertTextAt(patchText, 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }

        [Patch("add_portable_recycle_function", "Add portable recycle function", [])]
        public class AddPortableRecycleFunction(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = (@$"
                    public function {{[ {ctx.PatchDescriptor.ClassName}.Functions.OpenPortableRecycle ]}}(): void {{
                        var root:Body = bodyManager.getRoot();
                        root.name = ""Portable recycle"";
                        fadeIntoState(new LandedRecycle(this, root));
                    }}")
                .ExpandTags(ctx.GlobalPatchContext).Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Game\s+extends\s+SceneBase()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
        
        [Patch("add_load_shared_object_function_to_init", "Add load shared object function to init", ["add_function_to_load_shared_object"])]
        public class AddLoadSharedObjectFunctionToInit(string id, string name, string[] dependencies, int priority)
            : PatchBase(id, name, dependencies, priority)
        {
            public override PatchResult Apply(PatchContext ctx)
            {
                var text = ctx.Text;
                var patchText = $"{{[ {ctx.PatchDescriptor.ClassName}.Functions.LoadSharedObject ]}}();"
                    .ExpandTags(ctx.GlobalPatchContext)
                    .Flatten();
                Scope.Modify(
                    text,
                    @"public\s+class\s+Game\s+extends\s+SceneBase{.*?(?=override\s+(?:public|private|protected|internal)\s+function\s+init\s*\(.*?\)\s*:\s*\w+)()",
                    (info) => info.ScopeText.InsertTextAt(patchText, info.Length - 1),
                    (oldInfo, newInfo) => text = text.ReplaceFirst(oldInfo.ScopeText, newInfo.ScopeText)
                );
                return new PatchResult(text);
            }
        }
    }
}