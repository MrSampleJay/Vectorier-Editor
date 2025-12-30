using System.Collections.Generic;
using UnityEngine;

namespace Vectorier.Core
{
    [DisallowMultipleComponent]
    public class ExportConfig : MonoBehaviour
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
        public string musicName = "music_dinamic";
        public float musicVolume = 0.3f;
        public string commonModeModels =
            @"<Model Name=""Player"" Type=""1"" Color=""0"" BirthSpawn=""DefaultSpawn"" AI=""0"" Time=""0"" Respawns=""Hunter"" ForceBlasts=""Hunter"" Trick=""1"" Item=""1"" Victory=""1"" Lose=""1""/>
<Model Name=""Hunter"" Type=""0"" Color=""0"" BirthSpawn=""DefaultSpawn"" AI=""1"" Time=""1.5"" AllowedSpawns=""Respawn"" Skins=""hunter"" Murders=""Player"" Arrests=""Player"" Icon=""1""/>";

        public string hunterModeModels =
            @"<Model Name=""Player"" Type=""0"" Color=""0"" BirthSpawn=""DefaultSpawn"" AI=""5"" Time=""0"" Victory=""1"" Respawns=""Hunter""/>
<Model Name=""Hunter"" Type=""1"" Color=""0"" BirthSpawn=""DefaultSpawn"" AI=""0"" Time=""1.5"" Trick=""1"" Item=""1"" Skins=""hunter"" Murders=""Player"" Arrests=""Player"" Lose=""1"" AllowedSpawns=""Despawn""/>";

        public int coinValue = 50;
    }
}