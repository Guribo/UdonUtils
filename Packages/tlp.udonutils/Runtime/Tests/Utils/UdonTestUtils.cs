using System;
using System.Collections.Generic;
using UdonSharp;
using UnityEngine;
using UnityEngine.Assertions;
using VRC.SDKBase;
using Object = UnityEngine.Object;

namespace TLP.UdonUtils.Tests.Utils
{
    public static class UdonTestUtils
    {
        public class UdonTestEnvironment
        {
            public static void ResetApiBindings()
            {
                Networking._LocalPlayer = null;
                VRCPlayerApi.sPlayers = null;
                VRCPlayerApi._GetPlayerId = null;
                VRCPlayerApi._GetPosition = null;
                VRCPlayerApi._GetRotation = null;
                VRCPlayerApi._GetTrackingData = null;
                VRCPlayerApi._GetPlayerById = null;
                VRCPlayerApi._SetVoiceGain = null;
                VRCPlayerApi._SetVoiceLowpass = null;
                VRCPlayerApi._SetVoiceDistanceFar = null;
                VRCPlayerApi._SetVoiceDistanceNear = null;
                VRCPlayerApi._SetVoiceVolumetricRadius = null;
                VRCPlayerApi._SetAvatarAudioGain = null;
                VRCPlayerApi._SetAvatarAudioCustomCurve = null;
                VRCPlayerApi._SetAvatarAudioFarRadius = null;
                VRCPlayerApi._SetAvatarAudioNearRadius = null;
                VRCPlayerApi._SetAvatarAudioForceSpatial = null;
                VRCPlayerApi._SetAvatarAudioVolumetricRadius = null;
                VRCPlayerApi._CombatGetCurrentHitpoints = null;
                VRCPlayerApi._CombatSetCurrentHitpoints = null;
                VRCPlayerApi._IsUserInVR = null;
                Networking._IsOwner = null;
                Networking._GetOwner = null;
                VRCPlayerApi._IsOwner = null;
                Networking._SetOwner = null;
                Networking._IsMaster = null;
                VRCPlayerApi._isMasterDelegate = null;

                Networking._GetServerTimeInSeconds = null;
                VRCPlayerApi._GetPlayerTag = null;
                VRCPlayerApi._SetPlayerTag = null;
                VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
            }

            public class PlayerData
            {
                public static int Ids;
                public readonly VRCPlayerApi VrcPlayerApi;
                public readonly int ID;
                public readonly HashSet<GameObject> owns = new HashSet<GameObject>();
                public float VoiceRangeFar = 25f;
                public float HitPoints = 0;
                public readonly Dictionary<string, string> tags = new Dictionary<string, string>();
                public bool isMaster;
                public bool InVr { get; set; } = false;


                public PlayerData(VRCPlayerApi vrcPlayerApi)
                {
                    string name;
                    VrcPlayerApi = vrcPlayerApi;
                    VrcPlayerApi.gameObject = new GameObject();
                    ID = Ids;
                    VrcPlayerApi.isLocal = ID == 0;
                    Ids++;
                }
            }

            private readonly Dictionary<int, PlayerData> _idToPlayer = new Dictionary<int, PlayerData>();

            private readonly Dictionary<VRCPlayerApi, PlayerData> _vrcPlayerToPlayer =
                new Dictionary<VRCPlayerApi, PlayerData>();

            private readonly Dictionary<GameObject, PlayerData> ownershipGameobjectToPlayer =
                new Dictionary<GameObject, PlayerData>();

            public double initialServerTime;
            private PlayerData _master;

            public UdonTestEnvironment()
            {
                ResetApiBindings();
                InitVrcPlayerApi();
            }

            private void InitVrcPlayerApi()
            {
                PlayerData.Ids = 0;

                VRCPlayerApi.sPlayers = new List<VRCPlayerApi>();
                VRCPlayerApi._GetPlayerId = api => _vrcPlayerToPlayer[api].ID;
                VRCPlayerApi._GetPosition = player => player.gameObject.transform.position;
                VRCPlayerApi._GetRotation = player => player.gameObject.transform.rotation;
                VRCPlayerApi._GetTrackingData = (player, type) =>
                    new VRCPlayerApi.TrackingData(player.GetPosition(), player.GetRotation());
                VRCPlayerApi._GetPlayerById = i => _idToPlayer.ContainsKey(i) ? _idToPlayer[i].VrcPlayerApi : null;

                VRCPlayerApi._SetVoiceGain = (api, f) => { };
                VRCPlayerApi._SetVoiceLowpass = (api, f) => { };
                VRCPlayerApi._SetVoiceDistanceFar = (api, value) => _vrcPlayerToPlayer[api].VoiceRangeFar = value;
                VRCPlayerApi._SetVoiceDistanceNear = (api, f) => { };
                VRCPlayerApi._SetVoiceVolumetricRadius = (api, f) => { };

                VRCPlayerApi._SetAvatarAudioGain = (api, f) => { };
                VRCPlayerApi._SetAvatarAudioCustomCurve = (api, f) => { };
                VRCPlayerApi._SetAvatarAudioFarRadius = (api, f) => { };
                VRCPlayerApi._SetAvatarAudioNearRadius = (api, f) => { };
                VRCPlayerApi._SetAvatarAudioForceSpatial = (api, f) => { };
                VRCPlayerApi._SetAvatarAudioVolumetricRadius = (api, f) => { };

                VRCPlayerApi._CombatGetCurrentHitpoints = (api) => _vrcPlayerToPlayer[api].HitPoints;
                VRCPlayerApi._CombatSetCurrentHitpoints = (api, f) => _vrcPlayerToPlayer[api].HitPoints = f;
                VRCPlayerApi._IsUserInVR = api => _vrcPlayerToPlayer[api].InVr;

                Networking._IsOwner = (api, o) =>
                {
                    if (api == null)
                    {
                        Debug.LogWarning("'null' can not own an object");
                        return false;
                    }

                    var owner = Networking.GetOwner(o);
                    Debug.Assert(owner != null, "Owner can not be null, must be at least master");

                    return api.playerId == owner.playerId;
                };
                Networking._GetOwner = gameObject =>
                {
                    ownershipGameobjectToPlayer.TryGetValue(gameObject, out var player);
                    if (player?.VrcPlayerApi != null)
                    {
                        Debug.Log($"Player {player.VrcPlayerApi.playerId} with ownership found");
                        return player.VrcPlayerApi;
                    }

                    if (_master?.VrcPlayerApi == null)
                    {
                        throw new UdonTestEnvironmentException("A master must exist, have you created a player?");
                    }

                    Debug.Log($"Fallback owner is master with id {_master.VrcPlayerApi.playerId}");
                    return _master.VrcPlayerApi;
                };
                VRCPlayerApi._IsOwner = Networking._IsOwner;
                Networking._SetOwner = SetOwner;
                Networking._IsMaster = () => Networking.LocalPlayer.isMaster;
                VRCPlayerApi._isMasterDelegate = api => _vrcPlayerToPlayer[api].isMaster;

                Networking._GetServerTimeInSeconds = () => initialServerTime + Math.Floor(Time.timeSinceLevelLoad);

                VRCPlayerApi._GetPlayerTag = GetPlayerTag;
                VRCPlayerApi._SetPlayerTag = SetPlayerTag;
            }

            private void SetPlayerTag(VRCPlayerApi arg1, string arg2, string arg3)
            {
                PlayerData player = null;
                Debug.Assert(_vrcPlayerToPlayer.TryGetValue(arg1, out player));
                if (player != null)
                {
                    player.tags[arg2] = arg3;
                }
            }

            private string GetPlayerTag(VRCPlayerApi arg1, string arg2)
            {
                PlayerData player = null;
                Debug.Assert(_vrcPlayerToPlayer.TryGetValue(arg1, out player));
                if (player.tags.TryGetValue(arg2, out string tag))
                {
                    return tag;
                }

                return "";
            }

            private void SetOwner(VRCPlayerApi api, GameObject o)
            {
                if (!Utilities.IsValid(o))
                {
                    return;
                }

                if (ownershipGameobjectToPlayer.TryGetValue(o, out var oldOwnerPlayerData))
                {
                    ownershipGameobjectToPlayer.Remove(o);
                    oldOwnerPlayerData.owns.Remove(o);
                }

                if (!Utilities.IsValid(api))
                {
                    return;
                }

                var newOwnerPlayerData = _vrcPlayerToPlayer[api];
                Debug.Assert(newOwnerPlayerData != null);

                newOwnerPlayerData.owns.Add(o);
                ownershipGameobjectToPlayer[o] = newOwnerPlayerData;

                Debug.Assert(api.IsOwner(o));
            }

            public void Deconstruct()
            {
                foreach (var vrcPlayerApi in _idToPlayer.Values)
                {
                    Object.DestroyImmediate(vrcPlayerApi.VrcPlayerApi.gameObject);
                }

                ResetApiBindings();
            }

            public VRCPlayerApi CreatePlayer(bool master = false)
            {
                var newPlayer = new VRCPlayerApi();
                newPlayer.AddToList();
                var player = new PlayerData(newPlayer);

                if (newPlayer.isLocal)
                {
                    Networking._LocalPlayer = () => newPlayer;
                    player.isMaster = true;
                    _master = player;
                }

                if (master)
                {
                    if (_master != null)
                    {
                        _master.isMaster = false;
                    }

                    player.isMaster = true;
                    _master = player;
                }

                _vrcPlayerToPlayer[newPlayer] = player;
                _idToPlayer[newPlayer.playerId] = player;
                newPlayer.AddToList();

                newPlayer.displayName = $"TestEnvironmentPlayer_{newPlayer.playerId}";

                return newPlayer;
            }

            public void PlayerJoined(VRCPlayerApi newPlayer)
            {
                foreach (var udonSharpBehaviour in Object.FindObjectsOfType<UdonSharpBehaviour>())
                {
                    udonSharpBehaviour.OnPlayerJoined(newPlayer);
                }
            }

            public PlayerData GetPlayerData(VRCPlayerApi playerApi)
            {
                return _vrcPlayerToPlayer[playerApi];
            }

            public void RemovePlayer(VRCPlayerApi player)
            {
                player.RemoveFromList();
                Assert.IsFalse(player.IsValid());
                Assert.IsFalse(Utilities.IsValid(player));
                foreach (var udonSharpBehaviour in Object.FindObjectsOfType<UdonSharpBehaviour>())
                {
                    udonSharpBehaviour.OnPlayerLeft(player);
                }
            }
        }
    }
}