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
    public static class PremiumT01SceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Gameplay_Disassembly_T01_Premium.unity";
        public const string BoardPrefabPath = "Assets/_Game/Prefabs/Disassembly/Premium/T01_Premium_Board.prefab";
        public const string WorkshopPrefabPath = "Assets/_Game/Prefabs/Disassembly/Premium/T01_Premium_Workshop.prefab";

        const string PremiumRoot = "Assets/_Game/Art/PremiumStarter";
        const string ModelRoot = PremiumRoot + "/Models";
        const string TextureRoot = PremiumRoot + "/Textures";
        const string UiRoot = PremiumRoot + "/UI";
        const string AudioRoot = PremiumRoot + "/Audio";
        const string MaterialRoot = PremiumRoot + "/Materials";
        const string PrefabRoot = "Assets/_Game/Prefabs/Disassembly/Premium";
        const string SelectionRingPath = "Assets/_Game/Art/DisassemblyT01/Meshes/HoleRing.asset";
        const string HoleDiscPath = "Assets/_Game/Art/DisassemblyT01/Meshes/HoleDisc.asset";

        static readonly Color Ink = new Color(0.105f, 0.095f, 0.082f);
        static readonly Color Panel = new Color(0.155f, 0.13f, 0.105f, 0.97f);
        static readonly Color PanelRaised = new Color(0.235f, 0.195f, 0.15f, 0.98f);
        static readonly Color Ivory = new Color(0.94f, 0.9f, 0.82f);
        static readonly Color Brass = new Color(0.66f, 0.42f, 0.16f);
        static readonly Color CalmTeal = new Color(0.16f, 0.66f, 0.62f);

        [MenuItem("Tools/Screw & Create/Build/Build Premium Disassembly T01")]
        public static void Build()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImporters();
            var materials = CreateMaterials();
            var selectionRing = AssetDatabase.LoadAssetAtPath<Mesh>(SelectionRingPath);
            var holeDisc = AssetDatabase.LoadAssetAtPath<Mesh>(HoleDiscPath);
            Require(selectionRing, SelectionRingPath);
            Require(holeDisc, HoleDiscPath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("DisassemblyT01PremiumRoot");
            var controller = root.AddComponent<DisassemblyT01Controller>();
            var workshopRoot = new GameObject("WorkshopBackdrop");
            workshopRoot.transform.SetParent(root.transform);
            var boardRoot = new GameObject("BoardPresentation");
            boardRoot.transform.SetParent(root.transform);
            var cameraRoot = new GameObject("CameraAndLighting");
            cameraRoot.transform.SetParent(root.transform);
            var hudRoot = new GameObject("HUD");
            hudRoot.transform.SetParent(root.transform);
            new GameObject("InputSystemInteraction").transform.SetParent(root.transform);

            BuildWorkshop(workshopRoot.transform, materials);
            BuildBoard(boardRoot.transform, materials);
            var plank = BuildPlank(boardRoot.transform, materials);
            var source = BuildHole("H_C1", new Vector3(-1.42f, 1.18f, -0.06f), boardRoot.transform, materials, selectionRing, holeDisc);
            var destination = BuildHole("H_P1", new Vector3(1.42f, -1.62f, 0.48f), boardRoot.transform, materials, selectionRing, holeDisc);
            var screw = BuildScrew(source.ScrewAnchor, boardRoot.transform, materials, selectionRing);
            var releasedPose = new GameObject("ReleasedPose_C_PLANK").transform;
            releasedPose.SetParent(boardRoot.transform);
            releasedPose.localPosition = new Vector3(0.12f, -2.72f, 0.12f);
            releasedPose.localRotation = Quaternion.Euler(0f, 0f, -11f);
            plank.ReleasedPose = releasedPose;

            var camera = BuildCamera(cameraRoot.transform);
            BuildLights(cameraRoot.transform);
            BuildUi(hudRoot.transform, controller, out var level, out var progress, out var instruction, out var status,
                out var pause, out var restart, out var completion);
            BuildEventSystem(root.transform);
            ConfigureAudio(root.transform, controller);

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
            controller.EffectsVolume = 0.42f;

            PrefabUtility.SaveAsPrefabAsset(boardRoot, BoardPrefabPath);
            PrefabUtility.SaveAsPrefabAsset(workshopRoot, WorkshopPrefabPath);
            controller.Screws = root.GetComponentsInChildren<DisassemblyScrewView>(true);
            controller.Holes = root.GetComponentsInChildren<DisassemblyHoleView>(true)
                .OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            controller.Components = root.GetComponentsInChildren<DisassemblyComponentView>(true);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/_Game/Scenes/Gameplay_Windmill_Graybox.unity", true),
                new EditorBuildSettingsScene(DisassemblyT01SceneBuilder.ScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("PREMIUM_T01_SCENE_BUILT scene=" + ScenePath);
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
            PlayerSettings.bundleVersion = "0.3.0-premium";
            PlayerSettings.Android.bundleVersionCode = 3;
            var output = Path.GetFullPath("Builds/Premium/ScrewAndCreate-Premium-T01.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            Debug.Log($"PREMIUM_ANDROID_BUILD result={report.summary.result} bytes={report.summary.totalSize} output={output}");
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        static void ConfigureImporters()
        {
            foreach (var path in AssetDatabase.FindAssets("t:Model", new[] { ModelRoot }).Select(AssetDatabase.GUIDToAssetPath))
            {
                if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) continue;
                importer.globalScale = 1f;
                importer.importNormals = ModelImporterNormals.Calculate;
                importer.normalSmoothingAngle = 52f;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.isReadable = false;
                importer.SaveAndReimport();
            }

            ConfigureTexture("Oak_Natural_BaseColor.png", false, true, true);
            ConfigureTexture("Birch_Honey_BaseColor.png", false, true, true);
            ConfigureTexture("Walnut_Warm_BaseColor.png", false, true, true);
            ConfigureTexture("Oak_Natural_NormalGL.png", true, false, true);
            ConfigureTexture("Birch_Honey_NormalGL.png", true, false, true);
            ConfigureTexture("Walnut_Warm_NormalGL.png", true, false, true);
            ConfigureTexture("Metal_Brushed_NormalGL.png", true, false, true);
            foreach (var path in AssetDatabase.FindAssets("t:Texture2D", new[] { TextureRoot + "/Generated" }).Select(AssetDatabase.GUIDToAssetPath))
                ConfigureTexturePath(path, false, false, true);
            foreach (var path in AssetDatabase.FindAssets("t:Texture2D", new[] { UiRoot }).Select(AssetDatabase.GUIDToAssetPath))
            {
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
            foreach (var path in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot }).Select(AssetDatabase.GUIDToAssetPath))
            {
                if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) continue;
                importer.forceToMono = true;
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.66f;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        static void ConfigureTexture(string name, bool normal, bool srgb, bool repeat) =>
            ConfigureTexturePath(TextureRoot + "/" + name, normal, srgb, repeat);

        static void ConfigureTexturePath(string path, bool normal, bool srgb, bool repeat)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                throw new InvalidOperationException("Missing texture importer: " + path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb;
            importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = repeat ? 4 : 1;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        static Dictionary<string, Material> CreateMaterials()
        {
            var result = new Dictionary<string, Material>
            {
                ["Oak"] = WoodMaterial("Oak_Natural", new Color(0.93f, 0.9f, 0.84f), 0.52f),
                ["Birch"] = WoodMaterial("Birch_Honey", new Color(0.98f, 0.95f, 0.88f), 0.48f),
                ["Walnut"] = WoodMaterial("Walnut_Warm", new Color(0.78f, 0.73f, 0.68f), 0.56f),
                ["Brass"] = LitMaterial("Brass_Satin", new Color(0.76f, 0.5f, 0.2f), 0.88f, 0.6f,
                    Texture("Metal_Brushed_NormalGL.png"), 0.28f),
                ["Steel"] = LitMaterial("Steel_Satin", new Color(0.48f, 0.54f, 0.56f), 0.9f, 0.52f,
                    Texture("Metal_Brushed_NormalGL.png"), 0.24f),
                ["Recess"] = LitMaterial("Screw_Recess", new Color(0.045f, 0.04f, 0.032f), 0.25f, 0.22f),
                ["HoleInterior"] = LitMaterial("Hole_Interior", new Color(0.028f, 0.025f, 0.02f), 0.05f, 0.18f),
                ["Backdrop"] = LitMaterial("Workshop_Backdrop", new Color(0.115f, 0.095f, 0.077f), 0f, 0.25f),
                ["HoleIdle"] = LitMaterial("Hole_Idle", new Color(0.24f, 0.18f, 0.115f), 0.5f, 0.42f),
                ["HoleLegal"] = LitMaterial("Hole_Legal", new Color(0.08f, 0.54f, 0.46f), 0.28f, 0.58f),
                ["ScrewSelected"] = LitMaterial("Screw_Selected", new Color(0.96f, 0.68f, 0.2f), 0.82f, 0.68f),
                ["Invalid"] = LitMaterial("Invalid", new Color(0.76f, 0.1f, 0.07f), 0.1f, 0.35f, null, 0f,
                    new Color(0.38f, 0.015f, 0.008f))
            };
            return result;
        }

        static Material WoodMaterial(string species, Color tint, float normalStrength)
        {
            var material = LitMaterial(species, tint, 0f, 1f, Texture(species + "_NormalGL.png"), normalStrength);
            material.SetTexture("_BaseMap", Texture(species + "_BaseColor.png"));
            material.SetTexture("_MetallicGlossMap", Texture("Generated/" + species + "_MetallicSmoothness.png"));
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material LitMaterial(string name, Color color, float metallic, float smoothness,
            Texture normal = null, float normalStrength = 0f, Color? emission = null)
        {
            var path = MaterialRoot + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            Require(shader, "Universal Render Pipeline/Lit");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else material.shader = shader;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_EnvironmentReflections", 1f);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", normalStrength);
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                material.SetTexture("_BumpMap", null);
                material.DisableKeyword("_NORMALMAP");
            }
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        static void BuildWorkshop(Transform parent, IReadOnlyDictionary<string, Material> materials)
        {
            var background = Model("WorkshopWalnutPanel", parent, "Board_Base_Walnut.obj",
                new Vector3(0f, 0.1f, 2.1f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.8f, 1.8f, 1.5f));
            AssignAll(background, materials["Backdrop"]);
            var leftRail = Model("LeftWorkshopRail", parent, "Plank_Long_Oak.obj",
                new Vector3(-4.25f, 0.25f, 1.45f), Quaternion.Euler(0f, 180f, 90f), new Vector3(2.5f, 1.3f, 1.4f));
            var rightRail = Model("RightWorkshopRail", parent, "Plank_Long_Oak.obj",
                new Vector3(4.25f, 0.25f, 1.45f), Quaternion.Euler(0f, 180f, 90f), new Vector3(2.5f, 1.3f, 1.4f));
            AssignAll(leftRail, materials["Oak"]);
            AssignAll(rightRail, materials["Oak"]);
        }

        static void BuildBoard(Transform parent, IReadOnlyDictionary<string, Material> materials)
        {
            var walnut = Model("WalnutBacking", parent, "Board_Base_Walnut.obj",
                new Vector3(0f, 0.22f, 1.12f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.16f, 1.16f, 1.05f));
            AssignAll(walnut, materials["Walnut"]);
            var oak = Model("OakWorkSurface", parent, "Board_Base_Oak.obj",
                new Vector3(0f, 0.22f, 0.7f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.045f, 1.045f, 0.92f));
            AssignAll(oak, materials["Oak"]);
        }

        static DisassemblyComponentView BuildPlank(Transform parent, IReadOnlyDictionary<string, Material> materials)
        {
            var root = new GameObject("C_PLANK");
            root.transform.SetParent(parent);
            root.transform.localPosition = new Vector3(0f, 1.18f, 0.27f);
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(5.05f, 1.24f, 0.55f);
            var view = root.AddComponent<DisassemblyComponentView>();
            view.Id = "C_PLANK";
            var visual = Model("PremiumBirchPlank", root.transform, "Plank_Wide_Birch.obj",
                Vector3.zero, Quaternion.Euler(0f, 180f, 0f), new Vector3(1.36f, 1.36f, 2.0f));
            AssignAll(visual, materials["Birch"]);
            return view;
        }

        static DisassemblyHoleView BuildHole(string id, Vector3 position, Transform parent,
            IReadOnlyDictionary<string, Material> materials, Mesh selectionRing, Mesh holeDisc)
        {
            var root = new GameObject(id);
            root.transform.SetParent(parent);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            root.layer = LayerMask.NameToLayer("GameplayInteraction");
            var collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.radius = 0.49f;
            collider.height = 0.24f;
            var visual = Model("PremiumBrassHole", root.transform, "Hole_Brass_Rim.obj",
                Vector3.zero, Quaternion.Euler(90f, 0f, 0f), Vector3.one * 1.92f);
            AssignSlots(visual, materials["Brass"], materials["HoleInterior"]);
            MeshObject("HoleDepth", root.transform, new Vector3(0f, -0.03f, 0f), Vector3.one * 0.8f,
                Quaternion.identity, holeDisc, materials["HoleInterior"]);
            var glow = MeshObject("DestinationRing", root.transform, new Vector3(0f, -0.075f, 0f),
                Vector3.one * 1.08f, Quaternion.identity, selectionRing, materials["HoleIdle"]);
            var anchor = new GameObject("ScrewAnchor").transform;
            anchor.SetParent(root.transform, false);
            var view = root.AddComponent<DisassemblyHoleView>();
            view.Id = id;
            view.ScrewAnchor = anchor;
            view.RingRenderer = glow.GetComponent<Renderer>();
            return view;
        }

        static DisassemblyScrewView BuildScrew(Transform source, Transform parent,
            IReadOnlyDictionary<string, Material> materials, Mesh selectionRing)
        {
            var root = new GameObject("S_1");
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(source.position, source.rotation);
            root.layer = LayerMask.NameToLayer("GameplayInteraction");
            var visual = Model("PremiumPhillipsScrew", root.transform, "Screw_Phillips_Brass.obj",
                Vector3.zero, Quaternion.Euler(90f, 0f, 0f), Vector3.one * 1.56f);
            AssignSlots(visual, materials["Brass"], materials["Recess"]);
            var collar = MeshObject("SelectionCollar", root.transform, new Vector3(0f, -0.065f, 0f),
                Vector3.one * 0.9f, Quaternion.identity, selectionRing, materials["Brass"]);
            var collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.radius = 0.44f;
            collider.height = 1.18f;
            collider.center = new Vector3(0f, 0.08f, 0f);
            var view = root.AddComponent<DisassemblyScrewView>();
            view.Id = "S_1";
            view.TargetRenderer = collar.GetComponent<Renderer>();
            view.TargetCollider = collider;
            return view;
        }

        static Camera BuildCamera(Transform parent)
        {
            var go = new GameObject("PortraitCamera");
            go.transform.SetParent(parent);
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0.22f, -12f);
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.4f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            camera.backgroundColor = new Color(0.055f, 0.047f, 0.038f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = true;
            var fitter = go.AddComponent<DisassemblyCameraFitter>();
            fitter.TargetCamera = camera;
            fitter.ViewportRegion = new Rect(0.055f, 0.155f, 0.89f, 0.72f);
            fitter.Padding = 1.035f;
            return camera;
        }

        static void BuildLights(Transform parent)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.31f, 0.285f, 0.25f);
            RenderSettings.ambientIntensity = 0.82f;
            var key = new GameObject("SoftWorkshopKey");
            key.transform.SetParent(parent);
            key.transform.rotation = Quaternion.Euler(32f, -28f, 0f);
            var light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.91f, 0.79f);
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.52f;
            light.shadowBias = 0.055f;
            light.shadowNormalBias = 0.24f;

            var fill = new GameObject("SoftWindowFill");
            fill.transform.SetParent(parent);
            fill.transform.position = new Vector3(-3.4f, 2.7f, -3.5f);
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.color = new Color(0.68f, 0.81f, 0.86f);
            fillLight.intensity = 1.05f;
            fillLight.range = 11f;
            fillLight.shadows = LightShadows.None;

            var rim = new GameObject("WarmBenchBounce");
            rim.transform.SetParent(parent);
            rim.transform.position = new Vector3(3.2f, -2.8f, -2.2f);
            var rimLight = rim.AddComponent<Light>();
            rimLight.type = LightType.Point;
            rimLight.color = new Color(1f, 0.69f, 0.42f);
            rimLight.intensity = 0.62f;
            rimLight.range = 8f;
            rimLight.shadows = LightShadows.None;
        }

        static void ConfigureAudio(Transform parent, DisassemblyT01Controller controller)
        {
            var audio = new GameObject("AudioHooks");
            audio.transform.SetParent(parent);
            var source = audio.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
            source.dopplerLevel = 0f;
            controller.EffectsSource = source;
            controller.ScrewSelectClip = Clip("Screw_Select_Click.wav");
            controller.ScrewUnscrewClip = Clip("Screw_Unscrew_Whirr.wav");
            controller.ScrewInsertClip = Clip("Screw_Insert_Set.wav");
            controller.WoodReleaseClip = Clip("Wood_Release_Thud.wav");
            controller.LevelCompleteClip = Clip("Level_Complete_Chime.wav");
        }

        static void BuildUi(Transform parent, DisassemblyT01Controller controller, out Text level,
            out Text progress, out Text instruction, out Text status, out GameObject pause,
            out GameObject restart, out GameObject completion)
        {
            var canvasGo = new GameObject("PortraitCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(parent);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.78f;
            var safe = new GameObject("SafeArea", typeof(RectTransform), typeof(DisassemblySafeAreaFitter));
            safe.transform.SetParent(canvasGo.transform, false);
            Stretch(safe.GetComponent<RectTransform>());

            var top = PanelObject("TopBar", safe.transform, new Vector2(0.035f, 0.895f), new Vector2(0.965f, 0.973f), Panel);
            top.GetComponent<Image>().raycastTarget = false;
            AddOutline(top, new Color(0.55f, 0.42f, 0.28f, 0.42f), new Vector2(2f, -2f));
            level = Label("LevelLabel", top.transform, "LEVEL 1   FIRST RELEASE", 34, TextAnchor.MiddleLeft, Ivory,
                new Vector2(0.045f, 0.08f), new Vector2(0.69f, 0.92f));
            progress = Label("Progress", top.transform, "PARTS  0/1", 25, TextAnchor.MiddleRight,
                new Color(0.78f, 0.69f, 0.52f), new Vector2(0.65f, 0.08f), new Vector2(0.84f, 0.92f));
            IconButton("PauseButton", top.transform, Sprite("Pause.png"), string.Empty,
                new Vector2(0.875f, 0.14f), new Vector2(0.955f, 0.86f), PanelRaised, controller.TogglePause, true);

            instruction = Label("Instruction", safe.transform, "Move the screw to the empty hole.", 32,
                TextAnchor.MiddleCenter, Ivory, new Vector2(0.08f, 0.842f), new Vector2(0.92f, 0.888f));
            status = Label("Status", safe.transform, "Tap the brass screw.", 24,
                TextAnchor.MiddleCenter, new Color(0.75f, 0.82f, 0.78f), new Vector2(0.09f, 0.145f), new Vector2(0.91f, 0.182f));

            var bottom = PanelObject("BottomControls", safe.transform, new Vector2(0.035f, 0.028f), new Vector2(0.965f, 0.132f), Panel);
            bottom.GetComponent<Image>().raycastTarget = false;
            AddOutline(bottom, new Color(0.55f, 0.42f, 0.28f, 0.42f), new Vector2(2f, -2f));
            IconButton("UndoButton", bottom.transform, Sprite("Undo.png"), "UNDO",
                new Vector2(0.035f, 0.18f), new Vector2(0.315f, 0.82f), PanelRaised, controller.Undo);
            IconButton("RestartButton", bottom.transform, Sprite("Restart.png"), "RESTART",
                new Vector2(0.685f, 0.18f), new Vector2(0.965f, 0.82f), PanelRaised, controller.RequestRestart);
            Label("MechanicHint", bottom.transform, "SELECT  •  MOVE  •  RELEASE", 19, TextAnchor.MiddleCenter,
                new Color(0.67f, 0.58f, 0.44f), new Vector2(0.31f, 0.12f), new Vector2(0.69f, 0.88f));

            pause = Modal("PausePanel", safe.transform, "PAUSED\n\nYour puzzle is safe.", "RESUME", controller.TogglePause);
            restart = Modal("RestartPanel", safe.transform, "RESTART LEVEL?\n\nYour current move will be cleared.", "RESTART", controller.ConfirmRestart);
            IconButton("CancelButton", restart.transform, null, "CANCEL", new Vector2(0.16f, 0.12f),
                new Vector2(0.45f, 0.27f), PanelRaised, controller.CancelRestart);
            completion = Modal("CompletionPanel", safe.transform, "LEVEL COMPLETE\n\nThe plank is free!", "PLAY AGAIN", controller.ConfirmRestart);
            IconButton("UndoCompletion", completion.transform, Sprite("Undo.png"), "UNDO", new Vector2(0.16f, 0.12f),
                new Vector2(0.45f, 0.27f), PanelRaised, controller.Undo);
            pause.SetActive(false);
            restart.SetActive(false);
            completion.SetActive(false);
        }

        static GameObject Modal(string name, Transform parent, string message, string primary, UnityEngine.Events.UnityAction action)
        {
            var go = PanelObject(name, parent, new Vector2(0.1f, 0.315f), new Vector2(0.9f, 0.685f), new Color(0.095f, 0.08f, 0.065f, 0.99f));
            AddOutline(go, new Color(0.7f, 0.53f, 0.31f, 0.55f), new Vector2(3f, -3f));
            Label("Message", go.transform, message, 40, TextAnchor.MiddleCenter, Ivory,
                new Vector2(0.08f, 0.31f), new Vector2(0.92f, 0.9f));
            IconButton("PrimaryButton", go.transform, Sprite("Check.png"), primary,
                new Vector2(0.55f, 0.12f), new Vector2(0.84f, 0.27f), new Color(0.44f, 0.31f, 0.16f), action);
            return go;
        }

        static void BuildEventSystem(Transform parent)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetParent(parent);
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        static GameObject PanelObject(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Anchors(go.GetComponent<RectTransform>(), min, max);
            go.GetComponent<Image>().color = color;
            return go;
        }

        static void AddOutline(GameObject go, Color color, Vector2 distance)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        static Text Label(string name, Transform parent, string text, int size, TextAnchor alignment,
            Color color, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Anchors(go.GetComponent<RectTransform>(), min, max);
            var label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = size;
            label.raycastTarget = false;
            return label;
        }

        static Button IconButton(string name, Transform parent, Sprite icon, string text, Vector2 min, Vector2 max,
            Color color, UnityEngine.Events.UnityAction action, bool iconOnly = false)
        {
            var go = PanelObject(name, parent, min, max, color);
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            if (icon != null)
            {
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(go.transform, false);
                var iconRect = iconGo.GetComponent<RectTransform>();
                Anchors(iconRect, iconOnly ? new Vector2(0.22f, 0.2f) : new Vector2(0.08f, 0.2f),
                    iconOnly ? new Vector2(0.78f, 0.8f) : new Vector2(0.31f, 0.8f));
                var image = iconGo.GetComponent<Image>();
                image.sprite = icon;
                image.preserveAspect = true;
                image.color = Ivory;
                image.raycastTarget = false;
            }
            if (!string.IsNullOrEmpty(text))
                Label("Text", go.transform, text, 25, TextAnchor.MiddleCenter, Ivory,
                    icon == null ? Vector2.zero : new Vector2(0.27f, 0f), Vector2.one);
            UnityEventTools.AddPersistentListener(button.onClick, action);
            return button;
        }

        static GameObject Model(string name, Transform parent, string modelName, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var assetPath = ModelRoot + "/" + modelName;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            Require(prefab, assetPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.transform.localScale = scale;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            return go;
        }

        static void AssignAll(GameObject root, Material material)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = Enumerable.Repeat(material, Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
        }

        static void AssignSlots(GameObject root, Material primary, Material detail)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var existing = renderer.sharedMaterials;
                if (existing.Length == 0) renderer.sharedMaterial = primary;
                else renderer.sharedMaterials = existing.Select((material, index) =>
                {
                    var materialName = material == null ? string.Empty : material.name;
                    return index > 0 || materialName.IndexOf("Recess", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        materialName.IndexOf("Interior", StringComparison.OrdinalIgnoreCase) >= 0 ? detail : primary;
                }).ToArray();
            }
        }

        static GameObject MeshObject(string name, Transform parent, Vector3 position, Vector3 scale,
            Quaternion rotation, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        static Texture2D Texture(string relativePath)
        {
            var path = TextureRoot + "/" + relativePath;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Require(texture, path);
            return texture;
        }

        static Sprite Sprite(string fileName)
        {
            var path = UiRoot + "/" + fileName;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Require(sprite, path);
            return sprite;
        }

        static AudioClip Clip(string fileName)
        {
            var path = AudioRoot + "/" + fileName;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            Require(clip, path);
            return clip;
        }

        static void EnsureFolders()
        {
            foreach (var path in new[] { MaterialRoot, "Assets/_Game/Prefabs", "Assets/_Game/Prefabs/Disassembly", PrefabRoot, "Assets/_Game/Scenes" })
                EnsureFolder(path);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void Require(UnityEngine.Object value, string name)
        {
            if (value == null) throw new InvalidOperationException("Required premium asset is unavailable: " + name);
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Anchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
