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

namespace TiltFive
{
    public static class UnityExtensions
    {

#if UNITY_2018 || UNITY_2019_1
        // We'd like to use Unity 2019's TryGetComponent while supporting Unity 2018. Let's write some extension methods.
        public static bool TryGetComponent<T>(this GameObject obj, out T target) where T : Component
        {
            target = obj.GetComponent<T>();
            return target != null;
        }

        public static bool TryGetComponent<T>(this MonoBehaviour monoBehaviour, out T target) where T : Component
        {
            return monoBehaviour.gameObject.TryGetComponent<T>(out target);            
        }

        public static bool TryGetComponent<T>(this Transform transform, out T target) where T : Component
        {
            return transform.gameObject.TryGetComponent<T>(out target);            
        }
#endif

    }
}