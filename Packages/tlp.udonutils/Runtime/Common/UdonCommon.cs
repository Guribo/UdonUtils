using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
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
            if (!Utilities.IsValid(component)) return "";
            return component.transform.GetPathInScene() + "/" + component.GetType().Name;
        }

        /// <param name="transform"></param>
        /// <returns>The path from the scene root to the script provided,
        /// returns an empty string if the provided component is invalid</returns>
        public static string GetScriptPathInScene(this UdonSharpBehaviour component) {
            if (!Utilities.IsValid(component)) return "";

            return component.transform.GetPathInScene() + "/" +
                   UdonTypeNameShort(component.GetUdonTypeName());
        }
    }
}