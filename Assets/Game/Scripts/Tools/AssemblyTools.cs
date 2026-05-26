using System.Reflection;

namespace Game.Tools
{
	internal static class AssemblyTools
	{
		private static string[] excludePatterns = new[]
		{
			// Unity core assemblies
			"UnityEngine",
			"UnityEditor",
			"Unity.Plastic",
			"Unity.Rider",
			"Unity.TextMeshPro",
			"Unity.Timeline",
			"Unity.VisualScripting",
			"UnityEngine",
			"UnityEngine.CoreModule",
			"UnityEngine.PhysicsModule",
			"UnityEngine.UIModule",
			"UnityEngine.AnimationModule",
			"UnityEngine.AudioModule",
			"UnityEngine.InputModule",
			"UnityEngine.UI",
			"UnityEngine.UIElementsModule",
			"UnityEngine.VFXModule",
			"UnityEngine.VRModule",
			"UnityEngine.XRModule",
            
			// Unity packages
			"Unity.RenderPipelines",
			"Unity.Mathematics",
			"Unity.Burst",
			"Unity.Collections",
			"Unity.Jobs",
			"Unity.Properties",
			"Unity.Entities",
			"Unity.Transforms",
            
			// Standard .NET and system assemblies
			"System",
			"mscorlib",
			"netstandard",
			"Microsoft",
			"Mono.",
			"Accessibility",
            
			// Third-party common frameworks (optional, adjust as needed)
			"Newtonsoft.Json",
			"NUnit",
			"nunit.framework",
		};

		public static bool IsStandardAssembly(Assembly assembly)
		{
			var assemblyName = assembly.GetName().Name;
			// Check if assembly name matches any exclude pattern
			foreach (var pattern in excludePatterns) {
				if (assemblyName.StartsWith(pattern))
					return true;
			}
			return false;
		}
	}
}
