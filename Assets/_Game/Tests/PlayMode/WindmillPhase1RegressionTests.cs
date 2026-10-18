using System.Collections;
using System.Linq;
using NUnit.Framework;
using ScrewAndCreate.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ScrewAndCreate.PlayMode.Tests
{
    public sealed class WindmillPhase1RegressionTests
    {
        const string SceneName = "Gameplay_Windmill_Graybox";

        WindmillVerticalSliceController Controller => Object.FindFirstObjectByType<WindmillVerticalSliceController>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            Assert.That(Controller, Is.Not.Null);
            Controller.ReducedMotion = true;
        }

        [UnityTest]
        public IEnumerator ScrewHeadFacesCameraAndHomePoseSurvivesRestart()
        {
            var screw = Screw("S1");
            Assert.That(Mathf.Abs(Vector3.Dot(screw.transform.up.normalized, Controller.GameplayCamera.transform.forward)), Is.GreaterThan(0.95f));
            var home = screw.transform.localRotation;
            yield return Remove("S1");
            Controller.ConfirmRestart();
            Assert.That(Quaternion.Angle(screw.transform.localRotation, home), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator ScrewPoseSurvivesUndoLoadAndRepeatedInteractions()
        {
            var screw = Screw("S1");
            var home = screw.transform.localRotation;
            Controller.Save();
            for (var iteration = 0; iteration < 2; iteration++)
            {
                yield return Remove("S1");
                Controller.Undo();
                Assert.That(screw.gameObject.activeSelf, Is.True);
                Assert.That(Quaternion.Angle(screw.transform.localRotation, home), Is.LessThan(0.01f));
            }
            yield return Remove("S1");
            Controller.Load();
            Assert.That(screw.gameObject.activeSelf, Is.True);
            Assert.That(Quaternion.Angle(screw.transform.localRotation, home), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator ScrewAnimationRotatesAroundAuthoredLocalAxis()
        {
            Controller.ReducedMotion = false;
            var screw = Screw("S1");
            var homeRotation = screw.transform.localRotation;
            var homeAxis = homeRotation * Vector3.up;
            Controller.SelectScrew(screw);
            yield return null; yield return null;
            Assert.That(Quaternion.Angle(screw.transform.localRotation, homeRotation), Is.GreaterThan(0.1f));
            Assert.That(Vector3.Dot((screw.transform.localRotation * Vector3.up).normalized, homeAxis.normalized), Is.GreaterThan(0.999f));
        }

        [UnityTest]
        public IEnumerator RecoveredPartIsVisibleAndSelectableThroughPointerPath()
        {
            yield return Remove("S1");
            var part = Part("P1");
            var viewport = Controller.GameplayCamera.WorldToViewportPoint(part.transform.position);
            Assert.That(viewport.x, Is.InRange(0.08f, 0.92f));
            Assert.That(viewport.y, Is.InRange(0.21f, 0.72f), "Occupied tray parts must render above the opaque HUD and below the playfield.");

            var mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
            var screen = Controller.GameplayCamera.WorldToScreenPoint(part.transform.position);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen, buttons = 1 });
            InputSystem.Update();
            Assert.That(Controller.ProcessPointerPress(mouse.position.ReadValue()), Is.True, "The real pointer route must reach the visible tray part through decorative UI.");
            Assert.That(part.GetComponentInChildren<Renderer>().sharedMaterial, Is.EqualTo(Controller.SelectedMaterial));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen, buttons = 0 }); InputSystem.Update();
        }

        [UnityTest]
        public IEnumerator EmptyTrayPositionIsInertAndDecorationCannotOccludeStoredPart()
        {
            yield return Remove("S1");
            var part = Part("P1");
            var screen = Controller.GameplayCamera.WorldToScreenPoint(part.transform.position);
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "TEMP_TestDecorationBlocker";
            blocker.transform.position = part.transform.position - Vector3.forward;
            blocker.transform.localScale = Vector3.one * 1.5f;
            Assert.That(Controller.ProcessPointerPress(screen), Is.True, "Default-layer decoration must not intercept the interaction layer.");
            Object.Destroy(blocker);

            Controller.ConfirmRestart();
            var emptyRay = Controller.GameplayCamera.ViewportPointToRay(new Vector3(0.72f, 0.23f, 0f));
            var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, -0.55f));
            Assert.That(plane.Raycast(emptyRay, out var distance), Is.True);
            var emptyScreen = Controller.GameplayCamera.WorldToScreenPoint(emptyRay.GetPoint(distance));
            Assert.That(Controller.ProcessPointerPress(emptyScreen), Is.False);
        }

        [UnityTest]
        public IEnumerator SelectedPartRotationSurvivesRefresh()
        {
            yield return Remove("S1");
            Controller.SelectTrayPart("P1");
            Controller.RotateSelected();
            var expected = Part("P1").transform.rotation;
            Controller.Hint();
            Assert.That(Quaternion.Angle(Part("P1").transform.rotation, expected), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator EssentialInteractionPointsFitSupportedAspectRatios()
        {
            yield return Remove("S1");
            var camera = Controller.GameplayCamera;
            var originalAspect = camera.aspect;
            foreach (var aspect in new[] { 9f / 16f, 9f / 19.5f, 9f / 21f, 3f / 4f })
            {
                camera.aspect = aspect;
                yield return null;
                foreach (var view in Object.FindObjectsByType<ScrewView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    AssertInside(view.transform.position, $"{view.Id} at aspect {aspect:0.###}");
                AssertInside(Part("P1").transform.position, $"tray P1 at aspect {aspect:0.###}", 0.2f);
            }
            camera.aspect = originalAspect;
        }

        [UnityTest]
        public IEnumerator DeveloperControlsAreGatedOffByDefault()
        {
            foreach (var name in new[] { "SaveButton", "LoadButton", "SeedDisplay" })
            {
                var target = FindEvenIfInactive(name);
                Assert.That(target, Is.Not.Null, name + " should remain available in the gated developer hierarchy.");
                Assert.That(target.activeInHierarchy, Is.False, name + " must not be exposed on the ordinary player HUD.");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator InterruptedRemovalCommitsOnceAndReconciles()
        {
            Controller.ReducedMotion = false;
            var screw = Screw("S1");
            Controller.SelectScrew(screw);
            yield return null;
            Controller.enabled = false;
            yield return null;
            Controller.enabled = true;
            yield return null;
            Assert.That(Controller.Engine.ActionCount, Is.EqualTo(1));
            Assert.That(Controller.Engine.CaptureSnapshot().RemovedScrewIds, Does.Contain("S1"));
            Assert.That(screw.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator ApplicationPauseDuringRemovalReconcilesAuthoritativeState()
        {
            Controller.ReducedMotion = false;
            var screw = Screw("S1");
            Controller.SelectScrew(screw);
            yield return null;
            Controller.SendMessage("OnApplicationPause", true);
            yield return null;
            Assert.That(Controller.Engine.ActionCount, Is.EqualTo(1));
            Assert.That(Controller.Engine.CaptureSnapshot().RemovedScrewIds, Does.Contain("S1"));
            Assert.That(screw.gameObject.activeSelf, Is.False);
        }

        IEnumerator Remove(string id)
        {
            Controller.SelectScrew(Screw(id));
            yield return new WaitForSecondsRealtime(0.15f);
        }

        void AssertInside(Vector3 world, string message, float minimumY = 0.2f)
        {
            var viewport = Controller.GameplayCamera.WorldToViewportPoint(world);
            Assert.That(viewport.z, Is.GreaterThan(0f), message);
            Assert.That(viewport.x, Is.InRange(0.06f, 0.94f), message);
            Assert.That(viewport.y, Is.InRange(minimumY, 0.84f), message);
        }

        static ScrewView Screw(string id) => Object.FindObjectsByType<ScrewView>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => x.Id == id);
        static PartView Part(string id) => Object.FindObjectsByType<PartView>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => x.Id == id);

        static GameObject FindEvenIfInactive(string name)
        {
            return Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(x => x.scene.IsValid() && x.name == name);
        }
    }
}
