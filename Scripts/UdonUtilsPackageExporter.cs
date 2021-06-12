#if UNITY_EDITOR
using Guribo.UdonUtils.Scripts.Editor;
using UnityEditor;

namespace Guribo.UdonUtils.Scripts
{
    public class UdonUtilsPackageExporter : PackageExporter
    {
        [MenuItem("Guribo/UDON/UdonUtils/Export Unity Package")]
        public new static void ExportPackage()
        {
            Export<UdonUtilsPackageExporter>();
        }
        
        protected override string GetRepositoryPath()
        {
            return "./Assets/Guribo/UdonUtils";
        }

        protected override string GetExportPath()
        {
           return "./Assets/Guribo/UdonUtils/Releases/";
        }

        protected override string GetReleaseVersion()
        {
            return "Version.txt";
        }

        protected override string GetUnityPackage()
        {
            return "GuriboUdonUtils";
        }

        protected override string[] GetExportAssets()
        {
            return new[]
            {
                "Assets/Guribo/UdonUtils/Graphs",
                "Assets/Guribo/UdonUtils/Prefabs",
                "Assets/Guribo/UdonUtils/README",
                "Assets/Guribo/UdonUtils/Scenes",
                "Assets/Guribo/UdonUtils/Scripts",
                "Assets/Guribo/UdonUtils/LICENSE",
                "Assets/Guribo/UdonUtils/README.md",
                "Assets/Guribo/UdonUtils/Version.txt"
            };
        }
    }
}
#endif
