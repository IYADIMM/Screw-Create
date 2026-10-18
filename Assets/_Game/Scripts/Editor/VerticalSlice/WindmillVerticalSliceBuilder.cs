using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScrewAndCreate.Presentation;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ScrewAndCreate.Editor.VerticalSlice
{
    public static class WindmillVerticalSliceBuilder
    {
        const string ScenePath = "Assets/_Game/Scenes/Gameplay_Windmill_Graybox.unity";
        const string MaterialPath = "Assets/_Game/Materials/TEMP_Workshop";
        static readonly Color Wood = new Color(0.56f, 0.25f, 0.09f);
        static readonly Color LightWood = new Color(0.86f, 0.58f, 0.25f);
        static readonly Color Cream = new Color(1f, 0.86f, 0.62f);
        static readonly Color Teal = new Color(0.02f, 0.48f, 0.5f);
        static readonly Color Brass = new Color(0.8f, 0.48f, 0.08f);

        [MenuItem("Tools/Screw & Create/Build/Refresh Windmill Vertical Slice")]
        public static void Build()
        {
            EnsureFolders();
            var materials = CreateMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var composition = new GameObject("CompositionRoot");
            var controller = composition.AddComponent<WindmillVerticalSliceController>();
            new GameObject("PuzzleModelController").transform.SetParent(composition.transform);
            new GameObject("Interaction_InputSystem").transform.SetParent(composition.transform);
            new GameObject("DisassemblyPresentation").transform.SetParent(composition.transform);
            new GameObject("AssemblyPresentation").transform.SetParent(composition.transform);
            var mechanism = new GameObject("WindmillMechanism"); mechanism.transform.SetParent(composition.transform);
            var socketRoot = new GameObject("Sockets_BlueprintTargets"); socketRoot.transform.SetParent(composition.transform);
            var trayRoot = new GameObject("ActiveTray"); trayRoot.transform.SetParent(composition.transform);
            var cameraRoot = new GameObject("CameraAndLighting"); cameraRoot.transform.SetParent(composition.transform);
            var hudRoot = new GameObject("HUD"); hudRoot.transform.SetParent(composition.transform);
            new GameObject("AudioHapticsServices").transform.SetParent(composition.transform);
            new GameObject("LocalSaveService").transform.SetParent(composition.transform);
            new GameObject("DevelopmentDiagnostics").transform.SetParent(composition.transform);

            BuildWorkshop(cameraRoot.transform, materials);
            var camera = BuildCamera(cameraRoot.transform);
            BuildLights(cameraRoot.transform);
            BuildWindmillBody(mechanism.transform, materials);
            var trayTargets = BuildTray(trayRoot.transform, materials);
            BuildMechanism(mechanism.transform, socketRoot.transform, trayTargets, materials, out var screws, out var parts, out var sockets, out var bladesRoot);
            var particles = BuildParticles(composition.transform);
            BuildUi(hudRoot.transform, controller, materials, out var status, out var trayText, out var seedText, out var pause, out var settings, out var restart, out var completion);
            BuildEventSystem(composition.transform);

            controller.GameplayCamera = camera; controller.PlayfieldFitter = camera.GetComponent<PlayfieldFitter>(); controller.Screws = screws; controller.Parts = parts; controller.Sockets = sockets; controller.BladesRoot = bladesRoot;
            controller.StatusText = status; controller.TrayText = trayText; controller.SeedText = seedText; controller.PausePanel = pause; controller.SettingsPanel = settings; controller.RestartPanel = restart; controller.CompletionPanel = completion;
            controller.BrassMaterial = materials["Brass"]; controller.BlockedMaterial = materials["Blocked"]; controller.SelectedMaterial = materials["Selected"]; controller.HintMaterial = materials["Hint"]; controller.GhostMaterial = materials["Ghost"];
            controller.ScrewParticles = particles;

            PrefabUtility.SaveAsPrefabAsset(mechanism, "Assets/_Game/Prefabs/TEMP_Windmill/TEMP_Windmill_Mechanism.prefab");
            // Rebind after prefab serialization so scene references cannot retain replaced component handles.
            controller.Screws = composition.GetComponentsInChildren<ScrewView>(true).OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            controller.Parts = composition.GetComponentsInChildren<PartView>(true).OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            controller.Sockets = composition.GetComponentsInChildren<SocketView>(true).OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log($"PHASE2_VERTICAL_SLICE_BUILT scene={ScenePath} screws={screws.Length} parts={parts.Length} sockets={sockets.Length}");
        }

        public static void BuildFromCommandLine() { Build(); EditorApplication.Exit(0); }

        public static void BuildAndroidDevelopmentFromCommandLine()
        {
            Build();
            var output = Path.GetFullPath("Builds/Phase2/ScrewAndCreate-Phase2.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            Debug.Log($"PHASE2_ANDROID_BUILD result={report.summary.result} bytes={report.summary.totalSize} output={output}");
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        public static void CaptureScreenshotsFromCommandLine()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var camera = UnityEngine.Object.FindFirstObjectByType<WindmillVerticalSliceController>().GameplayCamera;
            var output = Path.GetFullPath("Docs/Phase2Screenshots"); Directory.CreateDirectory(output);
            foreach (var capture in new[] { ("reference-portrait", 540, 960), ("narrow-phone", 450, 900), ("tall-phone", 540, 1080) })
                Capture(camera, Path.Combine(output, capture.Item1 + ".png"), capture.Item2, capture.Item3);
            Debug.Log($"PHASE2_SCREENSHOTS_CAPTURED output={output}"); EditorApplication.Exit(0);
        }

        static void Capture(Camera camera, string path, int width, int height)
        {
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = canvases.Select(x => x.renderMode).ToArray(); var worldCameras = canvases.Select(x => x.worldCamera).ToArray();
            for (var i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = 0.5f; }
            var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active; var previousAspect = camera.aspect;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32); var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.aspect = (float)width / height; camera.targetTexture = target; RenderTexture.active = target; camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget; camera.aspect = previousAspect; RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(target);
                for (var i = 0; i < canvases.Length; i++) { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = worldCameras[i]; }
            }
        }

        static void EnsureFolders()
        {
            foreach (var folder in new[]
            {
                "Assets/_Game/Scenes", "Assets/_Game/Prefabs", "Assets/_Game/Prefabs/TEMP_Windmill", "Assets/_Game/Prefabs/Common",
                "Assets/_Game/Materials", MaterialPath, "Assets/_Game/UI", "Assets/_Game/VFX", "Assets/_Game/Scripts/Runtime/Services", "Assets/_Game/Tests/PlayMode"
            }) EnsureFolder(folder);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/'); var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent); AssetDatabase.CreateFolder(parent, name);
        }

        static Dictionary<string, Material> CreateMaterials()
        {
            var definitions = new Dictionary<string, Color>
            {
                ["Wood"] = Wood, ["LightWood"] = LightWood, ["Cream"] = Cream, ["Teal"] = Teal, ["Brass"] = Brass,
                ["Blocked"] = new Color(0.32f,0.28f,0.25f), ["Selected"] = new Color(0.2f,0.95f,0.82f), ["Hint"] = new Color(1f,0.82f,0.22f), ["Ghost"] = new Color(0.1f,0.9f,0.82f,0.35f),
                ["Roof"] = new Color(0.02f,0.22f,0.25f), ["Background"] = new Color(0.25f,0.13f,0.07f), ["Green"] = new Color(0.18f,0.42f,0.16f)
            };
            var result = new Dictionary<string, Material>();
            foreach (var pair in definitions)
            {
                var path = $"{MaterialPath}/TEMP_{pair.Key}.mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
                else material.shader = shader;
                material.color = pair.Value; material.SetColor("_BaseColor", pair.Value); material.SetColor("_EmissionColor", pair.Value * 0.18f); material.EnableKeyword("_EMISSION"); material.SetFloat("_Smoothness", pair.Key == "Brass" ? 0.72f : 0.24f);
                if (pair.Key == "Brass") material.SetFloat("_Metallic", 0.78f);
                if (pair.Key == "Ghost") { material.SetFloat("_Surface", 1f); material.SetFloat("_Blend", 0f); material.SetFloat("_SrcBlend", 5f); material.SetFloat("_DstBlend", 10f); material.SetFloat("_ZWrite", 0f); material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000; }
                EditorUtility.SetDirty(material); result[pair.Key] = material;
            }
            return result;
        }

        static Camera BuildCamera(Transform parent)
        {
            var go = new GameObject("PortraitCamera"); go.transform.SetParent(parent); go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5.7f; camera.nearClipPlane = 0.1f; camera.farClipPlane = 100f; camera.backgroundColor = new Color(0.16f, 0.08f, 0.04f);
            var fitter = go.AddComponent<PlayfieldFitter>(); fitter.TargetCamera = camera;
            go.transform.position = new Vector3(0f, 1.5f, -12f); go.transform.rotation = Quaternion.identity; return camera;
        }

        static void BuildLights(Transform parent)
        {
            RenderSettings.ambientLight = new Color(0.34f, 0.23f, 0.16f);
            var key = new GameObject("WarmSun"); key.transform.SetParent(parent); key.transform.rotation = Quaternion.Euler(36f, -28f, 0f); var light = key.AddComponent<Light>(); light.type = LightType.Directional; light.color = new Color(1f, 0.82f, 0.66f); light.intensity = 1.15f; light.shadows = LightShadows.Soft;
            var fill = new GameObject("TealFill"); fill.transform.SetParent(parent); fill.transform.position = new Vector3(-3f, 3f, -3f); var fillLight = fill.AddComponent<Light>(); fillLight.type = LightType.Point; fillLight.color = new Color(0.22f, 0.62f, 0.58f); fillLight.intensity = 1.1f; fillLight.range = 9f;
        }

        static void BuildWorkshop(Transform parent, Dictionary<string, Material> m)
        {
            Primitive("TEMP_BackWall", PrimitiveType.Cube, parent, new Vector3(0, 1.6f, 3.2f), new Vector3(12, 11, 0.4f), m["Background"]);
            Primitive("TEMP_Workbench", PrimitiveType.Cube, parent, new Vector3(0, -4.2f, 0.7f), new Vector3(12, 1.1f, 5f), m["Wood"]);
            for (var i = -2; i <= 2; i++) Primitive($"TEMP_BackPlank_{i}", PrimitiveType.Cube, parent, new Vector3(i * 2.2f, 2.4f + (i % 2) * 0.5f, 2.7f), new Vector3(1.8f, 0.18f, 0.5f), m["LightWood"]);
            for (var i = 0; i < 8; i++) Primitive($"TEMP_Plant_{i}", PrimitiveType.Sphere, parent, new Vector3(-4.4f + i * 1.25f, -3.25f + (i % 2) * 0.25f, 0.1f), Vector3.one * (0.25f + (i % 3) * 0.06f), m["Green"]);
        }

        static void BuildWindmillBody(Transform parent, Dictionary<string, Material> m)
        {
            Primitive("TEMP_RoundBase", PrimitiveType.Cylinder, parent, new Vector3(0, -2.9f, 0), new Vector3(3.3f, 0.28f, 3.3f), m["Wood"]);
            Primitive("TEMP_Tower", PrimitiveType.Cube, parent, new Vector3(0, -0.25f, 0.2f), new Vector3(3.0f, 5.1f, 1.2f), m["LightWood"]);
            for (var x = -1; x <= 1; x++) Primitive($"TEMP_TowerPlank_{x}", PrimitiveType.Cube, parent, new Vector3(x * 0.88f, -0.2f, -0.44f), new Vector3(0.08f, 4.8f, 0.08f), m["Wood"]);
            Primitive("TEMP_Roof", PrimitiveType.Sphere, parent, new Vector3(0, 2.28f, 0.12f), new Vector3(2.05f, 0.62f, 1.35f), m["Roof"]);
            Primitive("TEMP_Door", PrimitiveType.Cube, parent, new Vector3(0, -1.85f, -0.48f), new Vector3(1.05f, 1.8f, 0.14f), m["Wood"]);
            Primitive("TEMP_Window", PrimitiveType.Cube, parent, new Vector3(0.82f, -0.25f, -0.48f), new Vector3(0.62f, 0.9f, 0.14f), m["Teal"]);
        }

        static Transform[] BuildTray(Transform parent, Dictionary<string, Material> m)
        {
            Primitive("TEMP_TrayBack", PrimitiveType.Cube, parent, new Vector3(0, -4.0f, -0.3f), new Vector3(6.8f, 1.35f, 0.42f), m["Cream"]);
            Primitive("TEMP_TrayFrame", PrimitiveType.Cube, parent, new Vector3(0, -4.0f, -0.05f), new Vector3(7.15f, 1.65f, 0.16f), m["Wood"]);
            var targets = new Transform[3];
            for (var i = 0; i < 3; i++) { var target = new GameObject($"TraySlot_{i + 1}"); target.transform.SetParent(parent); target.transform.position = new Vector3(-2.05f + i * 2.05f, -4f, -0.55f); targets[i] = target.transform; }
            return targets;
        }

        static void BuildMechanism(Transform parent, Transform socketRoot, Transform[] trayTargets, Dictionary<string, Material> m, out ScrewView[] screws, out PartView[] parts, out SocketView[] sockets, out Transform bladesRoot)
        {
            var root = new GameObject("TEMP_BladesRoot"); root.transform.SetParent(parent); root.transform.position = new Vector3(0, 2.55f, -0.75f); bladesRoot = root.transform;
            Primitive("TEMP_Hub", PrimitiveType.Cylinder, root.transform, Vector3.zero, new Vector3(0.7f, 0.22f, 0.7f), m["Wood"], Quaternion.Euler(90, 0, 0));
            var screwList = new List<ScrewView>(); var partList = new List<PartView>(); var socketList = new List<SocketView>();
            for (var i = 0; i < 4; i++)
            {
                var angle = i * 90f; var rotation = Quaternion.Euler(0, 0, angle); var direction = rotation * Vector3.up;
                var partGo = BuildBlade($"TEMP_Blade_P{i + 1}", root.transform, direction * 1.48f, rotation, m);
                SetLayerRecursively(partGo, LayerMask.NameToLayer("GameplayInteraction"));
                var part = partGo.AddComponent<PartView>(); part.Id = $"P{i + 1}"; part.TrayTarget = trayTargets[i % 3];
                var socketTarget = new GameObject($"SocketTarget_P{i + 1}"); socketTarget.transform.SetParent(socketRoot); socketTarget.transform.position = root.transform.TransformPoint(direction * 1.48f); socketTarget.transform.rotation = rotation; part.SocketTarget = socketTarget.transform;
                var ghostGo = Primitive($"Ghost_P{i + 1}", PrimitiveType.Cube, socketTarget.transform, Vector3.zero, new Vector3(0.88f, 2.28f, 0.12f), m["Ghost"]);
                var socket = ghostGo.AddComponent<SocketView>(); socket.Id = $"Socket-P{i + 1}"; socket.TargetRenderer = ghostGo.GetComponent<Renderer>(); socket.TargetRenderer.enabled = false;
                SetLayerRecursively(ghostGo, LayerMask.NameToLayer("GameplayInteraction"));
                var screwGo = Primitive($"TEMP_Screw_S{i + 1}", PrimitiveType.Cylinder, parent, root.transform.TransformPoint(direction * 1.75f) + new Vector3(0, 0, -0.42f), new Vector3(0.3f, 0.15f, 0.3f), m["Brass"], Quaternion.Euler(90, 0, 0));
                var screw = screwGo.AddComponent<ScrewView>(); screw.Id = $"S{i + 1}"; screw.TargetRenderer = screwGo.GetComponent<Renderer>();
                var slotA = Primitive("Phillips_A", PrimitiveType.Cube, screwGo.transform, new Vector3(0, 0.18f, 0), new Vector3(0.38f, 0.06f, 0.09f), m["Blocked"]);
                var slotB = Primitive("Phillips_B", PrimitiveType.Cube, screwGo.transform, new Vector3(0, 0.18f, 0), new Vector3(0.09f, 0.06f, 0.38f), m["Blocked"]);
                SetLayerRecursively(screwGo, LayerMask.NameToLayer("GameplayInteraction"));
                partList.Add(part); socketList.Add(socket); screwList.Add(screw);
            }
            parts = partList.ToArray(); sockets = socketList.ToArray(); screws = screwList.ToArray();
        }

        static GameObject BuildBlade(string name, Transform parent, Vector3 position, Quaternion rotation, Dictionary<string, Material> m)
        {
            var blade = new GameObject(name); blade.transform.SetParent(parent); blade.transform.localPosition = position; blade.transform.localRotation = rotation;
            var collider = blade.AddComponent<BoxCollider>(); collider.size = new Vector3(0.9f, 2.25f, 0.28f);
            Primitive("FrameLeft", PrimitiveType.Cube, blade.transform, new Vector3(-0.36f, 0, 0), new Vector3(0.14f, 2.3f, 0.24f), m["Wood"]);
            Primitive("FrameRight", PrimitiveType.Cube, blade.transform, new Vector3(0.36f, 0, 0), new Vector3(0.14f, 2.3f, 0.24f), m["Wood"]);
            Primitive("FrameTop", PrimitiveType.Cube, blade.transform, new Vector3(0, 1.08f, 0), new Vector3(0.86f, 0.15f, 0.24f), m["Wood"]);
            Primitive("FrameBottom", PrimitiveType.Cube, blade.transform, new Vector3(0, -1.08f, 0), new Vector3(0.86f, 0.15f, 0.24f), m["Wood"]);
            for (var slat = -2; slat <= 2; slat++) Primitive($"Slat_{slat + 3}", PrimitiveType.Cube, blade.transform, new Vector3(0, slat * 0.36f, 0.01f), new Vector3(0.58f, 0.25f, 0.2f), m["LightWood"]);
            return blade;
        }

        static ParticleSystem BuildParticles(Transform parent)
        {
            var go = new GameObject("TEMP_ScrewSparkles"); go.transform.SetParent(parent); var particles = go.AddComponent<ParticleSystem>(); particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main; main.duration = 0.35f; main.loop = false; main.startLifetime = 0.35f; main.startSpeed = 0.55f; main.startSize = 0.09f; main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.1f, 0.9f, 0.8f), new Color(1f, 0.72f, 0.16f)); main.maxParticles = 16;
            var emission = particles.emission; emission.rateOverTime = 0f; emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.12f; particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); return particles;
        }

        static void BuildUi(Transform parent, WindmillVerticalSliceController controller, Dictionary<string, Material> m, out Text status, out Text trayText, out Text seedText, out GameObject pause, out GameObject settings, out GameObject restart, out GameObject completion)
        {
            var canvasGo = new GameObject("PortraitCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); canvasGo.transform.SetParent(parent); var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 1f;
            var safe = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter)); safe.transform.SetParent(canvasGo.transform, false); Stretch(safe.GetComponent<RectTransform>());
            var top = Panel("TopWoodHUD", safe.transform, new Vector2(0.04f, 0.86f), new Vector2(0.96f, 0.98f), new Color(0.33f, 0.14f, 0.05f, 0.96f)); top.GetComponent<Image>().raycastTarget = false;
            Label("LevelLabel", top.transform, "LEVEL 7     ☆ ☆ ☆     WINDMILL", 38, TextAnchor.MiddleCenter, Color.white, new Vector2(0.2f, 0), new Vector2(0.86f, 1));
            status = Label("Status", safe.transform, "Remove a glowing brass screw", 31, TextAnchor.MiddleCenter, Cream, new Vector2(0.08f, 0.81f), new Vector2(0.92f, 0.85f));
            var tray = Panel("BottomTrayHUD", safe.transform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.19f), new Color(0.95f, 0.82f, 0.62f, 0.97f)); tray.GetComponent<Image>().raycastTarget = false;
            trayText = Label("TrayCount", tray.transform, "ACTIVE TRAY  0/3", 30, TextAnchor.UpperCenter, Wood, new Vector2(0.2f, 0.66f), new Vector2(0.8f, 0.96f));
            var developer = new GameObject("DeveloperControls", typeof(RectTransform)); developer.transform.SetParent(tray.transform, false); Stretch(developer.GetComponent<RectTransform>());
            seedText = Label("SeedDisplay", developer.transform, "DEV • WINDMILL • 0007", 20, TextAnchor.LowerCenter, Wood, new Vector2(0.2f, 0.02f), new Vector2(0.8f, 0.24f));
            Button("UndoButton", tray.transform, "↶", new Vector2(0.01f, 0.12f), new Vector2(0.18f, 0.88f), Teal, controller.Undo);
            Button("HintButton", tray.transform, "?", new Vector2(0.82f, 0.12f), new Vector2(0.99f, 0.88f), Teal, controller.Hint);
            Button("RotateButton", tray.transform, "ROTATE", new Vector2(0.62f, 0.28f), new Vector2(0.8f, 0.66f), Wood, controller.RotateSelected);
            Button("PauseButton", top.transform, "Ⅱ", new Vector2(0.88f, 0.12f), new Vector2(0.98f, 0.88f), Teal, controller.TogglePause);
            Button("RestartButton", top.transform, "RESTART", new Vector2(0.02f, 0.12f), new Vector2(0.2f, 0.88f), Wood, controller.RequestRestart);
            Button("SaveButton", developer.transform, "SAVE", new Vector2(0.2f, 0.28f), new Vector2(0.38f, 0.66f), Wood, controller.Save);
            Button("LoadButton", developer.transform, "LOAD", new Vector2(0.4f, 0.28f), new Vector2(0.58f, 0.66f), Wood, controller.Load);
            developer.SetActive(false);
            pause = Modal("PausePanel", safe.transform, "PAUSED\nTap pause to resume", controller.TogglePause);
            settings = Modal("SettingsPanel", safe.transform, "SETTINGS\nMaster 100%  Music 80%  SFX 100%\nHaptics On  Reduced Motion Off", controller.ToggleSettings);
            Button("MasterVolume", settings.transform, "MASTER", new Vector2(0.06f,0.12f), new Vector2(0.23f,0.28f), Wood, controller.CycleMasterVolume);
            Button("MusicVolume", settings.transform, "MUSIC", new Vector2(0.25f,0.12f), new Vector2(0.42f,0.28f), Wood, controller.CycleMusicVolume);
            Button("SfxVolume", settings.transform, "SFX", new Vector2(0.44f,0.12f), new Vector2(0.61f,0.28f), Wood, controller.CycleSfxVolume);
            Button("Haptics", settings.transform, "HAPTICS", new Vector2(0.63f,0.12f), new Vector2(0.8f,0.28f), Teal, controller.ToggleHaptics);
            Button("ReducedMotion", settings.transform, "MOTION", new Vector2(0.82f,0.12f), new Vector2(0.96f,0.28f), Teal, controller.ToggleReducedMotion);
            restart = Modal("RestartConfirm", safe.transform, "Restart this windmill?", controller.ConfirmRestart); Button("Cancel", restart.transform, "CANCEL", new Vector2(0.2f,0.1f), new Vector2(0.45f,0.3f), Teal, controller.CancelRestart);
            completion = Modal("CompletionPanel", safe.transform, "WINDMILL COMPLETE\n☆ ☆ ☆\nReplay", controller.ConfirmRestart);
            pause.SetActive(false); settings.SetActive(false); restart.SetActive(false); completion.SetActive(false);
        }

        static void BuildEventSystem(Transform parent)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); go.transform.SetParent(parent); go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        static GameObject Modal(string name, Transform parent, string text, UnityEngine.Events.UnityAction close)
        {
            var go = Panel(name, parent, new Vector2(0.12f, 0.32f), new Vector2(0.88f, 0.68f), new Color(0.15f, 0.07f, 0.03f, 0.96f));
            Label("Message", go.transform, text, 38, TextAnchor.MiddleCenter, Color.white, new Vector2(0.08f,0.28f), new Vector2(0.92f,0.9f));
            Button("Primary", go.transform, "OK", new Vector2(0.55f,0.1f), new Vector2(0.8f,0.3f), Teal, close); return go;
        }

        static GameObject Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        { var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false); SetAnchors(go.GetComponent<RectTransform>(), min, max); go.GetComponent<Image>().color = color; return go; }

        static Text Label(string name, Transform parent, string text, int size, TextAnchor alignment, Color color, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false); SetAnchors(go.GetComponent<RectTransform>(), min, max);
            var label = go.GetComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.text = text; label.fontSize = size; label.alignment = alignment; label.color = color; label.resizeTextForBestFit = true; label.raycastTarget = false; return label;
        }

        static Button Button(string name, Transform parent, string text, Vector2 min, Vector2 max, Color color, UnityEngine.Events.UnityAction action)
        {
            var go = Panel(name, parent, min, max, color); var button = go.AddComponent<Button>(); var label = Label("Text", go.transform, text, 28, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one); label.raycastTarget = false; UnityEventTools.AddPersistentListener(button.onClick, action); return button;
        }

        static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max) { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; }

        static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent); go.transform.localPosition = position; go.transform.localRotation = rotation ?? Quaternion.identity; go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>(); if (renderer != null) renderer.sharedMaterial = material; return go;
        }

        static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform) SetLayerRecursively(child.gameObject, layer);
        }
    }
}
