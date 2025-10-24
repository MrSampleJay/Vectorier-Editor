using System.Collections.Generic;
using UnityEngine;

namespace Vectorier.Core
{
    [System.Serializable]
    public class BuildmapConfig : ScriptableObject
    {
        public enum ExportType { Level, Objects, Buildings }

        public ExportType exportType = ExportType.Level;

        // Common
        public string filePathDirectory = "";
        public string fileName = "";
        public bool fastBuild = false;
        public bool exportAsXML = false;

        // Sets
        public List<string> citySets = new List<string>();
        public List<string> groundSets = new List<string>();
        public List<string> librarySets = new List<string>();

        // Level-only
        public string musicName = "";
        public float musicVolume = 0.3f;
        public string commonModeModels = "";
        public string hunterModeModels = "";
        public int coinValue = 0;
    }
}
