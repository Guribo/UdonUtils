#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.Udon;

namespace Guribo.UdonUtils.Scripts.Common.Editor
{
    [CustomEditor(typeof(UdonDebug))]
    public class UdonDebugEditor : UdonLibraryEditor
    {
        protected override string GetSymbolName()
        {
            return "udonDebug";
        }
    }
}
#endif