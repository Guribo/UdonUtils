using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

#if UNITY_EDITOR
using TLP.UdonUtils.Runtime.Audio;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonUtils.Editor.Audio
{
    [CustomEditor(typeof(PlayOnEnable))]
    public class PlayOnEnableEditor : UnityEditor.Editor
    {
        private const string Description = "Plays a OneShot AudioSource on OnEnable. " +
                                           "Useful for playing a sound on startup.";

        public override void OnInspectorGUI() {
            EditorGUILayout.HelpBox(Description, MessageType.Info);

            DrawDefaultInspector();
        }
    }
}
#endif

namespace TLP.UdonUtils.Runtime.Audio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(PlayOnEnable), ExecutionOrder)]
    public class PlayOnEnable : TlpBaseBehaviour
    {
        #region ExecutionOrder
        [PublicAPI]
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.AudioStart + 1;
        #endregion

        [SerializeField]
        private AudioSource AudioSource;

        private void OnEnable() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(OnEnable));
#endif
            #endregion

            if (!AudioSource) {
                Error($"{nameof(AudioSource)} is not set");
                return;
            }

            AudioSource.time = 0;
            AudioSource.PlayOneShot(AudioSource.clip);
        }
    }
}