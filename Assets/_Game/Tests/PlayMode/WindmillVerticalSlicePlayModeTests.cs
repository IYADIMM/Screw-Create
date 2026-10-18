using System.Collections;
using System.Linq;
using NUnit.Framework;
using ScrewAndCreate.Core;
using ScrewAndCreate.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ScrewAndCreate.PlayMode.Tests
{
    public sealed class WindmillVerticalSlicePlayModeTests
    {
        const string SceneName = "Gameplay_Windmill_Graybox";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            Controller.ReducedMotion = true;
        }

        WindmillVerticalSliceController Controller => Object.FindFirstObjectByType<WindmillVerticalSliceController>();

        [UnityTest]
        public IEnumerator SceneLoadsWithRequiredRootsAndSafeArea()
        {
            Assert.That(Controller, Is.Not.Null);
            foreach (var name in new[] { "CompositionRoot", "PuzzleModelController", "Interaction_InputSystem", "DisassemblyPresentation", "AssemblyPresentation", "WindmillMechanism", "Sockets_BlueprintTargets", "ActiveTray", "CameraAndLighting", "HUD", "AudioHapticsServices", "LocalSaveService", "DevelopmentDiagnostics" })
                Assert.That(GameObject.Find(name), Is.Not.Null, name);
            var safe = GameObject.Find("SafeArea").GetComponent<RectTransform>();
            Assert.That(safe.anchorMin.x, Is.InRange(0f, 1f)); Assert.That(safe.anchorMax.x, Is.InRange(0f, 1f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MouseInputRemovesOneExposedScrewAndReleasesPart()
        {
            var mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
            var screw = Screw("S2");
            var screen = Controller.GameplayCamera.WorldToScreenPoint(screw.transform.position);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen, buttons = 1 }); InputSystem.Update();
            Assert.That(mouse.leftButton.isPressed, Is.True);
            Assert.That(Controller.ProcessPointerPress(mouse.position.ReadValue()), Is.True);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen, buttons = 0 }); InputSystem.Update();
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(Controller.Engine.ActionCount, Is.EqualTo(1));
            Assert.That(Controller.Engine.CaptureSnapshot().TrayPartIds, Does.Contain("P2"));
        }

        [UnityTest]
        public IEnumerator TrayFillsRejectsOverflowThenAssemblyFreesSlot()
        {
            yield return Remove("S1"); yield return Remove("S2"); yield return Remove("S3");
            var before = Controller.Engine.CaptureSnapshot().SolverKey();
            Controller.SelectScrew(Screw("S4"));
            Assert.That(Controller.StatusText.text, Does.Contain("overflow"));
            Assert.That(Controller.Engine.CaptureSnapshot().SolverKey(), Is.EqualTo(before));
            Controller.SelectTrayPart("P1"); Controller.AssembleSelected("Socket-P1");
            Assert.That(Controller.Engine.TrayCount, Is.EqualTo(2));
            yield return Remove("S4");
            Assert.That(Controller.Engine.CaptureSnapshot().ReleasedPartIds, Does.Contain("P4"));
        }

        [UnityTest]
        public IEnumerator UndoRestartHintPauseAndSaveReloadFunction()
        {
            yield return Remove("S1");
            Controller.SelectTrayPart("P1"); Controller.AssembleSelected("Socket-P1");
            Controller.Undo(); Assert.That(Controller.Engine.CaptureSnapshot().AssembledPartIds, Is.Empty);
            Controller.Hint(); Assert.That(Controller.Engine.HintCount, Is.EqualTo(1));
            var saved = Controller.Engine.CaptureSnapshot().SolverKey();
            Controller.TogglePause(); Assert.That(Controller.IsPaused, Is.True); Controller.TogglePause();
            Controller.Save(); Controller.ConfirmRestart(); Assert.That(Controller.Engine.ActionCount, Is.Zero);
            Controller.Load(); Assert.That(Controller.Engine.CaptureSnapshot().SolverKey(), Is.EqualTo(saved));
            Controller.ConfirmRestart(); Assert.That(Controller.Engine.ActionCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CompletionFiresOnceAfterMixedLoop()
        {
            for (var i = 1; i <= 3; i++) yield return Remove($"S{i}");
            Controller.SelectTrayPart("P1"); Controller.AssembleSelected("Socket-P1");
            yield return Remove("S4");
            for (var i = 2; i <= 4; i++) { Controller.SelectTrayPart($"P{i}"); for (var r = 0; r < i - 1; r++) Controller.RotateSelected(); Controller.AssembleSelected($"Socket-P{i}"); }
            yield return null;
            Assert.That(Controller.Engine.Phase, Is.EqualTo(PuzzlePhase.Complete));
            Assert.That(Controller.CompletionPlayed, Is.True);
            var actions = Controller.Engine.ActionCount; yield return null; Assert.That(Controller.Engine.ActionCount, Is.EqualTo(actions));
        }

        IEnumerator Remove(string id)
        {
            Controller.SelectScrew(Screw(id));
            yield return new WaitForSecondsRealtime(0.15f);
        }

        static ScrewView Screw(string id)
        {
            var views = Object.FindObjectsByType<ScrewView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(views.Select(x => x.Id), Does.Contain(id), $"Screw view {id} is missing from the loaded scene.");
            return views.First(x => x.Id == id);
        }
    }
}
