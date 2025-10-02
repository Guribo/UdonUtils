using System;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Extensions;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VRC.SDKBase;

#if UNITY_EDITOR
using TLP.UdonUtils.Runtime.Player;
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace TLP.UdonUtils.Editor.Player
{
    [CustomEditor(typeof(EnableDuringOccupation))]
    public class EnableDuringOccupationEditor : UnityEditor.Editor
    {

        private const string Description = "Enables or disables GameObjects when players enter/exit a trigger zone." +
                                           "Configure which players trigger the activation (local only, remote only, " +
                                           "or all players) and specify the objects to control.";
        public override void OnInspectorGUI() {
            EditorGUILayout.HelpBox(Description,MessageType.Info);

            DrawDefaultInspector();
        }
    }
}
#endif

namespace TLP.UdonUtils.Runtime.Player
{
    [Serializable]
    internal enum OccupationState
    {
        All,
        LocalPlayerOnly,
        RemotePlayersOnly
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(EnableDuringOccupation), ExecutionOrder, upperLimit:TlpExecutionOrder.PlayerMotionStart)]
    public class EnableDuringOccupation : TlpBaseBehaviour
    {
        #region ExecutionOrder
        [PublicAPI]
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TlpExecutionOrder.DefaultStart + 1;
        #endregion

        #region Configuration
        [Tooltip("Whether to activate or deactivate the GameObjects when the players are inside")]
        [SerializeField]
        private bool EnableOnEnter = true;

        [FormerlySerializedAs("Objects")]
        [SerializeField]
        private GameObject[] ObjectsToEnable;
        #endregion

        #region State
        [SerializeField]
        private OccupationState Occupation = OccupationState.All;

        private int _playerCount;
        #endregion

        #region Overrides
        protected override bool SetupAndValidate() {
            if (!base.SetupAndValidate()) {
                return false;
            }

            if (ObjectsToEnable.LengthSafe() <= 0) {
                Error($"{nameof(ObjectsToEnable)} is empty");
                return false;
            }

            foreach (var obj in ObjectsToEnable) {
                obj.SetActive(!EnableOnEnter);
            }

            // ensure that player are correctly detected if they are already inside the trigger
            gameObject.SetActive(false);
            gameObject.SetActive(true);
            return true;
        }
        #endregion

        #region VRC Player Events
        public override void OnPlayerTriggerEnter(VRCPlayerApi player) {
            base.OnPlayerTriggerEnter(player);

            switch (Occupation) {
                case OccupationState.LocalPlayerOnly:
                    if (player.IsLocalSafe()) {
                        Activate();
                    }

                    break;
                case OccupationState.RemotePlayersOnly:
                    if (!player.IsLocalSafe()) {
                        Activate();
                    }

                    break;
                default:
                    Activate();
                    break;
            }
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player) {
            base.OnPlayerTriggerExit(player);

            switch (Occupation) {
                case OccupationState.LocalPlayerOnly:
                    if (player.IsLocalSafe()) {
                        Deactivate();
                    }

                    break;
                case OccupationState.RemotePlayersOnly:
                    if (!player.IsLocalSafe()) {
                        Deactivate();
                    }

                    break;
                default:
                    Deactivate();
                    break;
            }
        }
        #endregion

        #region Internal
        private void Activate() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Activate));
#endif
            #endregion

            _playerCount = Mathf.Min(VRCPlayerApi.GetPlayerCount(), _playerCount + 1);

            if (_playerCount > 1 || ObjectsToEnable.LengthSafe() <= 0) {
                return;
            }

            foreach (var obj in ObjectsToEnable) {
                obj.SetActive(EnableOnEnter);
            }
        }

        private void Deactivate() {
            #region TLP_DEBUG
#if TLP_DEBUG
            DebugLog(nameof(Deactivate));
#endif
            #endregion

            _playerCount = Mathf.Max(0, _playerCount - 1);

            if (_playerCount > 0 || ObjectsToEnable.LengthSafe() <= 0) {
                return;
            }

            foreach (var obj in ObjectsToEnable) {
                obj.SetActive(!EnableOnEnter);
            }
        }
        #endregion
    }
}