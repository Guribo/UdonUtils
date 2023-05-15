#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;

namespace TLP.UdonUtils.Editor
{
    public class UdonUtilsPackageExporter : PackageExporter
    {
        [MenuItem("Tools/TLP/UdonUtils/Export/Unity Package")]
        public new static void ExportPackage()
        {
            Export<UdonUtilsPackageExporter>();
        }
        
        protected override string GetRepositoryPath()
        {
            return "./Assets/TLP/UdonUtils";
        }

        protected override string GetExportDirectoryPath()
        {
           return "./Assets/TLP/UdonUtils/Releases/";
        }

        protected override string GetReleaseVersion()
        {
            return "Version.txt";
        }

        protected override string GetUnityPackage()
        {
            return "TLPUdonUtils";
        }

        protected override string[] GetExportAssets()
        {
            return new[]
            {
                "Assets/TLP/UdonUtils/Graphs",
                "Assets/TLP/UdonUtils/Prefabs",
                "Assets/TLP/UdonUtils/README",
                "Assets/TLP/UdonUtils/Scenes",
                "Assets/TLP/UdonUtils/Runtime",
                "Assets/TLP/UdonUtils/LICENSE",
                "Assets/TLP/UdonUtils/README.md",
                "Assets/TLP/UdonUtils/Version.txt"
            };
        }
    }
}
#endif
