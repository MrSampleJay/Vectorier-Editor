using System.Collections.Generic;
using UnityEngine;

namespace Vectorier.Core
{
    public class ImportConfig : ScriptableObject
    {
        public enum ImportType
        {
            Level,
            Objects,
            Buildings
        }

        public string filePathDirectory = "";
        public string xmlName = "";
        public string selectedObject = "";
        public string ignoreTags = "";
        public bool untagChildren = false;
        public bool includeBuildingsMarker = true;
        public bool applyConfig = true;
        public List<string> textureFolders = new List<string>();
    }
}
