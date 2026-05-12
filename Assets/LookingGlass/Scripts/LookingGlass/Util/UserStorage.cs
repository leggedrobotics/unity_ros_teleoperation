using System.IO;
using UnityEngine;

namespace LookingGlass
{
    public class UserStorage
    {
        private const string JSONFILENAME = "visual.json";
      
        public static string PersistentDataJSONPath =>
            Path.Combine(Application.persistentDataPath, JSONFILENAME);
    }
}