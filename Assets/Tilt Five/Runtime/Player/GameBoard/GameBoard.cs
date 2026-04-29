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

namespace TiltFive
{
    /// <summary>
    /// Represents the game board.
    /// </summary>
    [ExecuteInEditMode]
    public class GameBoard : UniformScaleTransform
    {
        #region Public Fields

        /// <summary>
        /// Shows the game board gizmo in the editor.
        /// </summary>
        [Tooltip("Show/Hide the Board Gizmo in the Editor.")]
        public bool ShowGizmo;

        [Tooltip("Show/Hide the Unit Grid on the Board Gizmo in the Editor.")]
        public bool ShowGrid;

        public float GridHeightOffset = 0f;
        public bool StickyHeightOffset = true;


        /// <summary>
        /// Sets the opacity of the game board gizmo in the editor.
        /// </summary>
        [Tooltip("Sets the Alpha transparency of the Board Gizmo in the Editor.")]
        [Range(0f, 1f)]
        public float GizmoOpacity = 0.75f;

        /// <summary>
        /// The gameboard configuration, such as LE, XE, or folded XE.
        /// </summary>
        [HideInInspector]
        [Obsolete("Please use Gameboard.TryGetGameboardType() instead.")]
        public GameboardType GameboardType;

        #endregion Public Fields


        #region Private Fields

#if UNITY_EDITOR

        /// <summary>
        /// <b>EDITOR-ONLY</b> The board gizmo.
        /// </summary>
		private TableTopGizmo boardGizmo = new TableTopGizmo();

        /// <summary>
        /// <b>EDITOR-ONLY</b> The Y offset of the grid, taking snapping into account.
        /// </summary>
        private float gridOffsetY => StickyHeightOffset ? Mathf.RoundToInt(GridHeightOffset) : GridHeightOffset;

        /// <summary>
        /// <b>EDITOR-ONLY</b> The current content scale unit (e.g. inches, cm, snoots, etc) from the glasses settings.
        /// </summary>
        private LengthUnit currentContentScaleUnit;

        /// <summary>
        /// <b>EDITOR-ONLY</b> The current content scale value (e.g. 1.0 inch|centimeter|etc) from the glasses settings.
        /// </summary>
        private float currentContentScaleRatio;

        /// <summary>
        /// <b>EDITOR-ONLY</b> The current local scale of the attached GameObject's Transform.
        /// </summary>
        private Vector3 currentScale;

        private const float MIN_SCALE = 0.00001f;

#endif // UNITY_EDITOR

        private static Dictionary<PlayerIndex, GameboardType> playerGameboards = new Dictionary<PlayerIndex, GameboardType>();

        #endregion Private Fields


        #region Public Enums

        /// <summary>
        /// Represents cardinal positions around the gameboard (i.e. the centers of the edges of the gameboard),
        /// relative to a user positioned at the near (default) edge.
        /// </summary>
        public enum Edge
        {
            /// <summary>
            /// The near edge of the gameboard.
            /// </summary>
            /// <remarks>This is the side of the gameboard with the T5 logo.</remarks>
            Near,
            /// <summary>
            /// The far edge of the gameboard.
            /// </summary>
            /// <remarks>This edge is positioned opposite the near edge.</remarks>
            Far,
            /// <summary>
            /// The left edge of the gameboard.
            /// </summary>
            /// <remarks>From the perspective of a user sitting at the near edge of the gameboard,
            /// this edge is to their left.</remarks>
            Left,
            /// <summary>
            /// The right edge of the gameboard.
            /// </summary>
            /// <remarks>From the perspective of a user sitting at the near edge of the gameboard,
            /// this edge is to their right.</remarks>
            Right
        }

        /// <summary>
        /// Represents ordinal positions around the gameboard, relative to a user positioned at the near (default) edge.
        /// </summary>
        public enum Corner
        {
            FarLeft,
            FarRight,
            NearLeft,
            NearRight
        }

        #endregion Public Enums


        #region Public Structs

        [Obsolete("GameboardDimensions is obsolete, please use GameboardExtents instead.")]
        public struct GameboardDimensions
        {
            public Length playableSpaceX;
            public Length playableSpaceY;
            public Length borderWidth;
            public Length totalSpaceX => playableSpaceX + (borderWidth * 2);
            public Length totalSpaceY => playableSpaceY + (borderWidth * 2);

            public GameboardDimensions(T5_GameboardSize gameboardSize)
            {
                var gameboardExtents = new GameboardExtents(gameboardSize);
                playableSpaceX = gameboardExtents.ViewableSpanX;
                playableSpaceY = gameboardExtents.ViewableSpanZ;
                borderWidth = new Length(GameboardExtents.BORDER_WIDTH_IN_METERS, LengthUnit.Meters);
            }
        }

        /// <summary>
        /// Represents the distances from the gameboard tracking origin to the borders of the gameboard's viewable area.
        /// </summary>
        public struct GameboardExtents
        {
            /// <summary>
            /// The distance in meters from the gameboard origin to the edge of the viewable area in the positive X direction.
            /// </summary>
            public Length ViewableExtentPositiveX;

            /// <summary>
            /// The distance in meters from the gameboard origin to the edge of the viewable area in the negative X direction.
            /// </summary>
            public Length ViewableExtentNegativeX;

            /// <summary>
            /// The distance in meters from the gameboard origin to the edge of the viewable area in the positive Z direction.
            /// </summary>
            public Length ViewableExtentPositiveZ;

            /// <summary>
            /// The distance in meters from the gameboard origin to the edge of the viewable area in the negative Z direction.
            /// </summary>
            public Length ViewableExtentNegativeZ;

            /// <summary>
            /// The distance in meters above the gameboard origin that the viewable area extends in the positive Y direction.
            /// </summary>
            public Length ViewableExtentPositiveY;

            /// <summary>
            /// The distance in meters representing the side-to-side width of the viewable area of the gameboard;
            /// </summary>
            public Length ViewableSpanX => ViewableExtentPositiveX + ViewableExtentNegativeX;
            /// <summary>
            /// The distance in meters representing the front-to-back length of the viewable area of the gameboard;
            /// </summary>
            public Length ViewableSpanY => ViewableExtentPositiveY;
            /// <summary>
            /// The distance in meters representing the height of the viewable area of the gameboard;
            /// </summary>
            public Length ViewableSpanZ => ViewableExtentPositiveZ + ViewableExtentNegativeZ;

            /// <summary>
            /// The distance from the tracking origin to the physical center of the gameboard.
            /// </summary>
            /// <remarks>
            /// For square configurations like the LE gameboard, this value will be zero,
            /// but this can be helpful for spatial logic relating to the XE or XE Raised gameboards.
            /// </remarks>
            public Length OriginOffsetZ => new Length(ViewableSpanZ.ToMeters / 2f - ViewableExtentNegativeZ.ToMeters, LengthUnit.Meters);

            internal const float BORDER_WIDTH_IN_METERS = 0.05f;
            internal static readonly T5_GameboardSize GAMEBOARD_SIZE_LE;
            internal static readonly T5_GameboardSize GAMEBOARD_SIZE_XE;
            internal static readonly T5_GameboardSize GAMEBOARD_SIZE_XE_RAISED;

            static GameboardExtents()
            {
                GAMEBOARD_SIZE_LE = new T5_GameboardSize(0.35f, 0.35f, 0.35f, 0.35f, 0f);
                GAMEBOARD_SIZE_XE = new T5_GameboardSize(0.35f, 0.35f, 0.61667f, 0.35f, 0.0f);
                GAMEBOARD_SIZE_XE_RAISED = new T5_GameboardSize(0.35f, 0.35f, 0.55944f, 0.35f, 0.20944f);
            }

            public GameboardExtents(T5_GameboardSize gameboardSize)
            {
                ViewableExtentPositiveX = new Length(gameboardSize.ViewableExtentPositiveX, LengthUnit.Meters);
                ViewableExtentNegativeX = new Length(gameboardSize.ViewableExtentNegativeX, LengthUnit.Meters);
                ViewableExtentPositiveZ = new Length(gameboardSize.ViewableExtentPositiveZ, LengthUnit.Meters);
                ViewableExtentNegativeZ = new Length(gameboardSize.ViewableExtentNegativeZ, LengthUnit.Meters);
                ViewableExtentPositiveY = new Length(gameboardSize.ViewableExtentPositiveY, LengthUnit.Meters);
            }

            /// <summary>
            /// Gets the coordinates of the specified corner in gameboard space.
            /// </summary>
            /// <param name="corner"></param>
            /// <returns>The coordinates of the specified corner in gameboard space.</returns>
            /// <exception cref="ArgumentException">Thrown when an invalid value for <paramref name="corner"/> is provided.</exception>
            public Vector3 GetCornerPositionInGameboardSpace(Corner corner)
            {
                switch (corner)
                {
                    case Corner.FarLeft:
                        return Vector3.forward * ViewableExtentPositiveZ.ToMeters
                            + Vector3.left * ViewableExtentNegativeX.ToMeters;
                    case Corner.FarRight:
                        return Vector3.forward * ViewableExtentPositiveZ.ToMeters
                            + Vector3.right * ViewableExtentPositiveX.ToMeters;
                    case Corner.NearLeft:
                        return Vector3.back * ViewableExtentNegativeZ.ToMeters
                            + Vector3.left * ViewableExtentNegativeX.ToMeters;
                    case Corner.NearRight:
                        return Vector3.back * ViewableExtentNegativeZ.ToMeters
                            + Vector3.right * ViewableExtentPositiveX.ToMeters;
                    default:
                        throw new System.ArgumentException("Cannot get corner positions for invalid Corner enum values.");
                }
            }

            /// <summary>
            /// Gets the coordinates of the center of the specified edge in gameboard space.
            /// </summary>
            /// <param name="edge"></param>
            /// <returns>The coordinates of the center of the specified edge in gameboard space.</returns>
            /// <exception cref="ArgumentException">Thrown when an invalid value for <paramref name="edge"/> is provided.</exception>
            public Vector3 GetEdgeCenterPositionInGameboardSpace(Edge edge)
            {
                switch (edge)
                {
                    case Edge.Left:
                        return Vector3.left * ViewableExtentNegativeX.ToMeters
                            + Vector3.forward * OriginOffsetZ.ToMeters;
                    case Edge.Right:
                        return Vector3.right * ViewableExtentPositiveX.ToMeters
                            + Vector3.forward * OriginOffsetZ.ToMeters;
                    case Edge.Near:
                        return Vector3.back * ViewableExtentNegativeZ.ToMeters;
                    case Edge.Far:
                        return Vector3.forward * ViewableExtentPositiveZ.ToMeters;
                    default:
                        throw new System.ArgumentException("Cannot get edge center positions for invalid Edge enum values.");
                }
            }

            /// <summary>
            /// Gets the coordinates of the center of the gameboard in gameboard space.
            /// </summary>
            /// <returns></returns>
            /// <remarks>For the LE gameboard, the tracking origin and the center of the gameboard overlap,
            /// so this will return a vector of [0, 0, 0], but for the XE gameboard variants, the tracking origin is offset
            /// from the gameboard's physical center point, making function useful for accounting for this offset.</remarks>
            public Vector3 GetPhysicalCenterPositionInGameboardSpace()
            {
                // If we ever have some funky gameboard in the future with a double-eccentric tracking origin,
                // we'd just need to add another vector along the X axis (e.g. Vector3.right * OriginOffsetX.ToMeters)
                return Vector3.forward * OriginOffsetZ.ToMeters;
            }
        }

        #endregion Private Structs


        #region Public Functions

        /// <summary>
        /// Attempts to check the latest glasses pose for the current gameboard type, such as LE, XE, or none.
        /// </summary>
        /// <param name="gameboardType">Output gameboard type.  Contains
        /// <see cref="GameboardType.GameboardType_None"/> if no pose was provided, which can happen
        /// if the user looks away and the head tracking camera loses sight of the gameboard.</param>
        /// <returns>Returns true on successful pose retrieval, false otherwise.</returns>
        public static bool TryGetGameboardType(out GameboardType gameboardType)
        {
            return TryGetGameboardType(PlayerIndex.One, out gameboardType);
        }

        public static bool TryGetGameboardType(PlayerIndex playerIndex, out GameboardType gameboardType)
        {
            // Return false and a default gameboard if the player is nonexistent or disconnected.
            if(!Player.TryGetGlassesHandle(playerIndex, out var glassesHandle)
                || !playerGameboards.TryGetValue(playerIndex, out var currentGameboardType))
            {
                gameboardType = GameboardType.GameboardType_None;
                return false;
            }

            gameboardType = currentGameboardType;
            return true;
        }

        /// <summary>
        /// Attempts to obtain the physical dimensions for a particular gameboard type.
        /// </summary>
        /// <param name="gameboardType"></param>
        /// <param name="gameboardDimensions"></param>
        /// <returns>Returns dimensions for <see cref="GameboardType.GameboardType_LE"/> if it fails.</returns>
        [Obsolete("TryGetGameboardDimensions is obsolete. Please use TryGetGameboardExtents instead.")]
        public static bool TryGetGameboardDimensions(GameboardType gameboardType, out GameboardDimensions gameboardDimensions)
        {
            if(gameboardType == GameboardType.GameboardType_None)
            {
                gameboardDimensions = new GameboardDimensions();
                return false;
            }

            // Default to the LE gameboard dimensions in meters.
            T5_GameboardSize gameboardSize = GameboardExtents.GAMEBOARD_SIZE_LE;
            int result = NativePlugin.T5_RESULT_UNKNOWN_ERROR;

            try
            {
                result = NativePlugin.GetGameboardDimensions(gameboardType, ref gameboardSize);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

            gameboardDimensions = new GameboardDimensions(gameboardSize);

            return result == NativePlugin.T5_RESULT_SUCCESS;
        }

        /// <summary>
        /// Attempts to obtain the physical dimensions for a particular gameboard type.
        /// </summary>
        /// <param name="gameboardType"></param>
        /// <param name="gameboardExtents"></param>
        /// <returns>Returns true on successful gameboard extents retrieval, and false otherwise.
        /// Use this return value to determine whether <paramref name="gameboardExtents"/> is valid.</returns>
        public static bool TryGetGameboardExtents(GameboardType gameboardType, out GameboardExtents gameboardExtents)
        {
            if(gameboardType == GameboardType.GameboardType_None)
            {
                gameboardExtents = new GameboardExtents();
                return false;
            }

            T5_GameboardSize gameboardSize = new T5_GameboardSize();
            int result = NativePlugin.T5_RESULT_UNKNOWN_ERROR;

            try
            {
                result = NativePlugin.GetGameboardDimensions(gameboardType, ref gameboardSize);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

            gameboardExtents = new GameboardExtents(gameboardSize);

            return result == NativePlugin.T5_RESULT_SUCCESS;
        }

        /// <summary>
        /// Transforms the provided pose from gameboard space to world space.
        /// </summary>
        /// <param name="positionInGameboardSpace">A position in gameboard space.</param>
        /// <param name="rotationToGameboardSpace">An orientation in gameboard space.</param>
        /// <param name="playerIndex">The player whose content scale settings should be considered while converting between spaces.
        /// This is necessary because multiple players with differing scale settings may share the same gameboard.</param>
        /// <param name="positionInWorldSpace">The resulting position in Unity world space.</param>
        /// <param name="rotationToWorldSpace">The resulting orientation in Unity world space.</param>
        /// <returns>Returns false and sets <paramref name="positionInGameboardSpace"/> and <paramref name="rotationToWorldSpace"/>
        /// to default values if the function failed while calculating the transformed pose; returns true otherwise.
        /// This function can fail if <paramref name="playerIndex"/> is invalid.</returns>
        public bool TransformPose(Vector3 positionInGameboardSpace, Quaternion rotationToGameboardSpace, PlayerIndex playerIndex,
            out Vector3 positionInWorldSpace, out Quaternion rotationToWorldSpace)
        {
            if (!Player.TryGetSettings(playerIndex, out var playerSettings))
            {
                positionInWorldSpace = Vector3.zero;
                rotationToWorldSpace = Quaternion.identity;
                return false;
            }

            TransformPose(positionInGameboardSpace, rotationToGameboardSpace, playerSettings.scaleSettings,
                out positionInWorldSpace, out rotationToWorldSpace);
            return true;
        }

        /// <summary>
        /// Transforms the provided pose from gameboard space to world space.
        /// </summary>
        /// <param name="poseInGameboardSpace">A position and orientation in gameboard space.</param>
        /// <param name="playerIndex">The player whose content scale settings should be considered while converting between spaces.
        /// This is necessary because multiple players with differing scale settings may share the same gameboard.</param>
        /// <param name="poseInWorldSpace">The resulting position and orientation in Unity world space.</param>
        /// <returns>Returns false and sets <paramref name="poseInWorldSpace"/> to an empty pose
        /// if the function failed while calculating the transformed pose; returns true otherwise.
        /// This function can fail if <paramref name="playerIndex"/> is invalid.</returns>
        public bool TransformPose(Pose poseInGameboardSpace, PlayerIndex playerIndex, out Pose poseInWorldSpace)
        {
            if (!Player.TryGetSettings(playerIndex, out var playerSettings))
            {
                poseInWorldSpace = new Pose();
                return false;
            }

            poseInWorldSpace = TransformPose(poseInGameboardSpace, playerSettings.scaleSettings);
            return true;
        }

        /// <summary>
        /// Transforms the provided position from gameboard space to world space.
        /// </summary>
        /// <param name="pointInGameboardSpace">A point in gameboard space.</param>
        /// <param name="playerIndex">The player whose content scale settings should be considered while converting between spaces.
        /// This is necessary because multiple players with differing scale settings may share the same gameboard.</param>
        /// <param name="pointInWorldSpace">The resulting point in Unity world space.</param>
        /// <returns>Returns false and sets <paramref name="pointInWorldSpace"/> to <see cref="Vector3.zero"/>
        /// if the function failed while calculating the transformed point; returns true otherwise.
        /// This function can fail if <paramref name="playerIndex"/> is invalid.</returns>
        public bool TransformPoint(Vector3 pointInGameboardSpace, PlayerIndex playerIndex, out Vector3 pointInWorldSpace)
        {
            if (!Player.TryGetSettings(playerIndex, out var playerSettings))
            {
                pointInWorldSpace = Vector3.zero;
                return false;
            }

            pointInWorldSpace = TransformPoint(pointInGameboardSpace, playerSettings.scaleSettings);
            return true;
        }

        /// <summary>
        /// Transforms the provided pose from world space to gameboard space.
        /// </summary>
        /// <param name="positionInWorldSpace">A position in Unity world space.</param>
        /// <param name="rotationToWorldSpace">An orientation in Unity world space.</param>
        /// <param name="playerIndex">The player whose content scale settings should be considered while converting between spaces.
        /// This is necessary because multiple players with differing scale settings may share the same gameboard.</param>
        /// <param name="positionInGameboardSpace">The resulting position in gameboard space.</param>
        /// <param name="rotationToGameboardSpace">The resulting orientation in gameboard space.</param>
        /// <returns>Returns false and sets <paramref name="positionInGameboardSpace"/> and <paramref name="rotationToGameboardSpace"/>
        /// to default values if the function failed while calculating the transformed pose; returns true otherwise.
        /// This function can fail if <paramref name="playerIndex"/> is invalid.</returns>
        public bool InverseTransformPose(Vector3 positionInWorldSpace, Quaternion rotationToWorldSpace, PlayerIndex playerIndex,
            out Vector3 positionInGameboardSpace, out Quaternion rotationToGameboardSpace)
        {
            if (!Player.TryGetSettings(playerIndex, out var playerSettings))
            {
                positionInGameboardSpace = Vector3.zero;
                rotationToGameboardSpace = Quaternion.identity;
                return false;
            }

            InverseTransformPose(positionInWorldSpace, rotationToWorldSpace, playerSettings.scaleSettings,
                out positionInGameboardSpace, out rotationToGameboardSpace);
            return true;
        }

        /// <summary>
        /// Transforms the provided pose from world space to gameboard space.
        /// </summary>
        /// <param name="poseInWorldSpace">A position and orientation in Unity world space.</param>
        /// <param name="playerIndex">The player whose content scale settings should be considered while converting between spaces.
        /// This is necessary because multiple players with differing scale settings may share the same gameboard.</param>
        /// <param name="poseInGameboardSpace">The resulting position and orientation in gameboard space.</param>
        /// <returns>Returns false and sets <paramref name="poseInGameboardSpace"/> to an empty pose
        /// if the function failed while calculating the transformed pose; returns true otherwise.
        /// This function can fail if <paramref name="playerIndex"/> is invalid.</returns>
        public bool InverseTransformPose(Pose poseInWorldSpace, PlayerIndex playerIndex, out Pose poseInGameboardSpace)
        {
            if (!Player.TryGetSettings(playerIndex, out var playerSettings))
            {
                poseInGameboardSpace = new Pose();
                return false;
            }

            poseInGameboardSpace = InverseTransformPose(poseInWorldSpace, playerSettings.scaleSettings);
            return true;
        }

        /// <summary>
        /// Transforms the provided position from world space to gameboard space.
        /// </summary>
        /// <param name="pointInWorldSpace">A position in Unity world space.</param>
        /// <param name="playerIndex">The player whose content scale settings should be considered while converting between spaces.
        /// This is necessary because multiple players with differing scale settings may share the same gameboard.</param>
        /// <param name="pointInGameboardSpace">The resulting point in gameboard space.</param>
        /// <returns>Returns false and sets <paramref name="pointInGameboardSpace"/> to <see cref="Vector3.zero"/>
        /// if the function failed while calculating the transformed point; returns true otherwise.
        /// This function can fail if <paramref name="playerIndex"/> is invalid.</returns>
        public bool InverseTransformPoint(Vector3 pointInWorldSpace, PlayerIndex playerIndex, out Vector3 pointInGameboardSpace)
        {
            if (!Player.TryGetSettings(playerIndex, out var playerSettings))
            {
                pointInGameboardSpace = Vector3.zero;
                return false;
            }

            pointInGameboardSpace = InverseTransformPoint(pointInWorldSpace, playerSettings.scaleSettings);
            return true;
        }

#if UNITY_EDITOR

        new public void Awake()
        {
            base.Awake();
            currentScale = transform.localScale;
        }

        /// <summary>
        /// Draws the game board gizmo in the Editor Scene view.
        /// </summary>
		public void DrawGizmo(ScaleSettings scaleSettings, GameBoardSettings gameBoardSettings)
        {
            UnifyScale();

            if (ShowGizmo)
            {
                boardGizmo.Draw(scaleSettings, gameBoardSettings, GizmoOpacity, ShowGrid, gridOffsetY);
            }

            var sceneViewRepaintNecessary = ScaleCompensate(scaleSettings);
            sceneViewRepaintNecessary |= ContentScaleCompensate(scaleSettings);

            if(sceneViewRepaintNecessary)
            {
                boardGizmo.ResetGrid(scaleSettings, gameBoardSettings);     // This may need to change once separate game board configs are in.
                UnityEditor.SceneView.lastActiveSceneView.Repaint();
            }
        }

        /// <summary>
        /// Determines which gameboard type to render in the editor.
        /// </summary>
        /// <param name="gameBoardSettings"></param>
        /// <returns></returns>
        public GameboardType GetDisplayedGameboardType(GameBoardSettings gameBoardSettings)
        {
            return GetDisplayedGameboardType(PlayerIndex.One, gameBoardSettings);
        }

        public static GameboardType GetDisplayedGameboardType(PlayerIndex playerIndex, GameBoardSettings gameBoardSettings)
        {
            if(playerIndex == PlayerIndex.None)
            {
                return GameboardType.GameboardType_None;
            }

            var displayedGameboardType = playerGameboards.TryGetValue(playerIndex, out var gameboardType)
                ? gameboardType
                : GameboardType.GameboardType_LE;

            displayedGameboardType = gameBoardSettings.gameboardTypeOverride != GameboardType.GameboardType_None
                ? gameBoardSettings.gameboardTypeOverride
                : displayedGameboardType;

            return displayedGameboardType;
        }

#endif  // UNITY_EDITOR

        #endregion Public Functions


        #region Internal Functions

        internal static void SetGameboardType(PlayerIndex playerIndex, GameboardType gameboardType)
        {
            if(playerIndex == PlayerIndex.None)
            {
                return;
            }

            playerGameboards[playerIndex] = gameboardType;
        }

        internal static void SetGameboardType(GlassesHandle glassesHandle, GameboardType gameboardType)
        {
            if (Player.TryGetPlayerIndex(glassesHandle, out var playerIndex))
            {
                SetGameboardType(playerIndex, gameboardType);
            }
        }

        /// <summary>
        /// Transforms the provided pose from gameboard space to world space.
        /// </summary>
        /// <param name="poseInGameboardSpace">A position and orientation in gameboard space.</param>
        /// <param name="scaleSettings">The scale settings that should be considered while converting between spaces.
        /// This is necessary because multiple players with differing scale settings may share the same gameboard.</param>
        /// <returns>The resulting position and orientation in Unity world space.</returns>
        internal Pose TransformPose(Pose poseInGameboardSpace, ScaleSettings scaleSettings)
        {
            TransformPose(poseInGameboardSpace.position, poseInGameboardSpace.rotation, scaleSettings,
                out var posUWRLD, out var rotToUWRLD_OBJ);
            return new Pose(posUWRLD, rotToUWRLD_OBJ);
        }

        /// <summary>
        /// Transforms the provided pose from gameboard space to world space.
        /// </summary>
        /// <param name="positionInGameboardSpace">A position in gameboard space.</param>
        /// <param name="rotationToGameboardSpace">An orientation in gameboard space.</param>
        /// <param name="scaleSettings">The scale settings that should be considered while converting between spaces.
        /// This is necessary because multiple players with differing scale settings may share the same gameboard.</param>
        /// <param name="positionInWorldSpace">The resulting position in Unity world space.</param>
        /// <param name="rotationToWorldSpace">The resulting orientation in Unity world space.</param>
        internal void TransformPose(Vector3 positionInGameboardSpace, Quaternion rotationToGameboardSpace, ScaleSettings scaleSettings,
            out Vector3 positionInWorldSpace, out Quaternion rotationToWorldSpace)
        {
            positionInWorldSpace = TransformPoint(positionInGameboardSpace, scaleSettings);

            // The rotation from gameboard space to world space
            var rotToUWRLD_UGBD = transform.rotation;

            // The rotation from the object's local space to gameboard space
            var rotToUGBD_OBJ = rotationToGameboardSpace;

            // The rotation from the object's local space to world space
            var rotToUWRLD_OBJ = rotToUWRLD_UGBD * rotToUGBD_OBJ;

            rotationToWorldSpace = rotToUWRLD_OBJ;
        }

        /// <summary>
        /// Transforms the provided position from gameboard space to world space.
        /// </summary>
        /// <param name="pointInGameboardSpace">A position in gameboard space.</param>
        /// <param name="scaleSettings">The scale settings that should be considered while converting between spaces.
        /// This is necessary because multiple players with differing scale settings may share the same gameboard.</param>
        /// <returns>The resulting position in Unity world space.</returns>
        internal Vector3 TransformPoint(Vector3 pointInGameboardSpace, ScaleSettings scaleSettings)
        {
            var pos_UGBD = pointInGameboardSpace;

            var scaleToUWRLD_UGBD = scaleSettings.GetScaleToUWRLD_UGBD(localScale);
            var rotToUWRLD_UGBD = transform.rotation;
            var pos_UWRLD = rotToUWRLD_UGBD * (scaleToUWRLD_UGBD * pos_UGBD) + transform.position;

            return pos_UWRLD;
        }

        /// <summary>
        /// Transforms the provided pose from world space to gameboard space.
        /// </summary>
        /// <param name="poseInWorldSpace">A position and orientation in Unity world space.</param>
        /// <param name="scaleSettings">The scale settings that should be considered while converting between spaces.
        /// This is necessary because multiple players with differing scale settings may share the same gameboard.</param>
        /// <returns>The resulting position and orientation in gameboard space.</returns>
        internal Pose InverseTransformPose(Pose poseInWorldSpace, ScaleSettings scaleSettings)
        {
            InverseTransformPose(poseInWorldSpace.position, poseInWorldSpace.rotation, scaleSettings,
                out var posUGBD, out var rotToUGBD_OBJ);
            return new Pose(posUGBD, rotToUGBD_OBJ);
        }

        /// <summary>
        /// Transforms the provided pose from world space to gameboard space.
        /// </summary>
        /// <param name="positionInWorldSpace">A position in Unity world space.</param>
        /// <param name="rotationToWorldSpace">An orientation in Unity world space.</param>
        /// <param name="scaleSettings">The scale settings that should be considered while converting between spaces.
        /// This is necessary because multiple players with differing scale settings may share the same gameboard.</param>
        /// <param name="positionInGameboardSpace">The resulting position in gameboard space.</param>
        /// <param name="rotationToGameboardSpace">The resulting orientation in gameboard space.</param>
        internal void InverseTransformPose(Vector3 positionInWorldSpace, Quaternion rotationToWorldSpace, ScaleSettings scaleSettings,
            out Vector3 positionInGameboardSpace, out Quaternion rotationToGameboardSpace)
        {
            positionInGameboardSpace = InverseTransformPoint(positionInWorldSpace, scaleSettings);

            // The rotation from gameboard space to world space
            var rotToUWRLD_UGBD = transform.rotation;

            // The rotation from world space to gameboard space
            var rotToUGBD_UWRLD = Quaternion.Inverse(rotToUWRLD_UGBD);

            // The rotation from the object's local space to world space
            var rotToUWRLD_OBJ = rotationToWorldSpace;

            // The rotation from the object's local space to gameboard space
            var rotToUGBD_OBJ = rotToUGBD_UWRLD * rotToUWRLD_OBJ;

            rotationToGameboardSpace = rotToUGBD_OBJ;
        }

        /// <summary>
        /// Transforms the provided position from world space to gameboard space.
        /// </summary>
        /// <param name="pointInWorldSpace">A position in Unity world space.</param>
        /// <param name="scaleSettings">The scale settings that should be considered while converting between spaces.
        /// This is necessary because multiple players with differing scale settings may share the same gameboard.</param>
        /// <returns>The resulting position in gameboard space.</returns>
        internal Vector3 InverseTransformPoint(Vector3 pointInWorldSpace, ScaleSettings scaleSettings)
        {
            var pos_UWRLD = pointInWorldSpace;

            var scaleToUGBD_UWRLD = 1f / scaleSettings.GetScaleToUWRLD_UGBD(localScale);
            var rotToUGBD_UWRLD = Quaternion.Inverse(transform.rotation);
            var pos_UGBD = pos_UWRLD - transform.position;
            pos_UGBD = rotToUGBD_UWRLD * pos_UGBD;
            pos_UGBD *= scaleToUGBD_UWRLD;

            return pos_UGBD;
        }

        #endregion


        #region Private Functions

#if UNITY_EDITOR

        ///<summary>
        /// Tells the Scene view in the editor to zoom in/out as the game board is scaled.
        ///</summary>
        ///<remarks>
        /// This function enforces a minumum scale value for the attached GameObject transform.
        ///</remarks>
        private bool ScaleCompensate(ScaleSettings scaleSettings)
        {
            if(currentScale == transform.localScale) { return false; }

            // Prevent negative scale values for the game board.
            if( transform.localScale.x < MIN_SCALE)
            {
                transform.localScale = Vector3.one * MIN_SCALE;
            }

            //var sceneView = UnityEditor.SceneView.lastActiveSceneView;

            //sceneView.Frame(new Bounds(transform.position, (1/5f) * Vector3.one * scaleSettings.worldSpaceUnitsPerPhysicalMeter / localScale ), true);

            currentScale = transform.localScale;
            return true;
        }

        ///<summary>
        /// Tells the Scene view in the editor to zoom in/out as the content scale is modified.
        ///</summary>
        private bool ContentScaleCompensate(ScaleSettings scaleSettings)
        {
            if(currentContentScaleRatio == scaleSettings.contentScaleRatio
            && currentContentScaleUnit == scaleSettings.contentScaleUnit) { return false; }

            //var sceneView = UnityEditor.SceneView.lastActiveSceneView;

            currentContentScaleUnit = scaleSettings.contentScaleUnit;
            currentContentScaleRatio = scaleSettings.contentScaleRatio;

            //sceneView.Frame(new Bounds(transform.position, (1/5f) * Vector3.one * scaleSettings.worldSpaceUnitsPerPhysicalMeter / localScale ), true);

            return true;
        }


#endif  // UNITY_EDITOR

        #endregion Private Functions
    }
}
