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
	public class Singleton<T> where T : new()
	{
		private static readonly T instance = new T();

		// Explicit static constructor to tell C# compiler
		// not to mark type as beforefieldinit
		static Singleton()
		{
		}

		protected Singleton()
		{
		}

		protected static T Instance
		{
			get
			{
				return instance;
			}
		}
	}

	public class SingletonComponent<T> : MonoBehaviour where T : UnityEngine.MonoBehaviour
	{
		private static T s_Instance = null;
		public static T Instance
		{
			get
			{
				if( s_Instance != null )
				{
					return s_Instance;
				}

				T[] instances = Resources.FindObjectsOfTypeAll<T>();
				if( instances != null )
				{

					// find the one that is actually in the scene (and not the editor)
					for( int i = 0; i < instances.Length; ++i )
					{
						T instance = instances[ i ];
						if( instance == null )
						{
							continue;
						}

						if( instance.hideFlags != HideFlags.None )
						{
							continue;
						}

						// avoid selecting component attached to a prefab asset that isn't in scene
						if( string.IsNullOrEmpty(instance.gameObject.scene.name) )
						{
							continue;
						}

						s_Instance = instance;

						//We can't use DontDestroyOnLoad on objects that aren't root objects. (i.e. objects that have a parent)
						if(s_Instance.gameObject.transform.parent == null && Application.isPlaying)
						{
							DontDestroyOnLoad( s_Instance );
						}
						break;
					}
				}


				if(s_Instance == null
					// Don't automatically generate a new instance if the application isn't playing
					// (i.e. we're in the editor with the game stopped)
					&& Application.isPlaying)
				{
					string name = string.Format( "__{0}__", typeof( T ).FullName );
					GameObject singletonGo = new GameObject( name );
					s_Instance = singletonGo.AddComponent<T>();
					if(s_Instance.gameObject.transform.parent == null){
						DontDestroyOnLoad( s_Instance );
					}
				}

				return s_Instance;
			}
		}

		internal static bool IsInstantiated => s_Instance != null;

        protected virtual void Awake()
        {
			if(s_Instance == null)
            {
				s_Instance = Instance;
            }
        }
    }
}
