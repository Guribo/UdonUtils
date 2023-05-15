#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Threading;
using System.Timers;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TLP.UdonUtils.Editor
{
    public abstract class PackageExporter : UnityEditor.Editor
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
            var go = new GameObject { hideFlags = HideFlags.DontSave };
            var packageExporter = (T)CreateEditor(go, typeof(T));
            try
            {
                Debug.Assert(packageExporter);
                packageExporter.ExportPackage(packageExporter.GetExportDirectoryPath());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                DestroyImmediate(packageExporter);
                DestroyImmediate(go);
            }
        }

        protected abstract string GetRepositoryPath();
        protected abstract string GetExportDirectoryPath();
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
            string repositoryPath = GetRepositoryPath();
            cmdStandardInput.WriteLine($"cd {repositoryPath} && git rev-parse --short HEAD");
            cmdStandardInput.Flush();
            cmdStandardInput.Close();
            cmd.WaitForExit();
            var cmdStandardOutput = cmd.StandardOutput;

            // ignore the first 4 lines
            for (int i = 0; i < 4; ++i)
            {
                cmdStandardOutput.ReadLine();
            }

            // get the commit hash
            string commitHash = cmdStandardOutput.ReadLine();

            // ignore the rest
            cmdStandardOutput.ReadToEnd();

            // verify we actually succeeded
            if (cmd.ExitCode != 0
                || commitHash == null
                || string.IsNullOrWhiteSpace(commitHash))
            {
                throw new Exception($"Failed to get git hash for repository '{repositoryPath}'");
            }

            string trimmedHash = commitHash.Trim();
            Debug.Log($"Found git hash '{trimmedHash}'");

            return trimmedHash;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="exportDirectory"></param>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentException"></exception>
        private void ExportPackage(string exportDirectory)
        {
            if (!CheckExportDirectory(exportDirectory))
            {
                return;
            }

            string buildDirectory = GetBuildDirectory(exportDirectory);
            string repositoryPath = GetRepositoryPath();
            string releaseVersion = GetReleaseVersion();
            string version = GetPackageVersion(repositoryPath, releaseVersion);
            string commitHash = GetCommitHash();
            string unityPackage = GetUnityPackage();

            string packageTargetPathNoExtension = Path.Combine(
                buildDirectory,
                $"{unityPackage}_{version.Trim()}_{commitHash}"
            );

            string[] exportAssets = GetExportAssets();
            CheckExportAssetPaths(exportAssets);

            ExportAndAppendHash(exportAssets, packageTargetPathNoExtension);
        }

        private static string GetPackageVersion(string repositoryPath, string releaseVersion)
        {
            string versionFilePath = $"{repositoryPath}/{releaseVersion}";
            string version = File.ReadLines(versionFilePath).First();
            if (string.IsNullOrWhiteSpace(version))
            {
                throw new Exception($"Failed to get version from '{versionFilePath}'");
            }

            return version;
        }

        private static string GetBuildDirectory(string exportDirectory)
        {
            // absolute path of built project
            var directoryInfo = Directory.GetParent(exportDirectory);
            if (directoryInfo == null)
            {
                throw new ArgumentException($"Invalid directory {exportDirectory}");
            }

            string buildDirectory = directoryInfo.FullName;

            // absolute path of unity project
            string workingDirectory = Path.GetFullPath(@Environment.CurrentDirectory);

            Debug.Log($"Exporting Unity Package to '{buildDirectory}' from '{workingDirectory}'");
            return buildDirectory;
        }

        private static bool CheckExportDirectory(string exportDirectory)
        {
            if (exportDirectory == null
                || string.IsNullOrEmpty(exportDirectory)
                || string.IsNullOrWhiteSpace(exportDirectory))
            {
                throw new ArgumentException($"Invalid path '{exportDirectory}'");
            }

            if (!Directory.Exists(exportDirectory))
            {
                if (EditorUtility.DisplayDialog(
                        "Export",
                        $"'{exportDirectory}' does not exist.\n Create it now?",
                        "Yes",
                        "Abort"
                    ))
                {
                    var directory = Directory.CreateDirectory(exportDirectory);
                    if (!directory.Exists)
                    {
                        throw new IOException("Failed to create directory '{pathToBuiltProject}'");
                    }
                }
                else
                {
                    Debug.Log("Export aborted");
                    return false;
                }
            }

            return true;
        }

        private void CheckExportAssetPaths(string[] exportAssets)
        {
            foreach (string exportAsset in exportAssets)
            {
                string hint = $"Please update your {nameof(GetExportAssets)} method of your exporter. Reason:";
                if (!IsValidFileName(exportAsset))
                {
                    throw new ArgumentException($"{hint} Invalid path: '{exportAsset}'");
                }

                if (!(File.Exists(exportAsset) || Directory.Exists(exportAsset)))
                {
                    throw new ArgumentException($"{hint} File/Directory does not exist: '{exportAsset}'");
                }
            }
        }

        private void ExportAndAppendHash(string[] exportAssets, string packageTargetPathNoExtension)
        {
            Debug.Log($"Exporting to '{packageTargetPathNoExtension}_<hash>.{UnityPackageExtension}'");

            AssetDatabase.ExportPackage(
                exportAssets,
                packageTargetPathNoExtension,
                ExportPackageOptions.Recurse
            );

            AssetDatabase.Refresh();
            AssetDatabase.ReleaseCachedFileHandles();

            ThreadPool.QueueUserWorkItem(
                state =>
                {
                    Debug.Log("Adding hash");
                    WaitForFileToExist(packageTargetPathNoExtension);

                    string hash = GetFileHashSHA256(packageTargetPathNoExtension);
                    string finalFile = $"{packageTargetPathNoExtension}_{hash.Substring(0, 7)}.{UnityPackageExtension}";
                    File.Move(
                        packageTargetPathNoExtension,
                        finalFile
                    );
                    File.Delete(packageTargetPathNoExtension);

#if UNITY_EDITOR_WIN
                    string path = finalFile.Replace(@"/", @"\");
                    Process.Start("explorer.exe", "/select," + path);
#else
                EditorUtility.RevealInFinder(finalFile);
#endif
                }
            );
        }

        private static void WaitForFileToExist(string packageTargetPathNoExtension, int timeoutMs = 5000)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            // waiting for the file to be written completely
            while (!File.Exists(packageTargetPathNoExtension))
            {
                if (stopwatch.ElapsedMilliseconds > timeoutMs)
                {
                    throw new TimeoutException("Waiting for file to exist timed out. Please try again.");
                }

                Thread.Sleep(100);
            }

            stopwatch.Stop();
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

        public static string GetFileHashSHA256(string filename)
        {
            using (var hasher = SHA256.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    byte[] hash = hasher.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
        }
    }
}
#endif