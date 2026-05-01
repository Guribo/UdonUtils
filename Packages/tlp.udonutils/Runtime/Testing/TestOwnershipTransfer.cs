using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Testing;
using UdonSharp;
using UnityEngine;
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
        public new const int ExecutionOrder = TestCase.ExecutionOrder + 1;

        [UdonSynced]
        public int TestVariable;

        private bool _receivedConfirmation;
        private int _workingTestVariable;

        private bool _ownershipTransferred;

        protected override void InitializeTest() {
            Info("Initializing ownership transfer test");

            // Check if we have at least 2 players
            if (VRCPlayerApi.GetPlayerCount() < 2) {
                Error("Ownership transfer test requires at least 2 players");
                TestController.TestInitialized(false);
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
                    Info($"Transferred initial ownership to player {player.playerId}");
                    break;
                }
            }

            _ownershipTransferred = false;
            _receivedConfirmation = false;
    
            TestController.TestInitialized(true);
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
                SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(RPC_Confirmation));
                Info("Received TestVariable = 42, sending confirmation");
            }
        }

        public void RPC_Confirmation() {
                Info("Received confirmation from other player");
                _receivedConfirmation = true;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player) {
            base.OnOwnershipTransferred(player);
            if (player != Networking.LocalPlayer) {
                return;
            }

            _ownershipTransferred = true;
            Info("OnOwnershipTransferred called for local player");
        }

        public void Delay_RunTest() {
            if (Status != TestCaseStatus.Running) {
                Warn("Delay_RunTest called but test is not running, ignoring");
                return;
            }
            
            Info("Running ownership transfer test");

            // Check if not owner
            if (Networking.IsOwner(gameObject)) {
                Error("Test requires non-owner to take ownership, but local player is already owner");
                TestController.TestCompleted(false);
                return;
            }

            if (_ownershipTransferred) {
                Error("Ownership unexpectedly transferred to local player before test started");
                TestController.TestCompleted(false);
                return;
            }

            // Take ownership
            Networking.SetOwner(Networking.LocalPlayer, gameObject);

            // Verify ownership transferred locally immediately
            if (!Networking.IsOwner(gameObject)) {
                Error("Ownership was not transferred locally immediately after SetOwner");
                TestController.TestCompleted(false);
                return;
            }

            // Wait for OnOwnershipTransferred to be called (should be immediate)
            // In practice, it is called synchronously
            if (!_ownershipTransferred) {
                Error("OnOwnershipTransferred was not called after SetOwner");
                TestController.TestCompleted(false);
                return;
            }

            // Set synced variable
            _workingTestVariable = 42;

            // Request serialization
            RequestSerialization();

            // Wait a bit for network propagation
            SendCustomEventDelayedSeconds(nameof(CheckReception), 2f);
        }

        public void CheckReception() {
            if (Status != TestCaseStatus.Running) {
                Warn("CheckReception called but test is not running, ignoring");
                return;
            }
            if (_receivedConfirmation) {
                Info("Other player confirmed reception of TestVariable = 42");
                TestController.TestCompleted(true);
            } else {
                Error("Other player did not confirm reception of TestVariable = 42");
                TestController.TestCompleted(false);
            }
        }

        protected override void RunTest() {
            SendCustomEventDelayedSeconds(nameof(Delay_RunTest), 3f);
        }

        protected override void CleanUpTest() {
            Info("Cleaning up ownership transfer test");

            // Optionally take ownership back to local player
            if (!Networking.IsOwner(gameObject)) {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            _workingTestVariable = 0;
            _receivedConfirmation = false;
            RequestSerialization();
            
            TestController.TestCleanedUp(true);
        }
    }
}