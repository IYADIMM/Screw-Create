using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScrewAndCreate.DisassemblyPresentation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ScrewAndCreate.Editor.Disassembly
{
    public static class DisassemblyT01SceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Gameplay_Disassembly_T01.unity";
        const string ArtRoot = "Assets/_Game/Art/DisassemblyT01";
        const string MeshRoot = ArtRoot + "/Meshes";
        const string MaterialRoot = ArtRoot + "/Materials";
        const string TextureRoot = ArtRoot + "/Textures";
        const string PrefabRoot = "Assets/_Game/Prefabs/Disassembly";

        static readonly Color Walnut = new Color(0.23f, 0.09f, 0.035f);
        static readonly Color Honey = new Color(0.72f, 0.34f, 0.09f);
        static readonly Color Maple = new Color(0.88f, 0.59f, 0.25f);
        static readonly Color Brass = new Color(0.88f, 0.55f, 0.12f);
        static readonly Color Teal = new Color(0.04f, 0.72f, 0.68f);
        static readonly Color Cream = new Color(0.98f, 0.91f, 0.78f);

        [MenuItem("Tools/Screw & Create/Build/Build Disassembly T01")]
        public static void Build()
        {
            EnsureFolders();
            var meshes = CreateMeshes();
            var materials = CreateMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("DisassemblyT01Root");
            var controller = root.AddComponent<DisassemblyT01Controller>();
            var boardRoot = new GameObject("BoardPresentation"); boardRoot.transform.SetParent(root.transform);
            var cameraRoot = new GameObject("CameraAndLighting"); cameraRoot.transform.SetParent(root.transform);
            var hudRoot = new GameObject("HUD"); hudRoot.transform.SetParent(root.transform);
            new GameObject("InputSystemInteraction").transform.SetParent(root.transform);
            new GameObject("AudioHooks").transform.SetParent(root.transform);

            BuildBackdrop(boardRoot.transform, meshes, materials);
            var plank = BuildPlank(boardRoot.transform, meshes, materials);
            var source = BuildHole("H_C1", new Vector3(-1.35f, 1.05f, -0.16f), boardRoot.transform, meshes, materials);
            var destination = BuildHole("H_P1", new Vector3(1.35f, -1.72f, 0.08f), boardRoot.transform, meshes, materials);
            var screw = BuildScrew(source.ScrewAnchor, boardRoot.transform, meshes, materials);
            var releasedPose = new GameObject("ReleasedPose_C_PLANK").transform;
            releasedPose.SetParent(boardRoot.transform); releasedPose.position = new Vector3(0.25f, -3.32f, -0.05f); releasedPose.rotation = Quaternion.Euler(0f, 0f, -12f);
            plank.ReleasedPose = releasedPose;

            var camera = BuildCamera(cameraRoot.transform);
            BuildLights(cameraRoot.transform);
            BuildUi(hudRoot.transform, controller, out var level, out var progress, out var instruction, out var status, out var pause, out var restart, out var completion);
            BuildEventSystem(root.transform);

            controller.GameplayCamera = camera;
            controller.CameraFitter = camera.GetComponent<DisassemblyCameraFitter>();
            controller.BoardRoot = boardRoot.transform;
            controller.Screws = new[] { screw };
            controller.Holes = new[] { source, destination };
            controller.Components = new[] { plank };
            controller.LevelText = level;
            controller.ProgressText = progress;
            controller.InstructionText = instruction;
            controller.StatusText = status;
            controller.PausePanel = pause;
            controller.RestartPanel = restart;
            controller.CompletionPanel = completion;
            controller.HoleIdleMaterial = materials["HoleIdle"];
            controller.HoleLegalMaterial = materials["HoleLegal"];
            controller.ScrewIdleMaterial = materials["Brass"];
            controller.ScrewSelectedMaterial = materials["ScrewSelected"];
            controller.InvalidMaterial = materials["Invalid"];

            PrefabUtility.SaveAsPrefabAsset(boardRoot, PrefabRoot + "/T01_Board.prefab");
            controller.Screws = root.GetComponentsInChildren<DisassemblyScrewView>(true);
            controller.Holes = root.GetComponentsInChildren<DisassemblyHoleView>(true).OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            controller.Components = root.GetComponentsInChildren<DisassemblyComponentView>(true);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/_Game/Scenes/Gameplay_Windmill_Graybox.unity", true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("M2_T01_SCENE_BUILT scene=" + ScenePath);
        }

        public static void BuildFromCommandLine()
        {
            Build();
            EditorApplication.Exit(0);
        }

        public static void BuildAndroidDevelopmentFromCommandLine()
        {
            Build();
            PlayerSettings.productName = "Screw & Create";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.iyadimm.screwandcreate");
            PlayerSettings.bundleVersion = "0.2.0-m2";
            PlayerSettings.Android.bundleVersionCode = 2;
            var output = Path.GetFullPath("Builds/M2/ScrewAndCreate-M2-T01.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            Debug.Log($"M2_ANDROID_BUILD result={report.summary.result} bytes={report.summary.totalSize} output={output}");
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        static Dictionary<string, Mesh> CreateMeshes()
        {
            var result = new Dictionary<string, Mesh>
            {
                ["ChamferBox"] = SaveMesh("ChamferBox", CreateChamferedBox(1f, 1f, 1f, 0.12f)),
                ["Screw"] = SaveMesh("MechanicalScrew", CreateLathe(new[]
                {
                    new Vector2(0.30f,-0.36f), new Vector2(0.37f,-0.29f), new Vector2(0.38f,-0.15f),
                    new Vector2(0.18f,-0.08f), new Vector2(0.16f,0.02f), new Vector2(0.19f,0.09f),
                    new Vector2(0.16f,0.16f), new Vector2(0.19f,0.23f), new Vector2(0.16f,0.30f),
                    new Vector2(0.19f,0.37f), new Vector2(0.14f,0.56f), new Vector2(0f,0.75f)
                }, 32)),
                ["Torus"] = SaveMesh("HoleRing", CreateTorus(0.38f, 0.075f, 36, 10)),
                ["Disc"] = SaveMesh("HoleDisc", CreateCylinder(0.29f, 0.06f, 32))
            };
            return result;
        }

        static Dictionary<string, Material> CreateMaterials()
        {
            var woodTexture = CreateWoodTexture();
            return new Dictionary<string, Material>
            {
                ["Backdrop"] = Material("Backdrop", Walnut * 0.82f, 0f, 0.22f),
                ["Board"] = Material("Board", Honey * 0.88f, 0f, 0.34f, woodTexture),
                ["Frame"] = Material("Frame", Walnut, 0f, 0.3f, woodTexture),
                ["Plank"] = Material("Plank", Maple, 0f, 0.3f, woodTexture),
                ["PlankEdge"] = Material("PlankEdge", Honey, 0f, 0.28f, woodTexture),
                ["Brass"] = Material("Brass", Brass, 0.82f, 0.72f),
                ["Slot"] = Material("Slot", new Color(0.08f,0.045f,0.02f), 0.2f, 0.42f),
                ["HoleIdle"] = Material("HoleIdle", new Color(0.16f,0.08f,0.035f), 0.05f, 0.24f),
                ["HoleLegal"] = Material("HoleLegal", Teal, 0.25f, 0.58f, null, Teal * 1.8f),
                ["ScrewSelected"] = Material("ScrewSelected", new Color(1f,0.76f,0.18f), 0.78f, 0.78f, null, new Color(0.3f,0.12f,0.01f)),
                ["Invalid"] = Material("Invalid", new Color(0.85f,0.12f,0.08f), 0.1f, 0.35f, null, new Color(0.5f,0.02f,0.01f))
            };
        }

        static void BuildBackdrop(Transform parent, Dictionary<string, Mesh> mesh, Dictionary<string, Material> mat)
        {
            MeshObject("WorkshopBackdrop", parent, new Vector3(0f,0.25f,2.1f), new Vector3(9.5f,13.5f,0.6f), Quaternion.identity, mesh["ChamferBox"], mat["Backdrop"]);
            MeshObject("PersistentBaseboard", parent, new Vector3(0f,0.35f,0.75f), new Vector3(6.2f,8.5f,0.55f), Quaternion.identity, mesh["ChamferBox"], mat["Board"]);
            MeshObject("FrameTop", parent, new Vector3(0f,4.25f,0.35f), new Vector3(6.35f,0.28f,0.42f), Quaternion.identity, mesh["ChamferBox"], mat["Frame"]);
            MeshObject("FrameBottom", parent, new Vector3(0f,-3.55f,0.35f), new Vector3(6.35f,0.28f,0.42f), Quaternion.identity, mesh["ChamferBox"], mat["Frame"]);
            MeshObject("FrameLeft", parent, new Vector3(-2.95f,0.35f,0.35f), new Vector3(0.28f,7.55f,0.42f), Quaternion.identity, mesh["ChamferBox"], mat["Frame"]);
            MeshObject("FrameRight", parent, new Vector3(2.95f,0.35f,0.35f), new Vector3(0.28f,7.55f,0.42f), Quaternion.identity, mesh["ChamferBox"], mat["Frame"]);
            for (var i = -2; i <= 2; i++)
                MeshObject("BoardGrainRail_" + i, parent, new Vector3(i * 1.05f,0.35f,0.41f), new Vector3(0.025f,7.1f,0.03f), Quaternion.identity, mesh["ChamferBox"], mat["Frame"]);
        }

        static DisassemblyComponentView BuildPlank(Transform parent, Dictionary<string, Mesh> mesh, Dictionary<string, Material> mat)
        {
            var root = new GameObject("C_PLANK"); root.transform.SetParent(parent); root.transform.position = new Vector3(0f,1.05f,-0.06f);
            var collider = root.AddComponent<BoxCollider>(); collider.size = new Vector3(4.7f,1.5f,0.48f);
            var view = root.AddComponent<DisassemblyComponentView>(); view.Id = "C_PLANK";
            MeshObject("BeveledWoodPlank", root.transform, Vector3.zero, new Vector3(4.7f,1.5f,0.48f), Quaternion.identity, mesh["ChamferBox"], mat["Plank"]);
            MeshObject("TopAccent", root.transform, new Vector3(0f,0.53f,-0.27f), new Vector3(4.15f,0.11f,0.08f), Quaternion.identity, mesh["ChamferBox"], mat["PlankEdge"]);
            MeshObject("BottomAccent", root.transform, new Vector3(0f,-0.53f,-0.27f), new Vector3(4.15f,0.11f,0.08f), Quaternion.identity, mesh["ChamferBox"], mat["PlankEdge"]);
            for (var i = -2; i <= 2; i++)
                MeshObject("Grain_" + i, root.transform, new Vector3(i * 0.72f,0f,-0.265f), new Vector3(0.018f,0.82f,0.05f), Quaternion.Euler(0f,0f,i * 2f), mesh["ChamferBox"], mat["PlankEdge"]);
            return view;
        }

        static DisassemblyHoleView BuildHole(string id, Vector3 position, Transform parent, Dictionary<string, Mesh> mesh, Dictionary<string, Material> mat)
        {
            var root = new GameObject(id); root.transform.SetParent(parent); root.transform.position = position; root.transform.rotation = Quaternion.Euler(90f,0f,0f);
            root.layer = LayerMask.NameToLayer("GameplayInteraction");
            var collider = root.AddComponent<CapsuleCollider>(); collider.direction = 1; collider.radius = 0.47f; collider.height = 0.2f;
            var disc = MeshObject("Recess", root.transform, Vector3.zero, Vector3.one, Quaternion.identity, mesh["Disc"], mat["Slot"]);
            var ring = MeshObject("Ring", root.transform, new Vector3(0f,-0.04f,0f), Vector3.one, Quaternion.identity, mesh["Torus"], mat["HoleIdle"]);
            var anchor = new GameObject("ScrewAnchor").transform; anchor.SetParent(root.transform, false);
            var view = root.AddComponent<DisassemblyHoleView>(); view.Id = id; view.ScrewAnchor = anchor; view.RingRenderer = ring.GetComponent<Renderer>();
            return view;
        }

        static DisassemblyScrewView BuildScrew(Transform source, Transform parent, Dictionary<string, Mesh> mesh, Dictionary<string, Material> mat)
        {
            var root = new GameObject("S_1"); root.transform.SetParent(parent); root.transform.SetPositionAndRotation(source.position, source.rotation); root.layer = LayerMask.NameToLayer("GameplayInteraction");
            var screwBody = MeshObject("MechanicalScrew", root.transform, Vector3.zero, Vector3.one, Quaternion.identity, mesh["Screw"], mat["Brass"]);
            var slotA = MeshObject("PhillipsSlotA", root.transform, new Vector3(0f,-0.375f,0f), new Vector3(0.38f,0.035f,0.095f), Quaternion.identity, mesh["ChamferBox"], mat["Slot"]);
            var slotB = MeshObject("PhillipsSlotB", root.transform, new Vector3(0f,-0.375f,0f), new Vector3(0.095f,0.035f,0.38f), Quaternion.identity, mesh["ChamferBox"], mat["Slot"]);
            var collider = root.AddComponent<CapsuleCollider>(); collider.direction = 1; collider.radius = 0.43f; collider.height = 1.15f; collider.center = new Vector3(0f,0.08f,0f);
            var view = root.AddComponent<DisassemblyScrewView>(); view.Id = "S_1"; view.TargetRenderer = screwBody.GetComponent<Renderer>(); view.TargetCollider = collider;
            return view;
        }

        static Camera BuildCamera(Transform parent)
        {
            var go = new GameObject("PortraitCamera"); go.transform.SetParent(parent); go.tag = "MainCamera"; go.transform.position = new Vector3(0f,0.4f,-12f);
            var camera = go.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 6f; camera.nearClipPlane = 0.1f; camera.farClipPlane = 50f; camera.backgroundColor = new Color(0.055f,0.025f,0.015f); camera.clearFlags = CameraClearFlags.SolidColor;
            var fitter = go.AddComponent<DisassemblyCameraFitter>(); fitter.TargetCamera = camera;
            return camera;
        }

        static void BuildLights(Transform parent)
        {
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.52f,0.36f,0.25f);
            var key = new GameObject("WarmKey"); key.transform.SetParent(parent); key.transform.rotation = Quaternion.Euler(28f,-34f,0f); var light = key.AddComponent<Light>(); light.type = LightType.Directional; light.color = new Color(1f,0.86f,0.69f); light.intensity = 1.8f; light.shadows = LightShadows.Soft;
            var fill = new GameObject("CoolFill"); fill.transform.SetParent(parent); fill.transform.position = new Vector3(-3f,2.5f,-4f); var fillLight = fill.AddComponent<Light>(); fillLight.type = LightType.Point; fillLight.color = new Color(0.28f,0.88f,0.82f); fillLight.intensity = 3.4f; fillLight.range = 10f;
            var rim = new GameObject("WarmRim"); rim.transform.SetParent(parent); rim.transform.position = new Vector3(3f,4f,-2f); var rimLight = rim.AddComponent<Light>(); rimLight.type = LightType.Point; rimLight.color = new Color(1f,0.58f,0.28f); rimLight.intensity = 2.6f; rimLight.range = 9f;
        }

        static void BuildUi(Transform parent, DisassemblyT01Controller controller, out Text level, out Text progress, out Text instruction, out Text status, out GameObject pause, out GameObject restart, out GameObject completion)
        {
            var canvasGo = new GameObject("PortraitCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); canvasGo.transform.SetParent(parent); var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080f,1920f); scaler.matchWidthOrHeight = 0.75f;
            var safe = new GameObject("SafeArea", typeof(RectTransform), typeof(DisassemblySafeAreaFitter)); safe.transform.SetParent(canvasGo.transform,false); Stretch(safe.GetComponent<RectTransform>());
            var top = Panel("TopBar", safe.transform, new Vector2(0.035f,0.875f), new Vector2(0.965f,0.975f), new Color(0.12f,0.045f,0.018f,0.96f)); top.GetComponent<Image>().raycastTarget = false;
            level = Label("LevelLabel", top.transform, "LEVEL 1  •  FIRST RELEASE", 40, TextAnchor.MiddleLeft, Cream, new Vector2(0.05f,0.12f), new Vector2(0.72f,0.9f));
            progress = Label("Progress", top.transform, "PARTS  0/1", 30, TextAnchor.MiddleRight, new Color(0.35f,0.94f,0.84f), new Vector2(0.68f,0.12f), new Vector2(0.84f,0.9f));
            Button("PauseButton", top.transform, "Ⅱ", new Vector2(0.865f,0.16f), new Vector2(0.955f,0.84f), Teal, controller.TogglePause);
            instruction = Label("Instruction", safe.transform, "Move the screw to the empty hole.", 38, TextAnchor.MiddleCenter, Cream, new Vector2(0.08f,0.805f), new Vector2(0.92f,0.86f));
            status = Label("Status", safe.transform, "Tap the brass screw.", 27, TextAnchor.MiddleCenter, new Color(0.7f,0.91f,0.87f), new Vector2(0.09f,0.16f), new Vector2(0.91f,0.205f));
            var bottom = Panel("BottomControls", safe.transform, new Vector2(0.035f,0.03f), new Vector2(0.965f,0.145f), new Color(0.12f,0.045f,0.018f,0.97f)); bottom.GetComponent<Image>().raycastTarget = false;
            Button("UndoButton", bottom.transform, "↶  UNDO", new Vector2(0.04f,0.18f), new Vector2(0.34f,0.82f), Teal, controller.Undo);
            Button("RestartButton", bottom.transform, "RESTART", new Vector2(0.66f,0.18f), new Vector2(0.96f,0.82f), Honey, controller.RequestRestart);
            Label("MechanicHint", bottom.transform, "TAP  •  MOVE  •  RELEASE", 22, TextAnchor.MiddleCenter, new Color(0.76f,0.62f,0.43f), new Vector2(0.35f,0.12f), new Vector2(0.65f,0.88f));
            pause = Modal("PausePanel", safe.transform, "PAUSED\n\nYour puzzle is safe.", "RESUME", controller.TogglePause);
            restart = Modal("RestartPanel", safe.transform, "RESTART LEVEL?\n\nYour current move will be cleared.", "RESTART", controller.ConfirmRestart); Button("CancelButton", restart.transform, "CANCEL", new Vector2(0.18f,0.12f), new Vector2(0.45f,0.27f), new Color(0.25f,0.13f,0.07f), controller.CancelRestart);
            completion = Modal("CompletionPanel", safe.transform, "LEVEL COMPLETE\n\nThe plank is free!", "PLAY AGAIN", controller.ConfirmRestart); Button("UndoCompletion", completion.transform, "UNDO", new Vector2(0.18f,0.12f), new Vector2(0.45f,0.27f), Teal, controller.Undo);
            pause.SetActive(false); restart.SetActive(false); completion.SetActive(false);
        }

        static GameObject Modal(string name, Transform parent, string message, string primary, UnityEngine.Events.UnityAction action)
        {
            var go = Panel(name, parent, new Vector2(0.1f,0.3f), new Vector2(0.9f,0.7f), new Color(0.08f,0.025f,0.012f,0.985f));
            Label("Message", go.transform, message, 44, TextAnchor.MiddleCenter, Cream, new Vector2(0.08f,0.3f), new Vector2(0.92f,0.9f));
            Button("PrimaryButton", go.transform, primary, new Vector2(0.55f,0.12f), new Vector2(0.82f,0.27f), Honey, action); return go;
        }

        static void BuildEventSystem(Transform parent)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); go.transform.SetParent(parent); go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        static GameObject Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent,false); Anchors(go.GetComponent<RectTransform>(),min,max); go.GetComponent<Image>().color = color; return go;
        }

        static Text Label(string name, Transform parent, string text, int size, TextAnchor alignment, Color color, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent,false); Anchors(go.GetComponent<RectTransform>(),min,max);
            var label = go.GetComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.text = text; label.fontSize = size; label.alignment = alignment; label.color = color; label.resizeTextForBestFit = true; label.resizeTextMinSize = 16; label.resizeTextMaxSize = size; label.raycastTarget = false; return label;
        }

        static Button Button(string name, Transform parent, string text, Vector2 min, Vector2 max, Color color, UnityEngine.Events.UnityAction action)
        {
            var go = Panel(name,parent,min,max,color); var button = go.AddComponent<Button>(); var colors = button.colors; colors.highlightedColor = color * 1.18f; colors.pressedColor = color * 0.78f; button.colors = colors;
            Label("Text",go.transform,text,28,TextAnchor.MiddleCenter,Color.white,Vector2.zero,Vector2.one); UnityEventTools.AddPersistentListener(button.onClick,action); return button;
        }

        static GameObject MeshObject(string name, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent); go.transform.localPosition = position; go.transform.localRotation = rotation; go.transform.localScale = scale; go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material; return go;
        }

        static Material Material(string name, Color color, float metallic, float smoothness, Texture texture = null, Color? emission = null)
        {
            var path = MaterialRoot + "/" + name + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path); var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material,path); } else material.shader = shader;
            material.SetColor("_BaseColor",color); material.color = color; material.SetFloat("_Metallic",metallic); material.SetFloat("_Smoothness",smoothness);
            if (texture != null) material.SetTexture("_BaseMap",texture);
            if (emission.HasValue) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",emission.Value); } else material.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(material); return material;
        }

        static Texture2D CreateWoodTexture()
        {
            var path = TextureRoot + "/ProceduralWood.png";
            var texture = new Texture2D(256,256,TextureFormat.RGB24,false);
            for (var y = 0; y < 256; y++) for (var x = 0; x < 256; x++)
            {
                var wave = Mathf.Sin((x * 0.14f) + Mathf.Sin(y * 0.045f) * 2.4f) * 0.08f;
                var grain = Mathf.PerlinNoise(x * 0.035f,y * 0.018f) * 0.14f;
                texture.SetPixel(x,y,new Color(0.68f + wave + grain,0.36f + wave * 0.55f + grain * 0.5f,0.12f + grain * 0.28f));
            }
            texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Default; importer.wrapMode = TextureWrapMode.Repeat; importer.filterMode = FilterMode.Bilinear; importer.maxTextureSize = 256; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Mesh SaveMesh(string name, Mesh mesh)
        {
            var path = MeshRoot + "/" + name + ".asset"; var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { mesh.name = name; AssetDatabase.CreateAsset(mesh,path); return mesh; }
            EditorUtility.CopySerialized(mesh,existing); UnityEngine.Object.DestroyImmediate(mesh); EditorUtility.SetDirty(existing); return existing;
        }

        static Mesh CreateChamferedBox(float width, float height, float depth, float bevel)
        {
            var hx=width/2f; var hy=height/2f; var hz=depth/2f;
            var radius=Mathf.Min(bevel,Mathf.Min(hx,Mathf.Min(hy,hz))*0.8f);
            var ix=hx-radius; var iy=hy-radius; var iz=hz-radius;
            var xs=new[]{-hx,-ix,ix,hx}; var ys=new[]{-hy,-iy,iy,hy}; var zs=new[]{-hz,-iz,iz,hz};
            var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var triangles=new List<int>(); var uv=new List<Vector2>();

            Vector3 Rounded(Vector3 point)
            {
                var inner=new Vector3(Mathf.Clamp(point.x,-ix,ix),Mathf.Clamp(point.y,-iy,iy),Mathf.Clamp(point.z,-iz,iz));
                var delta=point-inner;
                return delta.sqrMagnitude<0.000001f ? point : inner+delta.normalized*radius;
            }
            Vector3 RoundedNormal(Vector3 point,Vector3 fallback)
            {
                var inner=new Vector3(Mathf.Clamp(point.x,-ix,ix),Mathf.Clamp(point.y,-iy,iy),Mathf.Clamp(point.z,-iz,iz));
                var delta=point-inner;
                return delta.sqrMagnitude<0.000001f ? fallback : delta.normalized;
            }
            void Cell(Vector3 a,Vector3 b0,Vector3 c,Vector3 d,Vector3 outward,int u,int v)
            {
                var i=vertices.Count;
                foreach(var raw in new[]{a,b0,c,d}) { vertices.Add(Rounded(raw)); normals.Add(RoundedNormal(raw,outward)); }
                uv.AddRange(new[]{new Vector2(u/3f,v/3f),new Vector2((u+1)/3f,v/3f),new Vector2((u+1)/3f,(v+1)/3f),new Vector2(u/3f,(v+1)/3f)});
                var winding=Vector3.Dot(Vector3.Cross(vertices[i+1]-vertices[i],vertices[i+2]-vertices[i]),outward)>=0f;
                triangles.AddRange(winding?new[]{i,i+1,i+2,i,i+2,i+3}:new[]{i,i+2,i+1,i,i+3,i+2});
            }
            for(var u=0;u<3;u++) for(var v=0;v<3;v++)
            {
                Cell(new Vector3(-hx,ys[u],zs[v]),new Vector3(-hx,ys[u+1],zs[v]),new Vector3(-hx,ys[u+1],zs[v+1]),new Vector3(-hx,ys[u],zs[v+1]),Vector3.left,u,v);
                Cell(new Vector3(hx,ys[u],zs[v]),new Vector3(hx,ys[u+1],zs[v]),new Vector3(hx,ys[u+1],zs[v+1]),new Vector3(hx,ys[u],zs[v+1]),Vector3.right,u,v);
                Cell(new Vector3(xs[u],-hy,zs[v]),new Vector3(xs[u+1],-hy,zs[v]),new Vector3(xs[u+1],-hy,zs[v+1]),new Vector3(xs[u],-hy,zs[v+1]),Vector3.down,u,v);
                Cell(new Vector3(xs[u],hy,zs[v]),new Vector3(xs[u+1],hy,zs[v]),new Vector3(xs[u+1],hy,zs[v+1]),new Vector3(xs[u],hy,zs[v+1]),Vector3.up,u,v);
                Cell(new Vector3(xs[u],ys[v],-hz),new Vector3(xs[u+1],ys[v],-hz),new Vector3(xs[u+1],ys[v+1],-hz),new Vector3(xs[u],ys[v+1],-hz),Vector3.back,u,v);
                Cell(new Vector3(xs[u],ys[v],hz),new Vector3(xs[u+1],ys[v],hz),new Vector3(xs[u+1],ys[v+1],hz),new Vector3(xs[u],ys[v+1],hz),Vector3.forward,u,v);
            }
            var mesh=new Mesh{vertices=vertices.ToArray(),normals=normals.ToArray(),triangles=triangles.ToArray(),uv=uv.ToArray()}; mesh.RecalculateBounds(); return mesh;
        }

        static Mesh CreateLathe(Vector2[] profile, int segments)
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (var s=0;s<=segments;s++) { var angle=Mathf.PI*2f*s/segments; var cos=Mathf.Cos(angle); var sin=Mathf.Sin(angle); for(var p=0;p<profile.Length;p++){ vertices.Add(new Vector3(profile[p].x*cos,profile[p].y,profile[p].x*sin)); uv.Add(new Vector2((float)s/segments,(float)p/(profile.Length-1))); } }
            for(var s=0;s<segments;s++) for(var p=0;p<profile.Length-1;p++){ var a=s*profile.Length+p; var b=(s+1)*profile.Length+p; triangles.AddRange(new[]{a,b,a+1,b,b+1,a+1}); }
            var mesh=new Mesh{vertices=vertices.ToArray(),triangles=triangles.ToArray(),uv=uv.ToArray()}; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        static Mesh CreateTorus(float radius,float tube,int radial,int sides)
        {
            var vertices=new List<Vector3>(); var uv=new List<Vector2>(); var triangles=new List<int>();
            for(var r=0;r<=radial;r++){ var a=Mathf.PI*2f*r/radial; for(var s=0;s<=sides;s++){ var b=Mathf.PI*2f*s/sides; var rr=radius+tube*Mathf.Cos(b); vertices.Add(new Vector3(rr*Mathf.Cos(a),tube*Mathf.Sin(b),rr*Mathf.Sin(a))); uv.Add(new Vector2((float)r/radial,(float)s/sides)); } }
            for(var r=0;r<radial;r++) for(var s=0;s<sides;s++){ var a=r*(sides+1)+s; var b=(r+1)*(sides+1)+s; triangles.AddRange(new[]{a,b,a+1,b,b+1,a+1}); }
            var mesh=new Mesh{vertices=vertices.ToArray(),triangles=triangles.ToArray(),uv=uv.ToArray()}; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        static Mesh CreateCylinder(float radius,float height,int segments)
        {
            var profile=new[]{new Vector2(0f,-height/2f),new Vector2(radius,-height/2f),new Vector2(radius,height/2f),new Vector2(0f,height/2f)};
            return CreateLathe(profile,segments);
        }

        static void EnsureFolders()
        {
            foreach(var path in new[]{"Assets/_Game/Art",ArtRoot,MeshRoot,MaterialRoot,TextureRoot,"Assets/_Game/Prefabs",PrefabRoot,"Assets/_Game/Scenes"}) EnsureFolder(path);
        }

        static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path)) return; var parent=Path.GetDirectoryName(path)?.Replace('\\','/'); if(!string.IsNullOrEmpty(parent)) EnsureFolder(parent); AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }

        static void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero; }
        static void Anchors(RectTransform rect,Vector2 min,Vector2 max) { rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero; }
    }
}
