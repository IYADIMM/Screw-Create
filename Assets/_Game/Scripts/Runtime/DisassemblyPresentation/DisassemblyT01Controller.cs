using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ScrewAndCreate.Disassembly;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ScrewAndCreate.DisassemblyPresentation
{
    public sealed class DisassemblyT01Controller : MonoBehaviour
    {
        public Camera GameplayCamera;
        public DisassemblyCameraFitter CameraFitter;
        public Transform BoardRoot;
        public DisassemblyScrewView[] Screws = Array.Empty<DisassemblyScrewView>();
        public DisassemblyHoleView[] Holes = Array.Empty<DisassemblyHoleView>();
        public DisassemblyComponentView[] Components = Array.Empty<DisassemblyComponentView>();
        public Text LevelText;
        public Text ProgressText;
        public Text InstructionText;
        public Text StatusText;
        public GameObject PausePanel;
        public GameObject RestartPanel;
        public GameObject CompletionPanel;
        public Material HoleIdleMaterial;
        public Material HoleLegalMaterial;
        public Material ScrewIdleMaterial;
        public Material ScrewSelectedMaterial;
        public Material InvalidMaterial;
        public AudioSource EffectsSource;
        public AudioClip ScrewSelectClip;
        public AudioClip ScrewUnscrewClip;
        public AudioClip ScrewInsertClip;
        public AudioClip WoodReleaseClip;
        public AudioClip LevelCompleteClip;
        [Range(0f, 1f)] public float EffectsVolume = 0.42f;
        public bool ReducedMotion;

        DisassemblyDefinition _definition;
        DisassemblySession _session;
        string _selectedScrewId = string.Empty;
        bool _busy;
        bool _paused;
        bool _completionShown;
        float _lastAspect = -1f;
        Bounds _boardBounds;
        bool _hasBoardBounds;

        public DisassemblySession Session => _session;
        public bool IsBusy => _busy;
        public bool IsPaused => _paused;
        public bool CompletionShown => _completionShown;
        public string SelectedScrewId => _selectedScrewId;
        public string InitializationError { get; private set; } = string.Empty;

        void Awake()
        {
            // Scene-authored arrays are the authoritative ID mapping. Unity can report an
            // incomplete prefab hierarchy during Awake when a scene is loaded by the test runner.
            if (Screws == null || Screws.Length == 0 || Holes == null || Holes.Length == 0 || Components == null || Components.Length == 0)
                BindViews();
            _definition = T01FirstReleaseDefinition.Create();
            var errors = ValidateMappings(_definition, Screws, Holes, Components);
            if (errors.Count > 0)
            {
                InitializationError = string.Join(" ", errors);
                Debug.LogError("Disassembly T01 mapping invalid: " + InitializationError, this);
                enabled = false;
                return;
            }

            foreach (var component in Components)
            {
                component.AttachedPosition = component.transform.position;
                component.AttachedRotation = component.transform.rotation;
            }
            CaptureBoardBounds();
            RestartImmediate();
        }

        void Start()
        {
            if (_session == null) return;
            FitCamera(true);
            ReconcilePresentation();
        }

        void Update()
        {
            FitCamera(false);
            if (_paused || _busy || GameplayCamera == null) return;
            if (TryReadPointerPress(out var position, out var pointerId)) ProcessPointerPress(position, pointerId);
        }

        void OnDisable()
        {
            if (_session == null) return;
            StopAllCoroutines();
            _busy = false;
            ReconcilePresentation();
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused || _session == null) return;
            InterruptAndReconcile();
        }

        void OnApplicationFocus(bool focused)
        {
            if (focused && _session != null) ReconcilePresentation();
        }

        void BindViews()
        {
            var screws = GetComponentsInChildren<DisassemblyScrewView>(true);
            var holes = GetComponentsInChildren<DisassemblyHoleView>(true);
            var components = GetComponentsInChildren<DisassemblyComponentView>(true);
            if (screws.Length > 0) Screws = screws.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            if (holes.Length > 0) Holes = holes.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            if (components.Length > 0) Components = components.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
        }

        public static IReadOnlyList<string> ValidateMappings(
            DisassemblyDefinition definition,
            IEnumerable<DisassemblyScrewView> screwViews,
            IEnumerable<DisassemblyHoleView> holeViews,
            IEnumerable<DisassemblyComponentView> componentViews)
        {
            var errors = new List<string>();
            ValidateIds("screw", definition.Screws.Select(x => x.Id), screwViews, x => x == null ? null : x.Id, errors);
            ValidateIds("hole", definition.Holes.Select(x => x.Id), holeViews, x => x == null ? null : x.Id, errors);
            ValidateIds("component", definition.Components.Select(x => x.Id), componentViews, x => x == null ? null : x.Id, errors);
            foreach (var view in holeViews ?? Array.Empty<DisassemblyHoleView>())
                if (view != null && view.ScrewAnchor == null) errors.Add($"Hole view {view.Id} has no screw anchor.");
            foreach (var view in componentViews ?? Array.Empty<DisassemblyComponentView>())
                if (view != null && view.ReleasedPose == null) errors.Add($"Component view {view.Id} has no released pose.");
            return errors;
        }

        static void ValidateIds<T>(string label, IEnumerable<string> expected, IEnumerable<T> views,
            Func<T, string> id, ICollection<string> errors)
        {
            var expectedSet = new HashSet<string>(expected, StringComparer.Ordinal);
            var actual = (views ?? Array.Empty<T>()).Select(id).ToArray();
            foreach (var duplicate in actual.Where(x => !string.IsNullOrWhiteSpace(x)).GroupBy(x => x, StringComparer.Ordinal).Where(x => x.Count() > 1))
                errors.Add($"Duplicate {label} view ID {duplicate.Key}.");
            foreach (var missing in expectedSet.Except(actual.Where(x => x != null), StringComparer.Ordinal))
                errors.Add($"Missing {label} view ID {missing}.");
            foreach (var extra in actual.Where(x => !string.IsNullOrWhiteSpace(x)).Except(expectedSet, StringComparer.Ordinal))
                errors.Add($"Unknown {label} view ID {extra}.");
            if (actual.Any(string.IsNullOrWhiteSpace)) errors.Add($"A {label} view has an empty ID.");
        }

        bool TryReadPointerPress(out Vector2 position, out int pointerId)
        {
            position = default;
            pointerId = -1;
            if (Touchscreen.current != null)
            {
                var activeTouches = Touchscreen.current.touches.Count(x => x.press.isPressed);
                if (activeTouches > 1) return false;
                if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    position = Touchscreen.current.primaryTouch.position.ReadValue();
                    pointerId = Touchscreen.current.primaryTouch.touchId.ReadValue();
                    return true;
                }
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                position = Mouse.current.position.ReadValue();
                return true;
            }
            return false;
        }

        public bool ProcessPointerPress(Vector2 screenPosition, int pointerId = -1)
        {
            if (_session == null || _paused || _busy || GameplayCamera == null || PointerBlockedByUi(screenPosition)) return false;
            var mask = LayerMask.GetMask("GameplayInteraction");
            var hits = Physics.RaycastAll(GameplayCamera.ScreenPointToRay(screenPosition), 100f, mask, QueryTriggerInteraction.Ignore)
                .OrderBy(x => x.distance).ThenBy(x => x.collider.name, StringComparer.Ordinal);
            foreach (var hit in hits)
            {
                var screw = hit.collider.GetComponentInParent<DisassemblyScrewView>();
                if (screw != null) return SelectScrew(screw.Id);
                var hole = hit.collider.GetComponentInParent<DisassemblyHoleView>();
                if (hole != null) return SelectDestination(hole.Id);
            }
            Status("Tap the screw, then the glowing hole.");
            return false;
        }

        bool PointerBlockedByUi(Vector2 position)
        {
            if ((PausePanel != null && PausePanel.activeInHierarchy) ||
                (RestartPanel != null && RestartPanel.activeInHierarchy) ||
                (CompletionPanel != null && CompletionPanel.activeInHierarchy)) return true;
            if (EventSystem.current == null) return false;
            var data = new PointerEventData(EventSystem.current) { position = position };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            return hits.Any(x => x.gameObject.GetComponentInParent<Selectable>() != null);
        }

        public bool SelectScrew(string screwId)
        {
            if (_session == null || _busy || _paused) return false;
            if (!_session.GetAccessibleScrewIds().Contains(screwId))
            {
                Status("That screw is not accessible.");
                FlashInvalid(screwId);
                return false;
            }
            var destinations = _session.GetLegalDestinationIds(screwId);
            if (destinations.Count == 0)
            {
                Status("No open destination for that screw.");
                return false;
            }
            _selectedScrewId = screwId;
            ApplySelectionVisuals();
            PlayEffect(ScrewSelectClip, 0.62f);
            Status("Now tap the glowing empty hole.");
            return true;
        }

        public bool SelectDestination(string holeId)
        {
            if (_session == null || _busy || _paused || string.IsNullOrEmpty(_selectedScrewId))
            {
                Status("Select the screw first.");
                return false;
            }
            if (!_session.GetLegalDestinationIds(_selectedScrewId).Contains(holeId))
            {
                Status("That hole cannot accept the screw.");
                FlashInvalidHole(holeId);
                return false;
            }

            var screwId = _selectedScrewId;
            var before = _session.Snapshot;
            var sourceHole = before.ScrewLocations.First(x => x.ScrewId == screwId).HoleId;
            var result = _session.TryTransferScrew(screwId, holeId);
            if (!result.Success)
            {
                Status(result.Reason);
                return false;
            }
            _selectedScrewId = string.Empty;
            _busy = true;
            StartCoroutine(TransferSequence(screwId, sourceHole, holeId, result.ReleasedComponentIds.ToArray()));
            return true;
        }

        IEnumerator TransferSequence(string screwId, string sourceHoleId, string destinationHoleId, string[] released)
        {
            var screw = Screw(screwId);
            var source = Hole(sourceHoleId).ScrewAnchor;
            var destination = Hole(destinationHoleId).ScrewAnchor;
            var startPosition = source.position;
            var startRotation = source.rotation;
            var withdraw = startPosition - source.up * 0.62f;
            var approach = destination.position - destination.up * 0.62f;

            Status("Loosening the screw…");
            PlayEffect(ScrewUnscrewClip, 0.48f);
            yield return Animate(0.28f, t =>
            {
                screw.transform.position = Vector3.Lerp(startPosition, withdraw, Smooth(t));
                screw.transform.rotation = startRotation * Quaternion.AngleAxis(t * 540f, Vector3.up);
            });
            Status("Moving to the empty hole…");
            yield return Animate(0.32f, t =>
            {
                var arc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.35f);
                screw.transform.position = Vector3.Lerp(withdraw, approach, Smooth(t)) + arc;
                screw.transform.rotation = Quaternion.Slerp(startRotation, destination.rotation, Smooth(t)) * Quaternion.AngleAxis(t * 360f, Vector3.up);
            });
            Status("Securing the screw…");
            PlayEffect(ScrewInsertClip, 0.46f);
            yield return Animate(0.24f, t =>
            {
                screw.transform.position = Vector3.Lerp(approach, destination.position, Smooth(t));
                screw.transform.rotation = destination.rotation * Quaternion.AngleAxis((1f - t) * 360f, Vector3.up);
            });

            foreach (var componentId in released)
            {
                var component = Component(componentId);
                var start = component.transform.position;
                var rotation = component.transform.rotation;
                Status("The wooden plank is free!");
                PlayEffect(WoodReleaseClip, 0.5f);
                yield return Animate(0.48f, t =>
                {
                    component.transform.position = Vector3.Lerp(start, component.ReleasedPose.position, Smooth(t));
                    component.transform.rotation = Quaternion.Slerp(rotation, component.ReleasedPose.rotation, Smooth(t));
                });
            }

            _busy = false;
            ReconcilePresentation();
            if (_session.Snapshot.Phase == DisassemblyPhase.Complete)
            {
                _completionShown = true;
                if (CompletionPanel != null) CompletionPanel.SetActive(true);
                PlayEffect(LevelCompleteClip, 0.42f);
                Status("Level complete!");
            }
        }

        IEnumerator Animate(float normalDuration, Action<float> apply)
        {
            var duration = ReducedMotion ? Mathf.Min(0.06f, normalDuration) : normalDuration;
            if (duration <= 0f) { apply(1f); yield break; }
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                apply(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            apply(1f);
        }

        static float Smooth(float t) => t * t * (3f - 2f * t);

        public void Undo()
        {
            if (_session == null) return;
            InterruptAndReconcile();
            if (!_session.TryUndo()) { Status("Nothing to undo."); return; }
            _selectedScrewId = string.Empty;
            _completionShown = false;
            ReconcilePresentation();
            Status("Move restored.");
        }

        public void RequestRestart()
        {
            if (RestartPanel != null) RestartPanel.SetActive(true);
            else RestartImmediate();
        }

        public void CancelRestart()
        {
            if (RestartPanel != null) RestartPanel.SetActive(false);
        }

        public void ConfirmRestart()
        {
            InterruptAndReconcile();
            RestartImmediate();
            if (RestartPanel != null) RestartPanel.SetActive(false);
        }

        public void TogglePause()
        {
            if (_session == null) return;
            if (!_paused) InterruptAndReconcile();
            _paused = !_paused;
            if (PausePanel != null) PausePanel.SetActive(_paused);
            Status(_paused ? "Paused" : "Move the screw to the empty hole.");
        }

        public void InterruptAndReconcile()
        {
            StopAllCoroutines();
            _busy = false;
            ReconcilePresentation();
        }

        void RestartImmediate()
        {
            StopAllCoroutines();
            _session = new DisassemblySession(_definition);
            _selectedScrewId = string.Empty;
            _busy = false;
            _paused = false;
            _completionShown = false;
            if (PausePanel != null) PausePanel.SetActive(false);
            if (CompletionPanel != null) CompletionPanel.SetActive(false);
            ReconcilePresentation();
            Status("Move the screw to the empty hole.");
        }

        public void ReconcilePresentation()
        {
            if (_session == null) return;
            var snapshot = _session.Snapshot;
            foreach (var location in snapshot.ScrewLocations)
            {
                var screw = Screw(location.ScrewId);
                var anchor = Hole(location.HoleId).ScrewAnchor;
                screw.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
                screw.gameObject.SetActive(true);
            }
            var released = new HashSet<string>(snapshot.ReleasedComponentIds, StringComparer.Ordinal);
            foreach (var component in Components)
            {
                if (released.Contains(component.Id))
                    component.transform.SetPositionAndRotation(component.ReleasedPose.position, component.ReleasedPose.rotation);
                else
                    component.transform.SetPositionAndRotation(component.AttachedPosition, component.AttachedRotation);
                component.gameObject.SetActive(true);
            }
            _completionShown = snapshot.Phase == DisassemblyPhase.Complete;
            if (CompletionPanel != null) CompletionPanel.SetActive(_completionShown);
            ApplySelectionVisuals();
            RefreshHud(snapshot);
            Physics.SyncTransforms();
        }

        void ApplySelectionVisuals()
        {
            var legal = string.IsNullOrEmpty(_selectedScrewId) || _session == null
                ? Array.Empty<string>()
                : _session.GetLegalDestinationIds(_selectedScrewId).ToArray();
            foreach (var hole in Holes)
                if (hole.RingRenderer != null) hole.RingRenderer.sharedMaterial = legal.Contains(hole.Id) ? HoleLegalMaterial : HoleIdleMaterial;
            foreach (var screw in Screws)
                if (screw.TargetRenderer != null) screw.TargetRenderer.sharedMaterial = screw.Id == _selectedScrewId ? ScrewSelectedMaterial : ScrewIdleMaterial;
        }

        void RefreshHud(DisassemblySnapshot snapshot)
        {
            if (LevelText != null) LevelText.text = "LEVEL 1  •  FIRST RELEASE";
            if (ProgressText != null) ProgressText.text = $"PARTS  {snapshot.ReleasedComponentIds.Length}/1";
            if (InstructionText != null) InstructionText.text = snapshot.Phase == DisassemblyPhase.Complete
                ? "The plank is released."
                : "Move the screw to the empty hole.";
        }

        void CaptureBoardBounds()
        {
            _hasBoardBounds = false;
            var renderers = (BoardRoot != null ? BoardRoot : transform).GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (!_hasBoardBounds) { _boardBounds = renderer.bounds; _hasBoardBounds = true; }
                else _boardBounds.Encapsulate(renderer.bounds);
            }
        }

        void FitCamera(bool force)
        {
            if (!_hasBoardBounds || CameraFitter == null || GameplayCamera == null) return;
            var aspect = GameplayCamera.aspect;
            if (!force && Mathf.Abs(aspect - _lastAspect) < 0.0001f) return;
            CameraFitter.Fit(_boardBounds, aspect);
            _lastAspect = aspect;
        }

        void FlashInvalid(string screwId)
        {
            var view = Screws.FirstOrDefault(x => x.Id == screwId);
            if (view != null && view.TargetRenderer != null) StartCoroutine(Flash(view.TargetRenderer));
        }

        void FlashInvalidHole(string holeId)
        {
            var view = Holes.FirstOrDefault(x => x.Id == holeId);
            if (view != null && view.RingRenderer != null) StartCoroutine(Flash(view.RingRenderer));
        }

        IEnumerator Flash(Renderer renderer)
        {
            var original = renderer.sharedMaterial;
            renderer.sharedMaterial = InvalidMaterial;
            yield return new WaitForSecondsRealtime(0.2f);
            if (renderer != null) renderer.sharedMaterial = original;
        }

        DisassemblyScrewView Screw(string id) => Screws.First(x => x.Id == id);
        DisassemblyHoleView Hole(string id) => Holes.First(x => x.Id == id);
        DisassemblyComponentView Component(string id) => Components.First(x => x.Id == id);
        void PlayEffect(AudioClip clip, float relativeVolume)
        {
            if (EffectsSource != null && clip != null)
                EffectsSource.PlayOneShot(clip, Mathf.Clamp01(EffectsVolume * relativeVolume));
        }
        void Status(string message) { if (StatusText != null) StatusText.text = message; }
    }
}
