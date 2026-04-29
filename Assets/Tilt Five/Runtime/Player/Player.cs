/*
 * Copyright (C) 2020-2023 Tilt Five, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using TiltFive.Logging;

#if UNITY_2019_1_OR_NEWER && INPUTSYSTEM_AVAILABLE
using UnityEngine.InputSystem;
#endif

namespace TiltFive
{
    /// <summary>
    /// Provides access to player settings and functionality.
    /// </summary>
    public class Player : Singleton<Player>
    {
        #region Private Fields

        private Dictionary<PlayerIndex, PlayerCore> players = new Dictionary<PlayerIndex, PlayerCore>();

        internal static bool scanningForPlayers = false;

        #endregion


        #region Public Functions

        /// <summary>
        /// Determines whether the specified player has an associated pair of glasses connected.
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <returns></returns>
        public static bool IsConnected(PlayerIndex playerIndex)
        {
            return playerIndex != PlayerIndex.None && Instance.players.TryGetValue(playerIndex, out var playerCore);
        }


        /// <summary>
        /// Attempts to get the friendly name assigned to the specified player's glasses.
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <param name="friendlyName"></param>
        /// <returns></returns>
        public static bool TryGetFriendlyName(PlayerIndex playerIndex, out string friendlyName)
        {
            if (playerIndex == PlayerIndex.None || !Instance.players.TryGetValue(playerIndex, out var playerCore))
            {
                friendlyName = "";
                return false;
            }

            friendlyName = playerCore.FriendlyName;
            return true;
        }

        /// <summary>
        /// Obtains the <see cref="PlayerSettings"/> corresponding to the specified player.
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <param name="playerSettings"></param>
        /// <returns>Returns false and sets <paramref name="playerSettings"/> to null if the provided player index is invalid,
        /// or if there are no player settings defined in the scene due to the absence of a <see cref="TiltFiveManager2"/> or <see cref="TiltFiveManager"/>.</returns>
        public static bool TryGetSettings(PlayerIndex playerIndex, out PlayerSettings playerSettings)
        {
            // We need to obtain a TiltFiveManager2 or TiltFiveManager to query for player settings.
            // Since we don't know which (if either) is available, we'll use TiltFiveSingletonHelper.
            if (playerIndex == PlayerIndex.None || !TiltFiveSingletonHelper.TryGetISceneInfo(out var sceneInfo))
            {
                playerSettings = null;
                return false;
            }

            if (sceneInfo is TiltFiveManager2 tiltFiveManager2
                && tiltFiveManager2.TryGetPlayerSettings(playerIndex, out var resultingPlayerSettings))
            {
                playerSettings = resultingPlayerSettings;
                return true;
            }

            // If we are dealing with a TiltFiveManager, the scene is singleplayer-only; reject other player indices.
            if (sceneInfo is TiltFiveManager tiltFiveManager && playerIndex == PlayerIndex.One)
            {
                playerSettings = tiltFiveManager.playerSettings;
                return true;
            }

            playerSettings = null;
            return false;
        }

        #endregion


        #region Internal Functions

        /// <summary>
        /// Updates the specified player.
        /// </summary>
        /// <param name="playerSettings"></param>
        internal static void Update(PlayerSettings playerSettings, SpectatorSettings spectatorSettings)
        {
            if (scanningForPlayers)
            {
                return;
            }

            if (playerSettings != null && Instance.players.TryGetValue(playerSettings.PlayerIndex, out var playerCore))
            {
                var playerIndex = playerSettings.PlayerIndex;

                // Check to make sure the glasses for this player are still connected.
                if (!Glasses.IsConnected(playerCore.GlassesHandle))
                {
                    // This player's glasses are gone, so it's time to destroy this PlayerCore.
                    Log.Info($"Player {playerIndex} was removed due to their glasses (handle: {playerCore.GlassesHandle}) being disconnected.");
                    Instance.players.Remove(playerIndex);
                    return;
                }

                playerCore.Update(playerSettings, spectatorSettings);
            }
        }

        /// <summary>
        /// Resets the internal state of the specified player.
        /// </summary>
        /// <param name="playerSettings"></param>
        internal static void Reset(PlayerSettings playerSettings, SpectatorSettings spectatorSettings)
        {
            var playerIndex = playerSettings.PlayerIndex;

            if (playerSettings != null && Instance.players.TryGetValue(playerIndex, out var playerCore))
            {
                if (Glasses.IsConnected(playerCore.GlassesHandle))
                {
                    playerCore.Reset(playerSettings, spectatorSettings);
                }
                else
                {
                    Validate(playerSettings);
                }
            }
        }

        /// <summary>
        /// Ensures that internal state information is valid.
        /// </summary>
        /// <param name="playerSettings"></param>
        internal static void Validate(PlayerSettings playerSettings)
        {
            if (playerSettings == null)
            {
                return;
            }

            var scaleSettings = playerSettings.scaleSettings;
            scaleSettings.contentScaleRatio = Mathf.Clamp(scaleSettings.contentScaleRatio, ScaleSettings.MIN_CONTENT_SCALE_RATIO, float.MaxValue);

            playerSettings.Validate();
        }


        /// <summary>
        /// Searches for newly connected hardware and creates one or more players if possible.
        /// </summary>
        internal static void ScanForNewPlayers()
        {
            if (scanningForPlayers)
            {
                return;
            }
            scanningForPlayers = true;
            Glasses.Scan();

            Wand.Scan();
            scanningForPlayers = false;
        }

        internal static bool TryGetGlassesHandle(PlayerIndex playerIndex, out GlassesHandle glassesHandle)
        {
            if (playerIndex == PlayerIndex.None)
            {
                glassesHandle = new GlassesHandle();
                return false;
            }
            var playerAvailable = Instance.players.TryGetValue(playerIndex, out var playerCore);
            glassesHandle = playerCore?.GlassesHandle ?? new GlassesHandle();
            return playerAvailable;
        }

        internal static bool TryGetPlayerIndex(GlassesHandle glassesHandle, out PlayerIndex playerIndex)
        {
            foreach (var keyValuePair in Instance.players)
            {
                var currentPlayerIndex = keyValuePair.Key;
                var currentPlayerCore = keyValuePair.Value;
                if (currentPlayerCore.GlassesHandle == glassesHandle)
                {
                    playerIndex = currentPlayerIndex;
                    return true;
                }
            }
            playerIndex = PlayerIndex.None;
            return false;
        }

        internal static bool AllSupportedPlayersConnected()
        {
            if(!TiltFiveSingletonHelper.TryGetISceneInfo(out var sceneInfo))
            {
                return false;
            }
            var supportedPlayers = sceneInfo.GetSupportedPlayerCount();

            for(PlayerIndex playerIndex = PlayerIndex.One; playerIndex <= (PlayerIndex)supportedPlayers; playerIndex++)
            {
                // If any supported player isn't connected, return false
                if(!IsConnected(playerIndex))
                {
                    return false;
                }
            }
            // Otherwise they're all connected, and we can return true.
            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns>Returns true if we successfully added a player.</returns>
        internal static bool TryAddPlayer(GlassesHandle glassesHandle, out PlayerIndex playerIndex)
        {
            var players = Instance.players;
            if (players.Count >= GlassesSettings.MAX_SUPPORTED_GLASSES_COUNT)
            {
                playerIndex = PlayerIndex.None;
                return false;
            }

            if(TryGetPlayerIndex(glassesHandle, out var existingPlayerIndex))
            {
                playerIndex = PlayerIndex.None;
                return false;   // This player already exists.
            }

            foreach (PlayerIndex currentPlayerIndex in Enum.GetValues(typeof(PlayerIndex)))
            {
                if (currentPlayerIndex == PlayerIndex.None)
                {
                    continue;
                }

                // Assign the smallest (numerically) playerIndex that isn't already assigned.
                // For example, if player #1 disappeared, and player #2 is still available,
                // then we assign the specified glasses to a new player #1.
                if (!players.ContainsKey(currentPlayerIndex))
                {
                    Glasses.TryGetFriendlyName(glassesHandle, out var friendlyName);
                    players[currentPlayerIndex] = new PlayerCore(glassesHandle, friendlyName);

                    Log.Info($"Player {currentPlayerIndex} created. Glasses: {glassesHandle} (\"{friendlyName}\")");

                    // Default control should go to the lowest player index.
                    // If this playerIndex is lower than that of any other current players,
                    // reset the default glasses handle.


                    bool lowestPlayerIndex = false;

                    // TODO: Determine whether this is the lowest player index.
                    // Until then, default control stays with player 2 in the above scenario.

                    if (lowestPlayerIndex)
                    {
                        Glasses.SetDefaultGlassesHandle(glassesHandle);
                    }

                    playerIndex = currentPlayerIndex;
                    return true;
                }
            }

            // We shouldn't ever reach this.
            playerIndex = PlayerIndex.None;
            return false;
        }

        internal static void OnDisable()
        {
            Glasses.OnDisable();
            Wand.OnDisable();
        }

        internal static bool TryGetFirstConnectedPlayer(out PlayerIndex playerIndex)
        {
            for(PlayerIndex i = PlayerIndex.One; i <= PlayerIndex.Four; i++)
            {
                if(IsConnected(i))
                {
                    playerIndex = i;
                    return true;
                }
            }

            playerIndex = PlayerIndex.None;
            return false;
        }

#if UNITY_EDITOR

        /// <summary>
        /// Draws any gizmos associated with the specified player.
        /// </summary>
        /// <param name="playerSettings"></param>
        internal static void DrawGizmos(PlayerSettings playerSettings)
        {
            if (playerSettings == null)
            {
                return;
            }

            var gameBoardSettings = playerSettings.gameboardSettings;

            if (gameBoardSettings.currentGameBoard != null)
            {
                gameBoardSettings.currentGameBoard.DrawGizmo(playerSettings.scaleSettings, gameBoardSettings);
            }
        }
#endif

        #endregion


        #region Private Classes

        private class PlayerCore
        {
            public readonly GlassesHandle GlassesHandle;
            public readonly string FriendlyName;

            public PlayerCore(GlassesHandle glassesHandle, string friendlyName)
            {
                GlassesHandle = glassesHandle;
                FriendlyName = friendlyName;
            }

            internal void Update(PlayerSettings playerSettings, SpectatorSettings spectatorSettings)
            {
                if (!Glasses.Validate(playerSettings.glassesSettings, spectatorSettings, GlassesHandle))
                {
                    Glasses.Reset(playerSettings.glassesSettings, spectatorSettings, GlassesHandle);
                }
                GetLatestPoseData(playerSettings, spectatorSettings);
            }

            internal void Reset(PlayerSettings playerSettings, SpectatorSettings spectatorSettings)
            {
                Glasses.Reset(playerSettings.glassesSettings, spectatorSettings, GlassesHandle);
            }


            /// <summary>
            /// Obtains the latest pose for all trackable objects.
            /// </summary>
            private void GetLatestPoseData(PlayerSettings playerSettings, SpectatorSettings spectatorSettings)
            {
                var glassesSettings = playerSettings.glassesSettings;
                var rightWandSettings = playerSettings.rightWandSettings;
                var leftWandSettings = playerSettings.leftWandSettings;
                var scaleSettings = playerSettings.scaleSettings;
                var gameboardSettings = playerSettings.gameboardSettings;

                Glasses.Update(GlassesHandle, glassesSettings, scaleSettings, gameboardSettings, spectatorSettings);
                Wand.Update(GlassesHandle, rightWandSettings, scaleSettings, gameboardSettings);
                Wand.Update(GlassesHandle, leftWandSettings, scaleSettings, gameboardSettings);
            }
        }

        #endregion


        #region Public Classes

        /// <summary>
        /// Suspends coroutine execution until the provided player has connected.
        /// </summary>
        /// <remarks>
        /// This custom yield instruction is useful for performing tasks that depend
        /// on a specific player being connected and initialized.
        /// This is a tidy alternative to repeatedly polling <see cref="Player.IsConnected(PlayerIndex)"/>
        /// in a loop or using UnityEngine.WaitUntil.
        /// </remarks>
        /// <example>
        /// Let's assume that we've written a script that needs to cache the player's head pose root GameObject
        /// in a member variable for later use. The head pose root GameObject doesn't exist until the specified player
        /// connects and creates it during initialization, so we need to wait for the player to connect.
        ///
        /// private IEnumerator Start()
        /// {
        ///     // ...
        ///     // We can do some other initialization here before we begin waiting.
        ///     // ...
        ///
        ///     // Suspend this coroutine until the specified player has connected.
        ///     yield return new Player.WaitUntilPlayerConnected(PlayerIndex.Two);
        ///
        ///     // Now that the player has connected, it is safe to obtain and store the player's head pose root GameObject.
        ///     m_glassesPoseRoot = Glasses.GetPoseRoot(PlayerIndex.Two);
        /// }
        /// </example>
        public class WaitUntilPlayerConnected : CustomYieldInstruction
        {
            public override bool keepWaiting => !TiltFive.Player.IsConnected(playerIndex);
            private PlayerIndex playerIndex = PlayerIndex.One;

            public WaitUntilPlayerConnected(PlayerIndex playerIndex)
            {
                this.playerIndex = playerIndex;
            }
        }

        #endregion
    }
}