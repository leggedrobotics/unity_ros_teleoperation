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

namespace TiltFive
{
    /// <summary>
    /// Handles functionality relating to mesh assets provided in the Tilt Five Unity SDK.
    /// </summary>
    public static class MeshAssets
    {
        #region Private Fields

#if TILT_FIVE_PACKAGE
        private const string ROOT_DIRECTORY = "Packages/com.tiltfive.sdk/";
#else
        private const string ROOT_DIRECTORY = "Assets/Tilt Five/";
#endif
        private const string MESHES_DIRECTORY = ROOT_DIRECTORY + "Meshes/";

        #endregion Private Fields


        #region Public Functions

        /// <summary>
        /// Gets the path to the specified gameboard mesh asset.
        /// </summary>
        /// <param name="gameboardType"></param>
        /// <exception cref="System.ArgumentException">Thrown when <paramref name="gameboardType"/> is set to <see cref="GameboardType.GameboardType_None"/>.</exception>
        public static string GetPathToGameboardMesh(GameboardType gameboardType)
        {
            string meshFileName = "";
            switch (gameboardType)
            {
                case GameboardType.GameboardType_LE:
                    meshFileName = "Gameboard_LE.fbx";
                    break;
                case GameboardType.GameboardType_XE:
                    meshFileName = "Gameboard_XE.fbx";
                    break;
                case GameboardType.GameboardType_XE_Raised:
                    meshFileName = "Gameboard_XE_Raised.fbx";
                    break;
                default:
                    throw new System.ArgumentException();
            }

            return MESHES_DIRECTORY + meshFileName;
        }

        /// <summary>
        /// Gets the path to Tilt Five logo mesh asset.
        /// </summary>
        public static string GetPathToT5LogoMesh()
        {
            return MESHES_DIRECTORY + "T5-Logo.fbx";
        }

        /// <summary>
        /// Gets the path to the Tilt Five Wand mesh asset.
        /// </summary>
        public static string GetPathToWandMesh()
        {
            return MESHES_DIRECTORY + "T5-Wand.fbx";
        }

        /// <summary>
        /// Gets the path to the Tilt Five Glasses mesh asset.
        /// </summary>
        public static string GetPathToGlassesMesh()
        {
            return MESHES_DIRECTORY + "T5-Glasses.fbx";
        }

        #endregion Public Functions
    }
}
