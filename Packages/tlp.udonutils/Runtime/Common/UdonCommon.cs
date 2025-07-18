using System;
using System.Text;
using JetBrains.Annotations;
using TLP.UdonUtils.Runtime.Events;
using TLP.UdonUtils.Runtime.Extensions;
using TLP.UdonUtils.Runtime.Logger;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.Persistence;
using VRC.SDKBase;

namespace TLP.UdonUtils.Runtime.Common
{
    /// <summary>
    /// Common Utilities and extension methods for <see cref="UdonSharpBehaviour"/>s.
    /// </summary>
    public static class UdonCommon
    {
        /// <summary>
        /// Finds the component of a given type in the current GameObject hierarchy which is closest to the scene root
        /// </summary>
        /// <param name="type"></param>
        /// <param name="start"></param>
        /// <returns>the found component or null if none was found</returns>
        public static Component FindTopComponent(Type type, Transform start) {
            if (!Utilities.IsValid(start)) {
                return null;
            }

            Component topComponent = null;
            var topTransform = start;

            do {
                var behaviour = topTransform.GetComponent(type);
                if (Utilities.IsValid(behaviour)) {
                    topComponent = behaviour;
                }

                topTransform = topTransform.parent;
            } while (Utilities.IsValid(topTransform));

            return topComponent;
        }

        /**
         * O(n) - scales linearly with number of players present, don't use in Update()!
         */
        public static VRCPlayerApi GetClosestPlayer(ref VRCPlayerApi[] players, Vector3 location) {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            if (players == null || players.Length < playerCount) {
                players = new VRCPlayerApi[playerCount * 2];
            }

            VRCPlayerApi.GetPlayers(players);
            VRCPlayerApi closestPlayer = null;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < playerCount; i++) {
                var playerInRange = players[i];
                if (!Utilities.IsValid(playerInRange)) {
                    continue;
                }

                var distanceVector = location - playerInRange.GetPosition();
                var projectedDistance = Vector3.ProjectOnPlane(
                        distanceVector,
                        playerInRange.GetRotation() * Vector3.up
                );
                float projectedDistanceMagnitude = projectedDistance.magnitude;
                if (projectedDistanceMagnitude < closestDistance) {
                    closestDistance = projectedDistanceMagnitude;
                    closestPlayer = playerInRange;
                }
            }

            return closestPlayer;
        }

        public static VRCPlayerApi GetClosestNonLocalPlayer(ref VRCPlayerApi[] players, Vector3 location) {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            if (players == null || players.Length < playerCount) {
                players = new VRCPlayerApi[playerCount * 2];
            }

            VRCPlayerApi.GetPlayers(players);
            VRCPlayerApi closestPlayer = null;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < playerCount; i++) {
                var playerInRange = players[i];
                if (!Utilities.IsValid(playerInRange)) {
                    continue;
                }

                if (playerInRange.isLocal) {
                    continue;
                }

                var distanceVector = location - playerInRange.GetPosition();
                var projectedDistance = Vector3.ProjectOnPlane(
                        distanceVector,
                        playerInRange.GetRotation() * Vector3.up
                );
                float projectedDistanceMagnitude = projectedDistance.magnitude;
                if (projectedDistanceMagnitude < closestDistance) {
                    closestDistance = projectedDistanceMagnitude;
                    closestPlayer = playerInRange;
                }
            }

            return closestPlayer;
        }

        public static int GetPlayersInRangeNoAlloc(
                ref VRCPlayerApi[] players,
                Vector3 position,
                float radius,
                VRCPlayerApi[] outPlayers
        ) {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            if (players == null || players.Length < playerCount) {
                players = new VRCPlayerApi[playerCount * 2];
            }

            if (outPlayers == null) {
                return 0;
            }

            VRCPlayerApi.GetPlayers(players);

            int playersFound = 0;

            for (int i = 0; i < playerCount; i++) {
                var playerInRange = players[i];
                if (!Utilities.IsValid(playerInRange)) {
                    continue;
                }

                if (Vector3.Distance(position, playerInRange.GetPosition()) < radius) {
                    if (outPlayers.Length >= playersFound) {
                        outPlayers[playersFound] = playerInRange;
                        ++playersFound;
                    }
                }
            }

            return playersFound;
        }

        public static string UdonTypeNameShort(string udonTypeName) {
            if (string.IsNullOrEmpty(udonTypeName)) {
                return "";
            }

            string[] productTypeName = udonTypeName.Split('.');
            return productTypeName.Length > 0 ? productTypeName[productTypeName.Length - 1] : udonTypeName;
        }

        /// <summary>
        /// Expects at most one object of the given type on each GameObject!
        /// </summary>
        /// <param name="start"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] GetBehavioursInChildren<T>(this Transform start) where T : UdonSharpBehaviour {
            var toProcess = new DataList();
            toProcess.Add(start);
            var result = new DataList();

            while (toProcess.Count > 0) {
                var current = (Transform)toProcess[0].Reference;
                toProcess.RemoveAt(0);
                if (!Utilities.IsValid(toProcess)) {
                    continue;
                }

                var behaviour = current.gameObject.GetComponent<T>();
                if (Utilities.IsValid(behaviour)) {
                    result.Add(behaviour);
                }

                for (int i = 0; i < current.childCount; i++) {
                    toProcess.Add(current.GetChild(i));
                }
            }

            var outPut = new T[result.Count];
            for (int i = 0; i < result.Count; i++) {
                outPut[i] = (T)result[i].Reference;
            }

            return outPut;
        }

        /// <param name="transform"></param>
        /// <returns>The path from the scene root to the transform provided,
        /// returns an empty string if the provided transform is invalid</returns>
        public static string GetPathInScene(this Transform transform) {
            if (!Utilities.IsValid(transform)) {
                return "";
            }

            string path = "";
            while (transform != null) {
                path = transform.name + (path.Length > 0 ? "/" + path : "");
                transform = transform.parent;
            }

            return path;
        }

        /// <param name="transform"></param>
        /// <returns>The path from the scene root to the component provided,
        /// returns an empty string if the provided component is invalid</returns>
        public static string GetComponentPathInScene(this Component component) {
            if (!Utilities.IsValid(component)) {
                return "";
            }

            return component.transform.GetPathInScene() + "/" + component.GetType().Name;
        }

        /// <param name="transform"></param>
        /// <returns>The path from the scene root to the script provided,
        /// returns an empty string if the provided component is invalid</returns>
        public static string GetScriptPathInScene(this UdonSharpBehaviour component) {
            if (!Utilities.IsValid(component)) {
                return "";
            }

            return $"{component.transform.GetPathInScene()}/{UdonTypeNameShort(component.GetUdonTypeName())}";
        }

        /// <param name="t"></param>
        /// <param name="elementsPerLine">if 0 then there is no linebreaks '\n', if > 0 then there will be a linebreak after the given number of elements</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>string in format '[a, b,...,\nc, d]'</returns>
        public static string ToReadableString<T>(this T[] t, int elementsPerLine = 0) {
            var sb = new StringBuilder("[");

            int length = t.LengthSafe();
            for (int i = 0; i < length; i++) {
                sb.Append(t[i]);
                if (i >= length - 1) {
                    continue;
                }

                if (elementsPerLine > 0 && i % elementsPerLine == elementsPerLine - 1) {
                    sb.Append(",\n");
                } else {
                    sb.Append(", ");
                }
            }

            sb.Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// <see cref="ToReadableString{T}"/>
        /// </summary>
        /// <param name="t"></param>
        /// <param name="elementsPerLine"></param>
        /// <returns>[keyA: stateA, keyB: stateB,...,\nkeyC: stateC, ...]</returns>
        public static string PlayerDataInfosToReadableString(this PlayerData.Info[] t, int elementsPerLine = 0) {
            var sb = new StringBuilder("[");

            int length = t.LengthSafe();
            for (int i = 0; i < length; i++) {
                var info = t[i];
                sb.Append(info.Key).Append(": ").Append(info.State);
                if (i >= length - 1) {
                    continue;
                }

                if (elementsPerLine > 0 && i % elementsPerLine == elementsPerLine - 1) {
                    sb.Append(",\n");
                } else {
                    sb.Append(", ");
                }
            }

            sb.Append("]");
            return sb.ToString();
        }

        [PublicAPI]
        public static DateTimeOffset SecondsToLocalTime(double lastSeenUnixTimeStampInSeconds) {
            var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds((long)lastSeenUnixTimeStampInSeconds);
            return TimeZoneInfo.ConvertTime(dateTimeOffset, TimeZoneInfo.Local);
        }

        /// <summary>
        /// Get any PlayerData component of the given type that belongs
        /// to the given player.
        /// </summary>
        /// <remarks>
        /// <see cref="UdonSharpBehaviour.OnPlayerRestored"/> must have been called by VRChat for that player
        /// to guarantee the player has all PlayerData components assigned.
        /// </remarks>
        /// <param name="player"></param>
        /// <param name="component">the first it finds or null if not found</param>
        /// <typeparam name="T">Class-type of the component</typeparam>
        /// <returns>true if a component was found</returns>
        public static bool TryGetPlayerDataComponent<T>(this VRCPlayerApi player, out T component) where T : Component {
            var objects = Networking.GetPlayerObjects(player);
            if (objects.LengthSafe() == 0) {
                TlpLogger.StaticError(
                        $"{nameof(TryGetPlayerDataComponent)}: {player.DisplayNameSafe()} is missing PlayerData objects (not yet created?)",
                        null);
                component = null;
                return false;
            }

            foreach (var gameObject in objects) {
                var found = gameObject.GetComponentInChildren<T>(true);
                if (!Utilities.IsValid(found)) {
                    continue;
                }

                component = found;
                return true;
            }

            TlpLogger.StaticError(
                    $"{nameof(TryGetPlayerDataComponent)}: {player.DisplayNameSafe()} is missing PlayerData components!",
                    null);
            // player was not fully initialized
            // because each player should have AT LEAST one of the component
            component = null;
            return false;
        }

        /// <summary>
        /// Get all PlayerData components of the given type that belong
        /// to the given player.
        /// </summary>
        /// <remarks>
        /// <see cref="UdonSharpBehaviour.OnPlayerRestored"/> must have been called by VRChat for that player
        /// to guarantee the player has all PlayerData components assigned.
        /// </remarks>
        /// <param name="player"></param>
        /// <param name="inOutComponents">in-out appends any found component to that list, but will not be modified on error</param>
        /// <typeparam name="T">Class-type of the components</typeparam>
        /// <returns>true if at least component was found, false otherwise</returns>
        public static bool TryGetAllPlayerDataComponents<T>(this VRCPlayerApi player, DataList inOutComponents)
                where T : Component {
            var objects = Networking.GetPlayerObjects(player);
            if (objects.LengthSafe() == 0) {
                TlpLogger.StaticError(
                        $"{nameof(TryGetAllPlayerDataComponents)}<{typeof(T).Name}>: {player.DisplayNameSafe()} is missing PlayerData objects (not yet created?)",
                        null);
                return false;
            }

            int size = inOutComponents.Count;
            foreach (var gameObject in objects) {
                var found = gameObject.GetComponentsInChildren<T>(true);
                if (found.LengthSafe() == 0) {
                    continue;
                }

                foreach (var foundComponent in found) {
                    if (Utilities.IsValid(foundComponent)) {
                        inOutComponents.Add(foundComponent);
                    }
                }
            }

            if (size == inOutComponents.Count) {
                TlpLogger.StaticError(
                        $"{nameof(TryGetAllPlayerDataComponents)}<{typeof(T).Name}>: {player.DisplayNameUniqueSafe()} is missing PlayerData components!",
                        null);
                return false; // a player was not fully initialized
                // because each player should have AT LEAST one of the components
            }

            return true;
        }

        /// <summary>
        /// Get all PlayerData components of the given type of all players.
        /// </summary>
        /// <remarks>
        /// <see cref="UdonSharpBehaviour.OnPlayerRestored"/> must have been called by VRChat for that player
        /// to guarantee each player has all PlayerData components assigned.
        /// </remarks>
        /// <param name="player"></param>
        /// <param name="components">all components found or null if not found</param>
        /// <typeparam name="T">Class-type of the components</typeparam>
        /// <returns>true if at least one component was for each player, false otherwise</returns>
        public static bool TryGetPlayerDataComponentsOfAllPlayers<T>(DataList components)
                where T : Component {
            var players = VRCPlayerApi.GetPlayers(new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()]);
            var newComponents = new DataList();
            foreach (var player in players) {
                if (!player.TryGetAllPlayerDataComponents<T>(newComponents)) {
                    return false;
                }
            }

            components.AddRange(newComponents);
            return true;
        }

        /// <summary>
        /// Retrieves a player-specific component associated with the given player, using caching to optimize performance.
        /// </summary>
        /// <param name="player">The player for whom the component is being retrieved.</param>
        /// <param name="instigator">The instigating script initiating the retrieval process.</param>
        /// <param name="cached">A reference to the cached component, if available, to avoid redundant lookups.</param>
        /// <typeparam name="T">The type of the component to retrieve.</typeparam>
        /// <returns>The player-specific component of type <typeparamref name="T"/> if found, or null if the component is not restored or unavailable.</returns>
        public static T GetPlayerComponent<T>(this VRCPlayerApi player, TlpBaseBehaviour instigator, ref T cached)
                where T : Component {
            if (Utilities.IsValid(cached)) {
                return cached;
            }

            #region TLP_DEBUG
#if TLP_DEBUG
            TlpLogger.StaticDebugLog(
                    $"{nameof(GetPlayerComponent)} for {instigator.GetScriptPathInScene()}: {player.DisplayNameUniqueSafe()}",
                    null,
                    instigator);
#endif
            #endregion

            if (!PlayerDataRestoredEvent.IsPlayerDataRestored(player)) {
                TlpLogger.StaticWarning(
                        $"{nameof(GetPlayerComponent)} for {instigator.GetScriptPathInScene()}: {player.DisplayNameUniqueSafe()} not yet restored",
                        null,
                        instigator);
                return null;
            }

            if (TryGetPlayerDataComponent(player, out cached)) {
                return cached;
            }

            TlpLogger.StaticError(
                    $"{nameof(GetPlayerComponent)} for {instigator.GetScriptPathInScene()}: Component not found for {player.DisplayNameUniqueSafe()}",
                    null,
                    instigator);
            return null;
        }

        public static VRCPlayerApi.TrackingDataType ToHandTrackingDataType(this VRC_Pickup.PickupHand hand) {
            return hand == VRC_Pickup.PickupHand.Left
                    ? VRCPlayerApi.TrackingDataType.LeftHand
                    : VRCPlayerApi.TrackingDataType.RightHand;
        }
    }
}