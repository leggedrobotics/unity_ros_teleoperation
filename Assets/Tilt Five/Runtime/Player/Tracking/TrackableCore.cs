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

using UnityEngine;
using TiltFive.Logging;

namespace TiltFive
{
    public abstract class TrackableCore<TSettings, TState> where TSettings : TrackableSettings
    {
        #region Properties

        /// <summary>
        /// The pose of the trackable w.r.t. the gameboard reference frame.
        /// </summary>
        public Pose Pose_GameboardSpace { get => pose_UGBD; }
        protected Pose pose_UGBD;

        /// <summary>
        /// The Pose of the trackable in Unity world space.
        /// </summary>
        public Pose Pose_UnityWorldSpace { get => pose_UWRLD; }
        protected Pose pose_UWRLD;

        /// <summary>
        /// Whether or not the trackable is being tracked.
        /// </summary>
        public bool IsTracked { get => isTracked; }
        protected bool isTracked = false;

        /// <summary>
        /// Whether or not the trackable is connected.
        /// </summary>
        public bool IsConnected { get => isConnected; }
        protected bool isConnected = false;

        /// <summary>
        /// The pose of the gameboard reference frame w.r.t. the Unity world-space
        /// reference frame.
        /// </summary>
        protected Pose gameboardPos_UWRLD;

        #endregion Properties


        #region Protected Functions

        protected void Reset(TSettings settings)
        {
            SetDefaultPoseGameboardSpace(settings);
            isTracked = false;
        }

        // Update is called once per frame
        protected virtual void Update(TSettings settings, ScaleSettings scaleSettings, GameBoardSettings gameboardSettings)
        {
            if(settings == null)
            {
                Log.Error("TrackableSettings configuration required for tracking updates.");
                return;
            }

            // Get the game board pose.
            gameboardPos_UWRLD = new Pose(gameboardSettings.gameBoardCenter,
                Quaternion.Inverse(gameboardSettings.currentGameBoard.rotation));

            var successfullyConnected = TryCheckConnected(out isConnected) && isConnected;
            var successfullyGotState = TryGetStateFromPlugin(out var state, out bool poseIsValid, gameboardSettings);

            if (successfullyConnected && successfullyGotState && poseIsValid)
            {
                isTracked = true;
                SetPoseGameboardSpace(state, settings, scaleSettings, gameboardSettings);
            }
            else
            {
                isTracked = false;
                SetInvalidPoseGameboardSpace(state, settings, scaleSettings, gameboardSettings);
            }

            SetPoseUnityWorldSpace(scaleSettings, gameboardSettings);

            SetDrivenObjectTransform(settings, scaleSettings, gameboardSettings);
        }

        protected static Vector3 ConvertPosGBDToUGBD(Vector3 pos_GBD)
        {
            // Swap Y and Z to change between GBD and UGBD
            var pos_UGBD = new Vector3(pos_GBD.x, pos_GBD.z, pos_GBD.y);
            return pos_UGBD;
        }

        protected static Pose GameboardToWorldSpace(Pose pose_GameboardSpace,
            ScaleSettings scaleSettings, GameBoardSettings gameboardSettings)
        {
            float scaleToUWRLD_UGBD = scaleSettings.GetScaleToUWRLD_UGBD(gameboardSettings.gameBoardScale);

            Vector3 pos_UWRLD = gameboardSettings.currentGameBoard.rotation *
                (scaleToUWRLD_UGBD * pose_GameboardSpace.position) + gameboardSettings.gameBoardCenter;

            Quaternion rotToUWRLD_OBJ = GameboardToWorldSpace(pose_GameboardSpace.rotation, gameboardSettings);

            return new Pose(pos_UWRLD, rotToUWRLD_OBJ);
        }

        protected static Vector3 GameboardToWorldSpace(Vector3 pos_UGBD,
            ScaleSettings scaleSettings, GameBoardSettings gameboardSettings)
        {
            float scaleToUWRLD_UGBD = scaleSettings.GetScaleToUWRLD_UGBD(gameboardSettings.gameBoardScale);

            return gameboardSettings.currentGameBoard.rotation *
                (scaleToUWRLD_UGBD * pos_UGBD) + gameboardSettings.gameBoardCenter;
        }

        protected static Vector3 WorldToGameboardSpace(Vector3 pos_UWRLD,
            ScaleSettings scaleSettings, GameBoardSettings gameboardSettings)
        {
            float scaleToUWRLD_UGBD = scaleSettings.GetScaleToUWRLD_UGBD(gameboardSettings.gameBoardScale);
            var rotToUWRLD_UGBD = gameboardSettings.currentGameBoard.rotation;
            var pos_UGBD = pos_UWRLD - gameboardSettings.gameBoardCenter;
            pos_UGBD = Quaternion.Inverse(rotToUWRLD_UGBD) * pos_UGBD;
            pos_UGBD /= scaleToUWRLD_UGBD;

            return pos_UGBD;
        }

        protected static Quaternion GameboardToWorldSpace(Quaternion rotToUGBD_OBJ, GameBoardSettings gameboardSettings)
        {
            var rotToUWRLD_UGBD = gameboardSettings.currentGameBoard.rotation;
            var rotToUWRLD_OBJ = rotToUWRLD_UGBD * rotToUGBD_OBJ;

            return rotToUWRLD_OBJ;
        }

        protected static Quaternion WorldToGameboardSpace(Quaternion rotToUWRLD_OBJ, GameBoardSettings gameboardSettings)
        {
            var rotToUWRLD_UGBD = gameboardSettings.currentGameBoard.rotation;
            var rotToUGBD_UWRLD = Quaternion.Inverse(rotToUWRLD_UGBD);
            var rotToUGBD_OBJ = rotToUGBD_UWRLD * rotToUWRLD_OBJ;

            return rotToUGBD_OBJ;

        }

        #endregion Protected Functions


        #region Abstract Functions

        /// <summary>
        /// Gets the default pose of the tracked object.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected abstract void SetDefaultPoseGameboardSpace(TSettings settings);

        /// <summary>
        /// Sets the pose values of the tracked object in Unity World Space
        /// </summary>
        /// <param name="state"></param>
        /// <param name="settings"></param>
        /// <param name="scaleSettings"></param>
        /// <param name="gameboardSettings"></param>
        protected abstract void SetPoseGameboardSpace(in TState state, TSettings settings, ScaleSettings scaleSettings, GameBoardSettings gameboardSettings);

        /// <summary>
        /// Sets the pose values of the tracked object in Unity World Space when we already know the pose is invalid.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="settings"></param>
        /// <param name="scaleSettings"></param>
        /// <param name="gameboardSettings"></param>
        protected abstract void SetInvalidPoseGameboardSpace(in TState state, TSettings settings, ScaleSettings scaleSettings, GameBoardSettings gameboardSettings);

        /// <summary>
        /// Sets the pose values of the tracked object in Unity World Space
        /// </summary>
        /// <param name="scaleSettings"></param>
        /// <param name="gameboardSettings"></param>
        protected abstract void SetPoseUnityWorldSpace(ScaleSettings scaleSettings, GameBoardSettings gameboardSettings);

        /// <summary>
        /// Determines whether the tracked object is still connected.
        /// </summary>
        /// <param name="connected"></param>
        /// <returns></returns>
        protected abstract bool TryCheckConnected(out bool connected);

        /// <summary>
        /// Gets the latest pose for the tracked object from the native plugin.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="poseIsValid"></param>
        /// <param name="gameboardSettings"></param>
        /// <returns></returns>
        protected abstract bool TryGetStateFromPlugin(out TState state, out bool poseIsValid, GameBoardSettings gameboardSettings);

        /// <summary>
        /// Sets the pose of the object(s) being driven by TrackableCore.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="scaleSettings"></param>
        /// <param name="gameboardSettings"></param>
        protected abstract void SetDrivenObjectTransform(TSettings settings, ScaleSettings scaleSettings, GameBoardSettings gameboardSettings);

        #endregion Abstract Functions
    }
}
