using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Sync;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace TLP.UdonUtils.Runtime.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(UdonInput), ExecutionOrder)]
    public class UdonInput : TlpBaseBehaviour
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = OwnerOnly.ExecutionOrder + 1;

        public static UdonInput Find() {
            var udonInputGameObject = GameObject.Find(nameof(UdonInput));
            if (!Utilities.IsValid(udonInputGameObject)) {
                Debug.LogError($"GameObject called '{nameof(UdonInput)}' does not exist in the scene");
                return null;
            }

            return udonInputGameObject.GetComponent<UdonInput>();
        }

        public bool InputValueJump;
        public UdonInputEventArgs InputArgsJump;

        public override void InputJump(bool value, UdonInputEventArgs args) {
#if TLP_DEBUG
            DebugLog(nameof(InputJump));
#endif
            InputValueJump = value;
            InputArgsJump = args;
        }

        public bool InputValueUse;
        public UdonInputEventArgs InputArgsUse;

        public override void InputUse(bool value, UdonInputEventArgs args) {
#if TLP_DEBUG
            DebugLog(nameof(InputUse));
#endif
            InputValueUse = value;
            InputArgsUse = args;
        }

        public bool InputValueGrab;
        public UdonInputEventArgs InputArgsGrab;

        public override void InputGrab(bool value, UdonInputEventArgs args) {
#if TLP_DEBUG
            DebugLog(nameof(InputGrab));
#endif
            InputValueGrab = value;
            InputArgsGrab = args;
        }

        public bool InputValueDrop;
        public UdonInputEventArgs InputArgsDrop;

        public override void InputDrop(bool value, UdonInputEventArgs args) {
#if TLP_DEBUG
            DebugLog(nameof(InputDrop));
#endif
            InputValueDrop = value;
            InputArgsDrop = args;
        }

        public float InputValueMoveHorizontal;
        public UdonInputEventArgs InputArgsMoveHorizontal;

        public override void InputMoveHorizontal(float value, UdonInputEventArgs args) {
#if TLP_DEBUG
            DebugLog(nameof(InputMoveHorizontal));
#endif
            InputValueMoveHorizontal = value;
            InputArgsMoveHorizontal = args;
        }

        public float InputValueMoveVertical;
        public UdonInputEventArgs InputArgsMoveVertical;

        public override void InputMoveVertical(float value, UdonInputEventArgs args) {
#if TLP_DEBUG
            DebugLog(nameof(InputMoveVertical));
#endif
            InputValueMoveVertical = value;
            InputArgsMoveVertical = args;
        }

        public float InputValueLookHorizontal;
        public UdonInputEventArgs InputArgsLookHorizontal;

        public override void InputLookHorizontal(float value, UdonInputEventArgs args) {
#if TLP_DEBUG
            DebugLog(nameof(InputLookHorizontal));
#endif
            InputValueLookHorizontal = value;
            InputArgsLookHorizontal = args;
        }

        public float InputValueLookVertical;
        public UdonInputEventArgs InputArgsLookVertical;

        public override void InputLookVertical(float value, UdonInputEventArgs args) {
#if TLP_DEBUG
            DebugLog(nameof(InputLookVertical));
#endif
            InputValueLookVertical = value;
            InputArgsLookVertical = args;
        }
    }
}