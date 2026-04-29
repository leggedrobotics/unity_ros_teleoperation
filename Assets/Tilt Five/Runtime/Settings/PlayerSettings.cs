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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TiltFive;

namespace TiltFive
{
    [System.Serializable]
    public class PlayerSettings
    {
        #region Sub-settings

        public GlassesSettings glassesSettings = new GlassesSettings();

        public ScaleSettings scaleSettings = new ScaleSettings();

        public GameBoardSettings gameboardSettings = new GameBoardSettings();

        public WandSettings leftWandSettings = new WandSettings();
        public WandSettings rightWandSettings = new WandSettings();

        #endregion


        #region Public Properties

        public PlayerIndex PlayerIndex;

        public static uint MAX_SUPPORTED_PLAYERS => GlassesSettings.MAX_SUPPORTED_GLASSES_COUNT;

        #endregion


        #region Public Functions

        public void Validate()
        {
            rightWandSettings.controllerIndex = ControllerIndex.Right;
            leftWandSettings.controllerIndex = ControllerIndex.Left;
        }

        #endregion
    }
}