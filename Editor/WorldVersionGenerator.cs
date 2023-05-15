using System;
using System.Linq;
using TLP.UdonUtils.Runtime.Common;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.Udon;

namespace TLP.UdonUtils.Editor
{
    public class MyAssetModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        public static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (string path in paths)
            {
                Debug.Log($"OnWillSaveAsset '{path}'");
            }


            if (WorldVersionGenerator.BuildRequested.UpdateBuild() > 0)
            {
                Debug.Log($"Succeeded in updating the {nameof(WorldVersionCheck)}");
                return paths;
            }

            // EditorUtility.DisplayDialog(
            //     $"{nameof(WorldVersionCheck)} missing",
            //     $"The scene does not contain a {nameof(WorldVersionCheck)} U# script.\nPlease add it to the scene and try again.",
            //     "OK"
            // );

            return paths;
        }
    }

    public class WorldVersionGenerator : MonoBehaviour
    {
        public class BuildRequested : IVRCSDKBuildRequestedCallback
        {
            public int callbackOrder => 0;

            public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
            {
                return true;
                if (requestedBuildType == VRCSDKRequestedBuildType.Avatar)
                {
                    return true;
                }

                if (UpdateBuild() > 0)
                {
                    Debug.Log($"Succeeded in updating the {nameof(WorldVersionCheck)}");
                    return true;
                }

                EditorUtility.DisplayDialog(
                    $"{nameof(WorldVersionCheck)} missing",
                    $"The scene does not contain a {nameof(WorldVersionCheck)} U# script.\nPlease add it to the scene and try again.",
                    "OK"
                );
                return false;
            }

            public static int UpdateBuild()
            {
                long timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                int anySucceeded = 0;
                try
                {
                    foreach (var rootGameObject in SceneManager.GetActiveScene().GetRootGameObjects())
                    {
                        var sceneVersionChecks = rootGameObject.GetComponentsInChildren<WorldVersionCheck>();
                        foreach (var sceneVersionCheck in sceneVersionChecks)
                        {
                            if (Mathf.Abs(sceneVersionCheck.Timestamp - timeStamp) < 10)
                            {
                                continue;
                            }

                            Undo.RecordObject(sceneVersionCheck, $"Update {nameof(WorldVersionCheck)} on build");
                            if (PrefabUtility.IsPartOfPrefabInstance(sceneVersionCheck))
                            {
                                PrefabUtility.RecordPrefabInstancePropertyModifications(sceneVersionCheck);
                            }

                            //UdonSharpEditorUtility.CopyUdonToProxy(
                            //    sceneVersionCheck,
                            //    ProxySerializationPolicy.All
                            //);

                            sceneVersionCheck.Timestamp = timeStamp;
                            sceneVersionCheck.build++;
                            sceneVersionCheck.SyncedBuild = sceneVersionCheck.build;

                            Debug.Log(
                                $"Updating to build {sceneVersionCheck.build} with timestamp {sceneVersionCheck.Timestamp}"
                            );

                            //UdonSharpEditorUtility.CopyProxyToUdon(
                            //    sceneVersionCheck,
                            //    ProxySerializationPolicy.All
                            //);
                            anySucceeded++;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return 0;
                }


                return anySucceeded;
            }
        }
    }
}