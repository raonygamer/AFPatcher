﻿using System.Reflection;
using AFPatcher.Patching;

namespace AFPatcher.Patches
{
    public class QoLAF
    {
        public static GlobalPatchContext GetGlobalPatchContext()
        {
            return new GlobalPatchContext(new Dictionary<string, object>
            {
                { 
                    "core.scene.Game.Variables", 
                    new Dictionary<string, string>
                    {
                        { "ZoomFactor", "zoomFactor" },
                        { "ClientDevelopers", "clientDevelopers" }
                    } 
                },
                {
                    "core.scene.Game.Functions",
                    new Dictionary<string, string>
                    {
                        { "OpenPortableRecycle", "openPortableRecycle" }
                    }
                },
                { 
                    "core.states.gameStates.PlayState.Functions", 
                    new Dictionary<string, string>
                    {
                        { "CheckZoomFactor", "checkZoomFactor" }
                    } 
                },
                { 
                    "generics.Util.Functions", 
                    new Dictionary<string, string>
                    {
                        { "HasValue", "hasValue" }
                    } 
                },
                { "EchoFormat", "<font color='{0}'>QoLAF (Server {1} / {2})</font>" },
                { "EchoColor", "#ff6200" },
                { "ServerVersion", 1389 },
                { "ClientVersion", "INDEV" },
                { 
                    "Developers", 
                    new Dictionary<string, string>()
                    {
                        { "ryd3v", "steam76561199032900322" },
                        { "TheRealPancake", "steam76561198188053594" },
                        { "Kaiser/Primiano", "" },
                        { "TheLostOne", "simple1622136353425" },
                        { "mufenz", "" }
                    }
                }
            });
        }
    
        public static PatchDescriptor[] GetPatchDescriptors()
        {
            var assembly = Assembly.GetAssembly(typeof(QoLAF));
            if (assembly is null)
                return [];
            var classNamesFromPatches = assembly
                .GetTypes()
                .Select(t => t.Namespace)
                .Where(t => t is not null && t.StartsWith(typeof(QoLAF).Namespace!))
                .Distinct()
                .Select(n => new PatchDescriptor(n!.Replace(typeof(QoLAF).Namespace! + ".", string.Empty), []))
                .ToArray();
        
            return classNamesFromPatches;
        }
    }
}