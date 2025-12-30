using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Vectorier.EditorScript.Tools
{
    public class MoveVisualizer : EditorWindow
    {
        // ================= ENUMS ================= //

        enum PlacementMode
        {
            Movement,
            Tricks,
            Custom
        }

        enum MovementType
        {
            Jump,
            JumpOff,
            JumpOffFly,
            Run,
            RunFast,
            RunFastJump,
            RunFastJumpOff,
            RunFastLandingFall,
            Slide,
            SlideOff,
            FastSlide,
            FastSlideOff,
            DivingKongToFly,
            SpeedVaultToFly,
            CollisionToFly,
            FlyCollision
        }

        public enum TrickType
        {
            Wall_Hop_360
        }

        // ================= CONSTANT DATA ================= //

        const string MOVEMENT_BASE_PATH = "Assets/Editor/Tools/MoveVisualizer/Movement";
        const string TRICKS_BASE_PATH = "Assets/Editor/Tools/MoveVisualizer/Tricks";
        public const string IMAGE_BASE_PATH = "Assets/Editor/Tools/MoveVisualizer/Image";

        const float SOURCE_FPS = 20f;
        string pivotNodeName = "NPivot";

        static readonly Dictionary<MovementType, string> MovementBins = new()
        {
            { MovementType.Jump, "fly.bin" },
            { MovementType.JumpOff, "jump_off.bin" },
            { MovementType.JumpOffFly, "jump_off_fly.bin" },
            { MovementType.Run, "run.bin" },
            { MovementType.RunFast, "run_fast_from_run.bin" },
            { MovementType.RunFastJump, "run_fast_fly.bin" },
            { MovementType.RunFastJumpOff, "run_fast_jump_off.bin" },
            { MovementType.RunFastLandingFall, "run_fast_landing_fall.bin" },
            { MovementType.Slide, "slide_simple.bin" },
            { MovementType.SlideOff, "slide_simple_and_fall.bin" },
            { MovementType.FastSlide, "fast_slide_simple.bin" },
            { MovementType.FastSlideOff, "fast_slide_simple_fall.bin" },
            { MovementType.DivingKongToFly, "diving_kong_to_fly.bin" },
            { MovementType.SpeedVaultToFly, "speed_vault_fly.bin" },
            { MovementType.CollisionToFly, "collision_to_fly.bin" },
            { MovementType.FlyCollision, "fly_collision.bin" }
        };

        static Dictionary<string, string> discoveredTrickBins;

        static readonly string[] NODEPOINT_ORDER =
        {
            "NHip_1","NHip_2","NStomach","NChest","NNeck","NShoulder_1","NShoulder_2",
            "NKnee_1","NKnee_2","NAnkle_1","NAnkle_2","NToe_1","NHeel_1","NToeTip_1",
            "NToeS_1","NHeel_2","NToe_2","NToeTip_2","NToeS_2","NElbow_1","NElbow_2",
            "NWrist_1","NWrist_2","NKnuckles_1","NFingertips_1","NKnucklesS_1",
            "NKnuckles_2","NFingertips_2","NKnucklesS_2","NHead","NTop","NChestS_1",
            "NChestS_2","NStomachS_1","NStomachS_2","NChestF","NStomachF",
            "NPelvisF","NHeadS_1","NHeadS_2","NHeadF","NPivot",
            "DetectorH","DetectorV","COM","Camera"
        };

        static readonly (string, string)[] CONNECTIONS =
        {
            ("NStomach","NHip_2"),("NStomach","NHip_1"),("NHip_2","NHip_1"),
            ("NChest","NStomach"),("NNeck","NChest"),
            ("NShoulder_1","NNeck"),("NShoulder_2","NNeck"),
            ("NKnee_1","NHip_1"),("NKnee_2","NHip_2"),
            ("NAnkle_1","NKnee_1"),("NAnkle_2","NKnee_2"),
            ("NToe_1","NAnkle_1"),("NHeel_1","NAnkle_1"),("NHeel_1","NToe_1"),
            ("NToe_1","NToeTip_1"),("NToe_1","NToeS_1"),
            ("NToeTip_1","NToeS_1"),("NHeel_1","NToeS_1"),("NAnkle_1","NToeS_1"),
            ("NHeel_2","NAnkle_2"),("NToe_2","NAnkle_2"),("NToe_2","NHeel_2"),
            ("NToeTip_2","NToe_2"),("NToe_2","NToeS_2"),
            ("NToeTip_2","NToeS_2"),("NToeS_2","NHeel_2"),("NToeS_2","NAnkle_2"),
            ("NElbow_1","NShoulder_1"),("NWrist_1","NElbow_1"),
            ("NKnuckles_1","NWrist_1"),("NFingertips_1","NKnuckles_1"),
            ("NKnuckles_1","NKnucklesS_1"),("NKnucklesS_1","NWrist_1"),
            ("NFingertips_1","NKnucklesS_1"),
            ("NElbow_2","NShoulder_2"),("NWrist_2","NElbow_2"),
            ("NKnuckles_2","NWrist_2"),("NFingertips_2","NKnuckles_2"),
            ("NKnucklesS_2","NKnuckles_2"),("NKnucklesS_2","NWrist_2"),
            ("NFingertips_2","NKnucklesS_2"),
            ("NNeck","NHead"),("NHead","NTop"),
            ("NChest","NChestS_1"),("NChestS_2","NChest"),
            ("NStomach","NStomachS_1"),("NStomach","NStomachS_2"),
            ("NNeck","NChestS_1"),("NChestS_2","NNeck"),
            ("NStomachS_1","NChest"),("NStomachS_2","NChest"),
            ("NChestS_2","NChestS_1"),("NStomachS_2","NStomachS_1"),
            ("NChestS_1","NChestF"),("NChestS_2","NChestF"),
            ("NStomachF","NStomachS_1"),("NStomachF","NStomachS_2"),
            ("NChestF","NNeck"),("NStomachF","NChest"),
            ("NChest","NChestF"),("NStomach","NStomachF"),
            ("NPelvisF","NHip_1"),("NHip_2","NPelvisF"),("NStomach","NPelvisF"),
            ("NHead","NHeadS_1"),("NHeadS_2","NHead"),("NTop","NHeadS_1"),
            ("NHeadS_2","NTop"),("NHeadS_1","NHeadS_2"),
            ("NHeadF","NHead"),("NHeadF","NHeadS_1"),
            ("NHeadS_2","NHeadF"),("NHeadF","NTop"),
            ("NStomach","NPivot"),("NPelvisF","NPivot"),
            ("NHip_2","NPivot"),("NHip_1","NPivot")
        };

        // ================= STATE ================= //

        PlacementMode currentPlacementMode = PlacementMode.Movement;
        MovementType currentMovementType;
        string currentTrickIdentifier;

        string binFolderPath;
        string binFileName;
        bool placementEnabled;

        readonly Dictionary<string, Vector3> animationNodes = new();
        readonly Dictionary<string, Vector3> previewNodes = new();
        readonly Dictionary<string, Vector3> previewPose = new();

        readonly List<Vector3[]> animationFrames = new();
        readonly List<Vector3> centerOfMassPath = new();

        int currentFrameIndex;
        int centerOfMassNodeIndex;

        double lastPlaybackTime;
        Vector3 startOffset;
        bool isOffsetInitialized;
        bool isPreviewActive;

        bool isPlaying = true;

        // ================= WINDOW ================= //

        [MenuItem("Vectorier/Tools/Move Visualizer", false, 34)]
        static void OpenWindow() => GetWindow<MoveVisualizer>("Move Visualizer");

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += UpdatePlayback;
            InitializePreviewPose();
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.update -= UpdatePlayback;
            ResetState();
        }

        // ================= UI ================= //

        void OnGUI()
        {
            GUILayout.Space(5);
            currentPlacementMode = (PlacementMode)EditorGUILayout.EnumPopup("Placement Mode", currentPlacementMode);

            switch (currentPlacementMode)
            {
                case PlacementMode.Custom:
                    binFolderPath = EditorGUILayout.TextField("Bin Folder", binFolderPath);
                    binFileName = EditorGUILayout.TextField("Bin Name", binFileName);
                    break;

                case PlacementMode.Movement:
                    currentMovementType = (MovementType)EditorGUILayout.EnumPopup("Movement Type", currentMovementType);
                    binFolderPath = MOVEMENT_BASE_PATH;
                    binFileName = MovementBins[currentMovementType];
                    break;

                case PlacementMode.Tricks:
                    DrawTrickSelectionUI();
                    break;
            }

            pivotNodeName = EditorGUILayout.TextField("Pivot Node", pivotNodeName);

            GUILayout.Space(12);

            if (GUILayout.Button(placementEnabled ? "Stop Placement" : "Start Placement", GUILayout.Height(60)))
                placementEnabled = !placementEnabled;

            if (GUILayout.Button("Clear", GUILayout.Height(40)))
                ResetState();

            GUILayout.Space(10);

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(isPlaying ? "Pause" : "Play", GUILayout.Height(30)))
                    isPlaying = !isPlaying;

                if (GUILayout.Button("Restart", GUILayout.Height(30)))
                {
                    currentFrameIndex = 0;
                    ApplyFrame(currentFrameIndex);
                }
            }

            GUI.enabled = animationFrames.Count > 0;

            int maxFrame = Mathf.Max(0, animationFrames.Count - 1);
            int newFrame = EditorGUILayout.IntSlider("Frame", currentFrameIndex, 0, maxFrame);

            if (newFrame != currentFrameIndex)
            {
                isPlaying = false;
                currentFrameIndex = newFrame;
                ApplyFrame(currentFrameIndex);
            }

            GUI.enabled = true;
        }

        void DrawTrickSelectionUI()
        {
            binFolderPath = TRICKS_BASE_PATH;

            if (discoveredTrickBins == null)
                DiscoverTricks(binFolderPath);

            if (string.IsNullOrEmpty(currentTrickIdentifier))
                currentTrickIdentifier = discoveredTrickBins.Keys.First();

            EditorGUILayout.LabelField("Trick", currentTrickIdentifier);

            if (GUILayout.Button("Select Trick...", GUILayout.Height(30)))
            {
                TrickSelectionWindow.Open(discoveredTrickBins, currentTrickIdentifier, identifier => {currentTrickIdentifier = identifier; binFileName = discoveredTrickBins[identifier];});
            }

            binFileName = discoveredTrickBins[currentTrickIdentifier];
        }

        // ================= SCENE ================= //

        void OnSceneGUI(SceneView sceneView)
        {
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            if (placementEnabled)
                HandlePlacementInput();

            DrawNodes();
            DrawConnections();
            DrawCenterOfMassPath();
        }

        void HandlePlacementInput()
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Vector3 worldPosition = ray.origin + ray.direction * 10f;
            worldPosition.z = 0f;

            UpdatePreview(worldPosition);
            isPreviewActive = true;

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                PlaceAt(worldPosition);
                isPreviewActive = false;
                Event.current.Use();
            }

            SceneView.RepaintAll();
        }

        // ================= CORE ================= //

        void PlaceAt(Vector3 worldPosition)
        {
            ResetState();
            LoadBinaryAnimation();
            InitializeAnimationNodes();

            centerOfMassNodeIndex = Array.IndexOf(NODEPOINT_ORDER, "COM");

            int pivotNodeIndex = -1;

            if (!string.IsNullOrWhiteSpace(pivotNodeName))
                pivotNodeIndex = Array.IndexOf(NODEPOINT_ORDER, pivotNodeName);

            if (pivotNodeIndex < 0)
                pivotNodeIndex = Array.IndexOf(NODEPOINT_ORDER, "NPivot");

            if (pivotNodeIndex < 0)
                throw new Exception("NPivot node not found in NODEPOINT_ORDER");

            Vector3 pivotFrameZero = ConvertAxis(animationFrames[0][pivotNodeIndex]);

            startOffset = worldPosition - pivotFrameZero;
            isOffsetInitialized = true;

            PrecomputeCenterOfMassPath();

            currentFrameIndex = 0;
            isPlaying = true;
            ApplyFrame(0);

            lastPlaybackTime = EditorApplication.timeSinceStartup;
        }

        void LoadBinaryAnimation()
        {
            animationFrames.Clear();
            using BinaryReader reader = new(File.OpenRead(Path.Combine(binFolderPath, binFileName)));

            int frameCount = reader.ReadInt32();

            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                reader.ReadByte();
                int nodeCount = reader.ReadInt32();

                if (nodeCount != NODEPOINT_ORDER.Length)
                    throw new Exception("Node count mismatch");

                Vector3[] frame = new Vector3[nodeCount];

                for (int nodeIndex = 0; nodeIndex < nodeCount; nodeIndex++)
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    float z = reader.ReadSingle();
                    frame[nodeIndex] = new Vector3(x, -z, -y);
                }

                animationFrames.Add(frame);
            }
        }

        void InitializeAnimationNodes()
        {
            animationNodes.Clear();
            foreach (string nodeName in NODEPOINT_ORDER)
                animationNodes[nodeName] = Vector3.zero;
        }

        void UpdatePlayback()
        {
            if (!isOffsetInitialized || animationFrames.Count == 0 || !isPlaying)
                return;

            if (EditorApplication.timeSinceStartup - lastPlaybackTime < 1f / SOURCE_FPS)
                return;

            lastPlaybackTime = EditorApplication.timeSinceStartup;
            currentFrameIndex = (currentFrameIndex + 1) % animationFrames.Count;

            ApplyFrame(currentFrameIndex);

            Repaint();
            SceneView.RepaintAll();
        }

        void ApplyFrame(int frameIndex)
        {
            Vector3[] frame = animationFrames[frameIndex];

            for (int i = 0; i < NODEPOINT_ORDER.Length; i++)
            {
                Vector3 localPosition = ConvertAxis(frame[i]);
                animationNodes[NODEPOINT_ORDER[i]] = localPosition + startOffset;
            }

            Repaint();
            SceneView.RepaintAll();
        }

        void UpdatePreview(Vector3 cursorWorldPosition)
        {
            if (!previewPose.ContainsKey(pivotNodeName))
                return;

            Vector3 pivotLocalPosition = previewPose[pivotNodeName];
            Vector3 offset = cursorWorldPosition - pivotLocalPosition;

            previewNodes.Clear();
            foreach (var entry in previewPose)
                previewNodes[entry.Key] = entry.Value + offset;
        }

        void PrecomputeCenterOfMassPath()
        {
            centerOfMassPath.Clear();
            foreach (var frame in animationFrames)
            {
                Vector3 localCenterOfMass = ConvertAxis(frame[centerOfMassNodeIndex]);
                centerOfMassPath.Add(localCenterOfMass + startOffset);
            }
        }

        Vector3 ConvertAxis(Vector3 value) => new(value.x, -value.z, value.y);

        void ResetState()
        {
            animationNodes.Clear();
            previewNodes.Clear();
            animationFrames.Clear();
            centerOfMassPath.Clear();

            currentFrameIndex = 0;
            isOffsetInitialized = false;
            isPreviewActive = false;
        }

        // ================= DRAWING ================= //

        void DrawNodes()
        {
            if (isPreviewActive)
            {
                Handles.color = new Color(1f, 0f, 0f, 0.4f);
                foreach (var position in previewNodes.Values)
                    Handles.DotHandleCap(0, position, Quaternion.identity, 3f, EventType.Repaint);
            }

            Handles.color = Color.red;
            foreach (var position in animationNodes.Values)
                Handles.DotHandleCap(0, position, Quaternion.identity, 3f, EventType.Repaint);
        }

        void DrawCenterOfMassPath()
        {
            if (centerOfMassPath.Count < 2)
                return;

            Handles.color = Color.red;
            Handles.DrawAAPolyLine(3f, centerOfMassPath.ToArray());
        }

        void DrawConnections()
        {
            if (isPreviewActive)
            {
                Handles.color = new Color(0f, 1f, 0f, 0.35f);
                DrawConnectionSet(previewNodes);
            }

            Handles.color = Color.green;
            DrawConnectionSet(animationNodes);
        }

        void DrawConnectionSet(Dictionary<string, Vector3> nodeSet)
        {
            foreach (var (first, second) in CONNECTIONS)
                if (nodeSet.ContainsKey(first) && nodeSet.ContainsKey(second))
                    Handles.DrawLine(nodeSet[first], nodeSet[second]);
        }

        // ================= PREVIEW POSE ================= //

        void InitializePreviewPose()
        {
            previewPose.Clear();
            void Set(string name, float x, float y, float z)
                => previewPose[name] = ConvertAxis(new Vector3(x, -y, -z));

            Set("NHip_1", -19.577221f, -8.585417f, 84.134026f);
            Set("NHip_2", -14.560858f, 8.122724f, 82.953896f);
            Set("NStomach", -9.392555f, -1.314231f, 99.322510f);
            Set("NChest", -0.985765f, -2.248583f, 114.576309f);
            Set("NNeck", 8.144234f, 0.799165f, 129.391342f);
            Set("NShoulder_1", 12.551178f, -15.790263f, 128.772690f);
            Set("NShoulder_2", 6.237663f, 17.609047f, 134.210663f);
            Set("NKnee_1", -50.975418f, -5.785246f, 45.456955f);
            Set("NKnee_2", 25.844582f, 9.903176f, 58.044540f);
            Set("NAnkle_1", -88.699814f, -3.378675f, 25.798462f);
            Set("NAnkle_2", -13.250669f, 6.528876f, 50.287231f);
            Set("NToe_1", -92.157410f, -0.610153f, 8.867973f);
            Set("NHeel_1", -98.535316f, -3.505732f, 27.600554f);
            Set("NToeTip_1", -87.669655f, 0.365667f, 2.316364f);
            Set("NToeS_1", -92.283051f, -8.508365f, 7.602760f);
            Set("NHeel_2", -22.572092f, 4.031524f, 52.909119f);
            Set("NToe_2", -18.491583f, 7.329743f, 33.609646f);
            Set("NToeTip_2", -16.405792f, 8.739633f, 26.016127f);
            Set("NToeS_2", -20.299704f, 15.065866f, 34.549374f);
            Set("NElbow_1", 23.573296f, -29.794552f, 106.561279f);
            Set("NElbow_2", -22.879360f, 22.530788f, 132.434006f);
            Set("NWrist_1", 47.289154f, -12.875849f, 114.187531f);
            Set("NWrist_2", -24.266720f, 36.565292f, 107.673439f);
            Set("NKnuckles_1", 56.037056f, -9.906665f, 115.244850f);
            Set("NFingertips_1", 55.178741f, -1.261982f, 119.921249f);
            Set("NKnucklesS_1", 50.209671f, -10.927604f, 120.081985f);
            Set("NKnuckles_2", -27.603422f, 39.348595f, 98.328270f);
            Set("NFingertips_2", -29.340508f, 31.007551f, 102.793465f);
            Set("NKnucklesS_2", -21.749163f, 35.052471f, 98.640076f);
            Set("NHead", 17.986877f, 0.118644f, 143.801025f);
            Set("NTop", 22.405685f, -0.554002f, 160.715347f);
            Set("NChestS_1", -0.897280f, -8.979884f, 116.031631f);
            Set("NChestS_2", -2.560478f, 8.153231f, 115.891487f);
            Set("NStomachS_1", -9.784924f, -9.954099f, 99.871407f);
            Set("NStomachS_2", -9.076771f, 7.395645f, 99.385635f);
            Set("NChestF", 4.352207f, 0.990542f, 110.784447f);
            Set("NStomachF", -2.270447f, -1.816215f, 95.587852f);
            Set("NPelvisF", -9.550920f, -2.755259f, 79.839340f);
            Set("NHeadS_1", 17.150383f, -8.589858f, 143.676453f);
            Set("NHeadS_2", 18.824371f, 8.826989f, 143.926865f);
            Set("NHeadF", 26.410934f, -0.664772f, 141.568832f);
            Set("NPivot", -17.069702f, -0.231332f, 83.542564f);
            Set("DetectorH", -12f, 0f, 0f);
            Set("DetectorV", 56f, 0f, 100f);
            Set("COM", -8.691902f, 0.692974f, 91.093567f);
            Set("Camera", 150.255203f, -9.272315f, 85f);
        }

        // ================= TRICKS ================= //

        static void DiscoverTricks(string folderPath)
        {
            discoveredTrickBins = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(folderPath))
                return;

            foreach (string filePath in Directory.GetFiles(folderPath, "*.bin"))
            {
                string fileName = Path.GetFileName(filePath);
                string identifier = fileName.Equals("360_wall_hop.bin", StringComparison.OrdinalIgnoreCase) ? nameof(TrickType.Wall_Hop_360) : Path.GetFileNameWithoutExtension(fileName);

                discoveredTrickBins[identifier] = fileName;
            }
        }
    }

    // ================= TRICK SELECTION WINDOW ================= //

    class TrickSelectionWindow : EditorWindow
    {
        private string selectedTrickIdentifier;
        Action<string> onSelected;
        Dictionary<string, string> availableTricks;

        static readonly Vector2 WINDOW_SIZE = new(400, 300);
        Vector2 scrollPosition;

        public static void Open(Dictionary<string, string> tricks, string current, Action<string> onSelected)
        {
            var window = CreateInstance<TrickSelectionWindow>();
            window.availableTricks = tricks;
            window.selectedTrickIdentifier = current;
            window.onSelected = onSelected;
            window.titleContent = new GUIContent("Select Trick");
            window.minSize = WINDOW_SIZE;
            window.ShowUtility();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Available Tricks", EditorStyles.boldLabel);
            GUILayout.Space(8);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, true, true);

            foreach (var pair in availableTricks)
            {
                string identifier = pair.Key;
                string binFile = pair.Value;
                string displayName = FormatTrickName(binFile);

                string imageName = "TRACK_TRICK_" + Path.GetFileNameWithoutExtension(binFile).Replace("_", string.Empty).ToUpperInvariant();

                string imagePath = Path.Combine(MoveVisualizer.IMAGE_BASE_PATH, imageName + ".png");
                Texture2D previewTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);

                using (new GUILayout.HorizontalScope(GUILayout.Height(64)))
                {
                    if (previewTexture != null)
                        GUILayout.Label(previewTexture, GUILayout.Width(64), GUILayout.Height(64));
                    else
                        GUILayout.Box("No Image", GUILayout.Width(64), GUILayout.Height(64));

                    if (GUILayout.Button(displayName, GUILayout.Height(64), GUILayout.MinWidth(200)))
                    {
                        selectedTrickIdentifier = identifier;
                        onSelected?.Invoke(identifier);
                        Close();
                    }
                }

                GUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();
        }

        static string FormatTrickName(string binFileName)
        {
            string[] parts = Path.GetFileNameWithoutExtension(binFileName).Split('_', StringSplitOptions.RemoveEmptyEntries);

            for (int index = 0; index < parts.Length; index++)
                parts[index] = char.ToUpperInvariant(parts[index][0]) + parts[index][1..].ToLowerInvariant();

            return string.Join(" ", parts);
        }
    }
}
