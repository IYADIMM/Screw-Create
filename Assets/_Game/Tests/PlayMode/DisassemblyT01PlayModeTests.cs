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
    public sealed class DisassemblyT01PlayModeTests
    {
        const string SceneName = "Gameplay_Disassembly_T01";
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
        public IEnumerator SceneInitializesWithExactMappingsAndActiveSnapshot()
        {
            Assert.That(Controller.InitializationError, Is.Empty);
            CollectionAssert.AreEquivalent(new[] { "S_1" }, Controller.Screws.Select(x => x.Id));
            CollectionAssert.AreEquivalent(new[] { "H_C1", "H_P1" }, Controller.Holes.Select(x => x.Id));
            CollectionAssert.AreEquivalent(new[] { "C_PLANK" }, Controller.Components.Select(x => x.Id));
            Assert.That(Controller.Session.Snapshot.Phase, Is.EqualTo(DisassemblyPhase.Active));
            Assert.That(GameObject.Find("SafeArea"), Is.Not.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator VisiblePointerTargetsCommitTheAuthoritativeMove()
        {
            var screw = Controller.Screws.Single();
            var destination = Controller.Holes.Single(x => x.Id == "H_P1");
            Assert.That(Controller.ProcessPointerPress(Controller.GameplayCamera.WorldToScreenPoint(screw.transform.position)), Is.True);
            Assert.That(Controller.SelectedScrewId, Is.EqualTo("S_1"));
            Assert.That(Controller.ProcessPointerPress(Controller.GameplayCamera.WorldToScreenPoint(destination.transform.position)), Is.True);
            Assert.That(Controller.Session.Snapshot.Phase, Is.EqualTo(DisassemblyPhase.Complete), "Logic commits before animation.");
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(Controller.CompletionShown, Is.True);
            Assert.That(Controller.Session.Snapshot.ActionCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator InvalidDestinationDoesNotMutateSnapshot()
        {
            Assert.That(Controller.SelectScrew("S_1"), Is.True);
            var before = Controller.Session.Snapshot.CanonicalKey();
            Assert.That(Controller.SelectDestination("H_C1"), Is.False);
            Assert.That(Controller.Session.Snapshot.CanonicalKey(), Is.EqualTo(before));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RapidDuplicateDestinationTapCommitsOnlyOnce()
        {
            Assert.That(Controller.SelectScrew("S_1"), Is.True);
            Assert.That(Controller.SelectDestination("H_P1"), Is.True);
            Assert.That(Controller.SelectDestination("H_P1"), Is.False);
            Assert.That(Controller.Session.Snapshot.ActionCount, Is.EqualTo(1));
            Assert.That(Controller.Session.UndoCount, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnimationInterruptionReconstructsCommittedSnapshot()
        {
            Controller.ReducedMotion = false;
            Controller.SelectScrew("S_1");
            Controller.SelectDestination("H_P1");
            yield return new WaitForSecondsRealtime(0.12f);
            Controller.InterruptAndReconcile();
            var destination = Controller.Holes.Single(x => x.Id == "H_P1").ScrewAnchor;
            Assert.That(Vector3.Distance(Controller.Screws.Single().transform.position, destination.position), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(Controller.Components.Single().transform.position, Controller.Components.Single().ReleasedPose.position), Is.LessThan(0.001f));
            Assert.That(Controller.CompletionShown, Is.True);
        }

        [UnityTest]
        public IEnumerator CompletionUndoAndRestartReconcileEveryView()
        {
            Controller.SelectScrew("S_1"); Controller.SelectDestination("H_P1");
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(Controller.Session.Snapshot.Phase, Is.EqualTo(DisassemblyPhase.Complete));
            Controller.Undo();
            Assert.That(Controller.Session.Snapshot.Phase, Is.EqualTo(DisassemblyPhase.Active));
            Assert.That(Controller.Session.Snapshot.ScrewLocations.Single().HoleId, Is.EqualTo("H_C1"));
            Assert.That(Controller.CompletionShown, Is.False);
            Controller.SelectScrew("S_1"); Controller.SelectDestination("H_P1");
            Controller.ConfirmRestart();
            Assert.That(Controller.Session.Snapshot.CanonicalKey(), Is.EqualTo("0|S_1=H_C1|released=|actions=0|revision=0"));
        }

        [UnityTest]
        public IEnumerator CameraFramingAndSafeAreaRemainValidAcrossPortraitRatios()
        {
            var safe = GameObject.Find("SafeArea").GetComponent<RectTransform>();
            foreach (var aspect in new[] { 9f/16f, 9f/19.5f, 9f/21f, 3f/4f })
            {
                Controller.GameplayCamera.aspect = aspect;
                yield return null;
                Assert.That(safe.anchorMin.x, Is.InRange(0f,1f)); Assert.That(safe.anchorMin.y, Is.InRange(0f,1f));
                Assert.That(safe.anchorMax.x, Is.InRange(0f,1f)); Assert.That(safe.anchorMax.y, Is.InRange(0f,1f));
                foreach (var renderer in Controller.BoardRoot.GetComponentsInChildren<Renderer>(true))
                {
                    var viewport = Controller.GameplayCamera.WorldToViewportPoint(renderer.bounds.center);
                    Assert.That(viewport.x, Is.InRange(-0.02f,1.02f), renderer.name + " x at " + aspect);
                    Assert.That(viewport.y, Is.InRange(0.08f,0.96f), renderer.name + " y at " + aspect);
                }
            }
        }

        [UnityTest]
        public IEnumerator CaptureM2RuntimeEvidence()
        {
            Controller.ReducedMotion = false;
            var output = Path.GetFullPath("Docs/M2Screenshots"); Directory.CreateDirectory(output);
            foreach (var capture in new[]
            {
                ("initial-9x16",540,960), ("initial-9x19_5",540,1170),
                ("initial-9x21",540,1260), ("initial-3x4",720,960)
            })
            {
                Controller.GameplayCamera.aspect = (float)capture.Item2 / capture.Item3; yield return null;
                Capture(Path.Combine(output,capture.Item1 + ".png"),capture.Item2,capture.Item3);
            }
            Controller.GameplayCamera.aspect = 9f/16f; yield return null;
            Controller.SelectScrew("S_1"); Capture(Path.Combine(output,"selected-9x16.png"),540,960);
            Controller.SelectDestination("H_P1");
            yield return new WaitForSecondsRealtime(0.68f); Capture(Path.Combine(output,"transferred-9x16.png"),540,960);
            yield return new WaitForSecondsRealtime(0.42f); Capture(Path.Combine(output,"released-9x16.png"),540,960);
            yield return new WaitForSecondsRealtime(0.35f); Capture(Path.Combine(output,"completion-9x16.png"),540,960);
            Controller.Undo(); yield return null; Capture(Path.Combine(output,"undo-9x16.png"),540,960);
            Assert.That(Directory.GetFiles(output,"*.png").Length, Is.GreaterThanOrEqualTo(9));
        }

        void Capture(string path, int width, int height)
        {
            var camera = Controller.GameplayCamera;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = canvases.Select(x => x.renderMode).ToArray(); var cameras = canvases.Select(x => x.worldCamera).ToArray();
            var target = new RenderTexture(width,height,24,RenderTextureFormat.ARGB32); var texture = new Texture2D(width,height,TextureFormat.RGB24,false);
            var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
            try
            {
                for(var i=0;i<canvases.Length;i++){ canvases[i].renderMode=RenderMode.ScreenSpaceCamera; canvases[i].worldCamera=camera; canvases[i].planeDistance=0.5f; }
                Canvas.ForceUpdateCanvases(); camera.targetTexture=target; camera.aspect=(float)width/height; RenderTexture.active=target; camera.Render();
                texture.ReadPixels(new Rect(0,0,width,height),0,0); texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=previousTarget; RenderTexture.active=previousActive;
                for(var i=0;i<canvases.Length;i++){ canvases[i].renderMode=modes[i]; canvases[i].worldCamera=cameras[i]; }
                Object.DestroyImmediate(texture); target.Release(); Object.DestroyImmediate(target);
            }
            Assert.That(new FileInfo(path).Length,Is.GreaterThan(4096),path);
        }
    }
}
