#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Guribo.UdonUtils.Scripts.Editor
{
    public abstract class PackageExporter : MonoBehaviour
    {
        protected readonly string UnityPackageExtension = "unitypackage";

        // /// <summary> TODO not used (remove?)
        // /// exports the unity package after successfully building
        // /// </summary>
        // /// <param name="target"></param>
        // /// <param name="pathToBuiltProject"></param>
        // /// <exception cref="Exception"></exception>
        // [PostProcessBuild]
        // public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        // {
        //     ExportBetterAudioPackage(pathToBuiltProject);
        // }

        // TODO override and add e.g. [MenuItem("Menu/Item/Path/Export My Package")]
        // TODO call Export<class type>() in the override
        public static void ExportPackage()
        {
            Export<PackageExporter>();
        }

        protected static void Export<T>() where T : PackageExporter
        {
            var go = new GameObject {hideFlags = HideFlags.DontSave};
            try
            {
                var packageExporter = (T) go.AddComponent(typeof(T));
                Debug.Assert(packageExporter);
                packageExporter.ExportPackage(packageExporter.GetExportPath());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                DestroyImmediate(go);
            }
        }

        protected abstract string GetRepositoryPath();
        protected abstract string GetExportPath();
        protected abstract string GetReleaseVersion();
        protected abstract string GetUnityPackage();
        protected abstract string[] GetExportAssets();

        public string GetCommitHash()
        {
            var cmd = new Process();
            var processStartInfo = cmd.StartInfo;
            processStartInfo.FileName = "cmd.exe";
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            cmd.Start();
            var cmdStandardInput = cmd.StandardInput;
            cmdStandardInput.Flush();
            var repositoryPath = GetRepositoryPath();
            cmdStandardInput.WriteLine($"cd {repositoryPath} && git rev-parse --short HEAD");
            cmdStandardInput.Flush();
            cmdStandardInput.Close();
            cmd.WaitForExit();
            var cmdStandardOutput = cmd.StandardOutput;

            // ignore the first 4 lines
            for (var i = 0; i < 4; ++i)
            {
                cmdStandardOutput.ReadLine();
            }

            // get the commit hash
            var commitHash = cmdStandardOutput.ReadLine();

            // ignore the rest
            cmdStandardOutput.ReadToEnd();

            // verify we actually succeeded
            if (cmd.ExitCode != 0
                || commitHash == null
                || string.IsNullOrWhiteSpace(commitHash))
            {
                throw new Exception($"Failed to get git hash for repository '{repositoryPath}'");
            }

            var trimmedHash = commitHash.Trim();
            Debug.Log($"Found git hash '{trimmedHash}'");

            return trimmedHash;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="pathToBuiltProject"></param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentException"></exception>
        private void ExportPackage(string pathToBuiltProject)
        {
            if (pathToBuiltProject == null
                || string.IsNullOrEmpty(pathToBuiltProject)
                || string.IsNullOrWhiteSpace(pathToBuiltProject))
            {
                throw new ArgumentException($"Invalid path {pathToBuiltProject}");
            }

            // absolute path of built project
            var directoryInfo = Directory.GetParent(@pathToBuiltProject);
            if (directoryInfo == null)
            {
                throw new ArgumentException($"Invalid directory {pathToBuiltProject}");
            }

            var buildDirectory = directoryInfo.FullName;

            // absolute path of unity project
            var workingDirectory = Path.GetFullPath(@Environment.CurrentDirectory);

            Debug.Log($"Exporting BetterPlayerAudio Unity Package to '{buildDirectory}' from '{workingDirectory}'");

            var repositoryPath = GetRepositoryPath();
            var releaseVersion = GetReleaseVersion();
            var versionFilePath = $"{repositoryPath}/{releaseVersion}";
            var version = File.ReadLines(versionFilePath).First();
            if (string.IsNullOrWhiteSpace(version))
            {
                throw new Exception($"Failed to get version from '{versionFilePath}'");
            }

            var packageTargetPath = Path.Combine(buildDirectory,
                $"{GetUnityPackage()}_{version.Trim()}_{GetCommitHash()}.{UnityPackageExtension}");

            Debug.Log($"Exporting to '{packageTargetPath}'");

            var exportAssets = GetExportAssets();
            foreach (var exportAsset in exportAssets)
            {
                if (!IsValidFileName(exportAsset))
                {
                    throw new ArgumentException($"Invalid file path: '{exportAsset}'");
                }
            }

            // AssetDatabase.ExportPackage(ExportAssets, packageTargetPath,ExportPackageOptions.Recurse | ExportPackageOptions.Interactive | ExportPackageOptions.IncludeDependencies);
            AssetDatabase.ExportPackage(exportAssets, packageTargetPath,
                ExportPackageOptions.Recurse | ExportPackageOptions.Interactive);
        }

        /// <summary>
        /// <remarks>Source: https://stackoverflow.com/questions/422090/in-c-sharp-check-that-filename-is-possibly-valid-not-that-it-exists</remarks>
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            FileInfo fileInfo = null;
            try
            {
                fileInfo = new FileInfo(fileName);
            }
            catch (Exception)
            {
                // ignored
            }

            return !ReferenceEquals(fileInfo, null);
        }
    }
}
#endif