using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ScrewAndCreate.Disassembly;
using ScrewAndCreate.DisassemblyPresentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ScrewAndCreate.PlayMode.Tests
{
    public sealed class PremiumT01PlayModeTests
    {
        const string SceneName = "Gameplay_Disassembly_T01_Premium";
        DisassemblyT01Controller Controller => Object.FindFirstObjectByType<DisassemblyT01Controller>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            Assert.That(Controller, Is.Not.Null);
            Controller.ReducedMotion = true;
        }

        [UnityTest]
        public IEnumerator PremiumSceneRetainsAuthoritativeMappingsAndPresentationAssets()
        {
            Assert.That(Controller.InitializationError, Is.Empty);
            CollectionAssert.AreEquivalent(new[] { "S_1" }, Controller.Screws.Select(x => x.Id));
            CollectionAssert.AreEquivalent(new[] { "H_C1", "H_P1" }, Controller.Holes.Select(x => x.Id));
            CollectionAssert.AreEquivalent(new[] { "C_PLANK" }, Controller.Components.Select(x => x.Id));
            Assert.That(Controller.Session.Snapshot.Phase, Is.EqualTo(DisassemblyPhase.Active));
            Assert.That(GameObject.Find("PremiumBirchPlank"), Is.Not.Null);
            Assert.That(GameObject.Find("PremiumPhillipsScrew"), Is.Not.Null);
            Assert.That(GameObject.Find("PremiumBrassHole"), Is.Not.Null);
            Assert.That(Controller.EffectsSource, Is.Not.Null);
            Assert.That(Controller.ScrewSelectClip, Is.Not.Null);
            Assert.That(Controller.ScrewUnscrewClip, Is.Not.Null);
            Assert.That(Controller.ScrewInsertClip, Is.Not.Null);
            Assert.That(Controller.WoodReleaseClip, Is.Not.Null);
            Assert.That(Controller.LevelCompleteClip, Is.Not.Null);
            Assert.That(Controller.BoardRoot.GetComponentsInChildren<Renderer>(true)
                .SelectMany(x => x.sharedMaterials).Any(x => x != null && x.name == "Oak_Natural"), Is.True);
            Assert.That(Controller.BoardRoot.GetComponentsInChildren<Renderer>(true)
                .SelectMany(x => x.sharedMaterials).Any(x => x != null && x.name == "Birch_Honey"), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PremiumInteractionCompletesThenUndoAndRestartRemainExact()
        {
            var initial = Controller.Session.Snapshot.CanonicalKey();
            Assert.That(Controller.SelectScrew("S_1"), Is.True);
            Assert.That(Controller.SelectedScrewId, Is.EqualTo("S_1"));
            Assert.That(Controller.SelectDestination("H_P1"), Is.True);
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(Controller.Session.Snapshot.Phase, Is.EqualTo(DisassemblyPhase.Complete));
            Assert.That(Controller.CompletionShown, Is.True);
            Controller.Undo();
            Assert.That(Controller.Session.Snapshot.CanonicalKey(), Is.EqualTo(initial));
            Assert.That(Controller.CompletionShown, Is.False);
            Controller.SelectScrew("S_1");
            Controller.SelectDestination("H_P1");
            Controller.ConfirmRestart();
            Assert.That(Controller.Session.Snapshot.CanonicalKey(), Is.EqualTo(initial));
        }

        [UnityTest]
        public IEnumerator PremiumLayoutFitsRequiredPortraitRatios()
        {
            var safe = GameObject.Find("SafeArea").GetComponent<RectTransform>();
            foreach (var aspect in new[] { 9f / 16f, 9f / 19.5f, 9f / 21f, 3f / 4f })
            {
                Controller.GameplayCamera.aspect = aspect;
                yield return null;
                Assert.That(safe.anchorMin.x, Is.InRange(0f, 1f));
                Assert.That(safe.anchorMin.y, Is.InRange(0f, 1f));
                Assert.That(safe.anchorMax.x, Is.InRange(0f, 1f));
                Assert.That(safe.anchorMax.y, Is.InRange(0f, 1f));
                foreach (var renderer in Controller.BoardRoot.GetComponentsInChildren<Renderer>(true))
                {
                    var viewport = Controller.GameplayCamera.WorldToViewportPoint(renderer.bounds.center);
                    Assert.That(viewport.x, Is.InRange(-0.02f, 1.02f), renderer.name + " x at " + aspect);
                    Assert.That(viewport.y, Is.InRange(0.08f, 0.96f), renderer.name + " y at " + aspect);
                }
            }
        }

        [UnityTest]
        public IEnumerator CapturePremiumRuntimeEvidence()
        {
            Controller.ReducedMotion = false;
            var output = Path.GetFullPath("Docs/PremiumIntegrationScreenshots");
            Directory.CreateDirectory(output);
            foreach (var capture in new[]
            {
                ("after-initial-9x16", 540, 960),
                ("after-initial-9x19_5", 540, 1170),
                ("after-initial-9x21", 540, 1260),
                ("after-initial-3x4", 720, 960)
            })
            {
                Controller.GameplayCamera.aspect = (float)capture.Item2 / capture.Item3;
                yield return null;
                Capture(Path.Combine(output, capture.Item1 + ".png"), capture.Item2, capture.Item3);
            }
            Controller.GameplayCamera.aspect = 9f / 16f;
            yield return null;
            Controller.SelectScrew("S_1");
            Capture(Path.Combine(output, "after-selected-9x16.png"), 540, 960);
            Controller.SelectDestination("H_P1");
            yield return new WaitForSecondsRealtime(1.45f);
            Capture(Path.Combine(output, "after-completion-9x16.png"), 540, 960);
            Controller.Undo();
            yield return null;
            Capture(Path.Combine(output, "after-undo-9x16.png"), 540, 960);
            Assert.That(Directory.GetFiles(output, "after-*.png").Length, Is.GreaterThanOrEqualTo(7));
        }

        void Capture(string path, int width, int height)
        {
            var camera = Controller.GameplayCamera;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = canvases.Select(x => x.renderMode).ToArray();
            var cameras = canvases.Select(x => x.worldCamera).ToArray();
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                for (var i = 0; i < canvases.Length; i++)
                {
                    canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                    canvases[i].worldCamera = camera;
                    canvases[i].planeDistance = 0.5f;
                }
                Canvas.ForceUpdateCanvases();
                camera.targetTexture = target;
                camera.aspect = (float)width / height;
                RenderTexture.active = target;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                for (var i = 0; i < canvases.Length; i++)
                {
                    canvases[i].renderMode = modes[i];
                    canvases[i].worldCamera = cameras[i];
                }
                Object.DestroyImmediate(texture);
                target.Release();
                Object.DestroyImmediate(target);
            }
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(4096), path);
        }
    }
}
