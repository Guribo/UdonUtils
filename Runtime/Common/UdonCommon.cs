using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace TLP.UdonUtils.Runtime.Common
{
    public static class UdonCommon
    {
        /// <summary>
        /// Finds the component of a given type in the current gameobject hierarchy which is closest to the scene root
        /// </summary>
        /// <param name="type"></param>
        /// <param name="start"></param>
        /// <returns>the found component or null if none was found</returns>
        public static Component FindTopComponent(Type type, Transform start)
        {
            if (!Utilities.IsValid(start))
            {
                return null;
            }

            Component topComponent = null;
            var topTransform = start;

            do
            {
                var behaviour = topTransform.GetComponent(type);
                if (Utilities.IsValid(behaviour))
                {
                    topComponent = behaviour;
                }

                topTransform = topTransform.parent;
            } while (Utilities.IsValid(topTransform));

            return topComponent;
        }

        /**
         * O(n) - scales linearly with number of players present, don't use in Update()!
         */
        public static VRCPlayerApi GetClosestPlayer(ref VRCPlayerApi[] players, Vector3 location)
        {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            if (players == null || players.Length < playerCount)
            {
                players = new VRCPlayerApi[playerCount * 2];
            }

            VRCPlayerApi.GetPlayers(players);
            VRCPlayerApi closestPlayer = null;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < playerCount; i++)
            {
                var playerInRange = players[i];
                if (!Utilities.IsValid(playerInRange))
                {
                    continue;
                }

                var distanceVector = location - playerInRange.GetPosition();
                var projectedDistance = Vector3.ProjectOnPlane(
                    distanceVector,
                    playerInRange.GetRotation() * Vector3.up
                );
                float projectedDistanceMagnitude = projectedDistance.magnitude;
                if (projectedDistanceMagnitude < closestDistance)
                {
                    closestDistance = projectedDistanceMagnitude;
                    closestPlayer = playerInRange;
                }
            }

            return closestPlayer;
        }

        public static VRCPlayerApi GetClosestNonLocalPlayer(ref VRCPlayerApi[] players, Vector3 location)
        {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            if (players == null || players.Length < playerCount)
            {
                players = new VRCPlayerApi[playerCount * 2];
            }

            VRCPlayerApi.GetPlayers(players);
            VRCPlayerApi closestPlayer = null;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < playerCount; i++)
            {
                var playerInRange = players[i];
                if (!Utilities.IsValid(playerInRange))
                {
                    continue;
                }

                if (playerInRange.isLocal)
                {
                    continue;
                }

                var distanceVector = location - playerInRange.GetPosition();
                var projectedDistance = Vector3.ProjectOnPlane(
                    distanceVector,
                    playerInRange.GetRotation() * Vector3.up
                );
                float projectedDistanceMagnitude = projectedDistance.magnitude;
                if (projectedDistanceMagnitude < closestDistance)
                {
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
        )
        {
            int playerCount = VRCPlayerApi.GetPlayerCount();
            if (players == null || players.Length < playerCount)
            {
                players = new VRCPlayerApi[playerCount * 2];
            }

            if (outPlayers == null)
            {
                return 0;
            }

            VRCPlayerApi.GetPlayers(players);

            int playersFound = 0;

            for (int i = 0; i < playerCount; i++)
            {
                var playerInRange = players[i];
                if (!Utilities.IsValid(playerInRange))
                {
                    continue;
                }

                if (Vector3.Distance(position, playerInRange.GetPosition()) < radius)
                {
                    if (outPlayers.Length >= playersFound)
                    {
                        outPlayers[playersFound] = playerInRange;
                        ++playersFound;
                    }
                }
            }

            return playersFound;
        }

        public static string UdonTypeNameShort(string udonTypeName)
        {
            string[] productTypeName = udonTypeName.Split('.');
            return productTypeName.Length > 0 ? productTypeName[productTypeName.Length - 1] : udonTypeName;
        }
    }
}