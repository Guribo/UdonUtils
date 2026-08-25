using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Testing;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace TLP.UdonUtils.Runtime.Tests
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(ExecutionOrder)]
    [TlpDefaultExecutionOrder(typeof(TestOwnershipTransfer), ExecutionOrder)]
    public class TestOwnershipTransfer : TestCase
    {
        public override int ExecutionOrderReadOnly => ExecutionOrder;

        [PublicAPI]
        public new const int ExecutionOrder = TestSanity.ExecutionOrder + 1;

        [UdonSynced]
        public int TestVariable;

        private bool _receivedConfirmation;
        private int _workingTestVariable;

        private bool _ownershipTransferred;

        protected override void _InitializeTest() {
            _Info("Initializing ownership transfer test");

            // Check if we have at least 2 players
            if (VRCPlayerApi.GetPlayerCount() < 2) {
                _Error("Ownership transfer test requires at least 2 players");
                TestController._TestInitialized(false);
                return;
            }

            // If local player is owner, transfer ownership to another player
            if (Networking.IsOwner(gameObject)) {
                _workingTestVariable = 0;
                RequestSerialization();
                
                var players = VRCPlayerApi.GetPlayers();
                foreach (var player in players) {
                    if (player.isLocal) {
                        continue;
                    }

                    Networking.SetOwner(player, gameObject);
                    _Info($"Transferred initial ownership to player {player.playerId}");
                    break;
                }
            }

            _ownershipTransferred = false;
            _receivedConfirmation = false;
    
            TestController._TestInitialized(true);
        }

        public override void OnPreSerialization() {
            base.OnPreSerialization();
            TestVariable = _workingTestVariable;
        }

        public override void OnDeserialization(DeserializationResult deserializationResult) {
            base.OnDeserialization(deserializationResult);

            _workingTestVariable = TestVariable;
            if (Status != TestCaseStatus.Running
                && TestVariable == 42 
                && !Networking.IsOwner(gameObject)) {
                SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_RPC_Confirmation));
                _Info("Received TestVariable = 42, sending confirmation");
            }
        }

        [NetworkCallable(1)]
        public void _RPC_Confirmation() {
                _Info("Received confirmation from other player");
                _receivedConfirmation = true;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            base.OnOwnershipTransferred(player);
            if (player != Networking.LocalPlayer) {
                return;
            }

            _ownershipTransferred = true;
            _Info("OnOwnershipTransferred called for local player");
        }

        public void _Delay_RunTest() {
            if (Status != TestCaseStatus.Running) {
                _Warn("Delay_RunTest called but test is not running, ignoring");
                return;
            }
            
            _Info("Running ownership transfer test");

            // Check if not owner
            if (Networking.IsOwner(gameObject)) {
                _Error("Test requires non-owner to take ownership, but local player is already owner");
                TestController._TestCompleted(false);
                return;
            }

            if (_ownershipTransferred) {
                _Error("Ownership unexpectedly transferred to local player before test started");
                TestController._TestCompleted(false);
                return;
            }

            // Take ownership
            Networking.SetOwner(Networking.LocalPlayer, gameObject);

            // Verify ownership transferred locally immediately
            if (!Networking.IsOwner(gameObject)) {
                _Error("Ownership was not transferred locally immediately after SetOwner");
                TestController._TestCompleted(false);
                return;
            }

            // Wait for OnOwnershipTransferred to be called (should be immediate)
            // In practice, it is called synchronously
            if (!_ownershipTransferred) {
                _Error("OnOwnershipTransferred was not called after SetOwner");
                TestController._TestCompleted(false);
                return;
            }

            // Set synced variable
            _workingTestVariable = 42;

            // Request serialization
            RequestSerialization();

            // Wait a bit for network propagation
            SendCustomEventDelayedSeconds(nameof(_CheckReception), 2f);
        }

        public void _CheckReception() {
            if (Status != TestCaseStatus.Running) {
                _Warn("CheckReception called but test is not running, ignoring");
                return;
            }
            if (_receivedConfirmation) {
                _Info("Other player confirmed reception of TestVariable = 42");
                TestController._TestCompleted(true);
            } else {
                _Error("Other player did not confirm reception of TestVariable = 42");
                TestController._TestCompleted(false);
            }
        }

        protected override void _RunTest() {
            SendCustomEventDelayedSeconds(nameof(_Delay_RunTest), 3f);
        }

        protected override void _CleanUpTest() {
            _Info("Cleaning up ownership transfer test");

            // Optionally take ownership back to local player
            if (!Networking.IsOwner(gameObject)) {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            _workingTestVariable = 0;
            _receivedConfirmation = false;
            RequestSerialization();
            
            TestController._TestCleanedUp(true);
        }
    }
}