
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.Udon;

namespace Guribo.UdonUtils.Scripts.Common.Editor
{
    [CustomEditor(typeof(UdonCommon))]
    public class UdonCommonEditor : UdonLibraryEditor
    {
        protected override string GetSymbolName()
        {
            return "udonCommon";
        }
    }
}
#endif
