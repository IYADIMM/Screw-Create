using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ScrewAndCreate.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ScrewAndCreate.PlayMode.Tests
{
    public sealed class Phase1RuntimeEvidenceTests
    {
        WindmillVerticalSliceController Controller => Object.FindFirstObjectByType<WindmillVerticalSliceController>();

        [UnityTest]
        public IEnumerator CaptureInitializedRuntimeStatesAtPortraitAspect()
        {
            SceneManager.LoadScene("Gameplay_Windmill_Graybox", LoadSceneMode.Single);
            yield return null;
            Assert.That(Controller, Is.Not.Null);
            Controller.ReducedMotion = true;
            Controller.GameplayCamera.aspect = 9f / 16f;
            yield return null;

            var output = Path.GetFullPath("Docs/Phase1Screenshots");
            Directory.CreateDirectory(output);
            yield return Capture(Path.Combine(output, "01-playmode-initial.png"));

            Controller.SelectScrew(Screw("S1"));
            yield return new WaitForSecondsRealtime(0.15f);
            yield return Capture(Path.Combine(output, "02-playmode-screw-removed.png"));
            yield return Capture(Path.Combine(output, "03-playmode-part-in-inventory.png"));

            var part = Part("P1");
            Assert.That(Controller.ProcessPointerPress(Controller.GameplayCamera.WorldToScreenPoint(part.transform.position)), Is.True);
            yield return Capture(Path.Combine(output, "04-playmode-part-selected.png"));

            Controller.Undo();
            yield return null;
            yield return Capture(Path.Combine(output, "05-playmode-after-undo.png"));
        }

        IEnumerator Capture(string path)
        {
            const int width = 540, height = 960;
            var camera = Controller.GameplayCamera;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = canvases.Select(x => x.renderMode).ToArray();
            var cameras = canvases.Select(x => x.worldCamera).ToArray();
            var particles = Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.None);
            var particleStates = particles.Select(x => x.enabled).ToArray();
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                for (var i = 0; i < particles.Length; i++) particles[i].enabled = false;
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
                for (var i = 0; i < particles.Length; i++) particles[i].enabled = particleStates[i];
                Object.DestroyImmediate(texture);
                target.Release();
                Object.DestroyImmediate(target);
            }
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(1024), "Runtime screenshot was unexpectedly small: " + path);
            yield return null;
        }

        static ScrewView Screw(string id) => Object.FindObjectsByType<ScrewView>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => x.Id == id);
        static PartView Part(string id) => Object.FindObjectsByType<PartView>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => x.Id == id);
    }
}
