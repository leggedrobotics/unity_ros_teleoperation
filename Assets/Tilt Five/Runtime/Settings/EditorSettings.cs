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
#if UNITY_EDITOR

namespace TiltFive
{
    /// <summary>
    /// Keeps track of the Tilt Five Manager's most recently opened panel.
    /// </summary>
    [System.Serializable]
    public class EditorSettings
    {
        public enum PanelView {
            GlassesConfig,
            ScaleConfig,
            GameBoardConfig,
            WandConfig,
            LogConfig,
            SpectatorConfig,
        };

        public PanelView activePanel = PanelView.GlassesConfig;
    }
}

#endif