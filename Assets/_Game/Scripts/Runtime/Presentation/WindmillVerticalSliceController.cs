using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScrewAndCreate.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ScrewAndCreate.Presentation
{
    /// <summary>Explicit composition root and deterministic presentation adapter for the temporary Windmill slice.</summary>
    public sealed class WindmillVerticalSliceController : MonoBehaviour
    {
        public Camera GameplayCamera;
        public PlayfieldFitter PlayfieldFitter;
        public ScrewView[] Screws = Array.Empty<ScrewView>();
        public PartView[] Parts = Array.Empty<PartView>();
        public SocketView[] Sockets = Array.Empty<SocketView>();
        public Transform BladesRoot;
        public Text StatusText;
        public Text TrayText;
        public Text SeedText;
        public GameObject PausePanel;
        public GameObject SettingsPanel;
        public GameObject RestartPanel;
        public GameObject CompletionPanel;
        public Material BrassMaterial;
        public Material BlockedMaterial;
        public Material SelectedMaterial;
        public Material HintMaterial;
        public Material GhostMaterial;
        public ParticleSystem ScrewParticles;
        public bool ReducedMotion;
        public bool HapticsEnabled = true;
        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float SfxVolume = 1f;

        PuzzleDefinition _definition;
        PuzzleEngine _engine;
        AtomicJsonSaveService _save;
        string _selectedPart = string.Empty;
        int _selectedRotation;
        bool _busy;
        bool _paused;
        bool _completionPlayed;
        string _hintedItem = string.Empty;
        readonly Dictionary<string, ScrewPose> _screwHomes = new Dictionary<string, ScrewPose>();
        readonly Dictionary<string, Material[]> _partHomeMaterials = new Dictionary<string, Material[]>();
        Bounds _essentialBounds;
        bool _hasEssentialBounds;
        float _fittedAspect = -1f;

        readonly struct ScrewPose
        {
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public ScrewPose(Vector3 position, Quaternion rotation) { Position = position; Rotation = rotation; }
        }

        public PuzzleEngine Engine => _engine;
        public bool IsPaused => _paused;
        public bool CompletionPlayed => _completionPlayed;

        void Awake()
        {
            BindViews();
            _definition = CreateDefinition();
            _save = new AtomicJsonSaveService(Path.Combine(Application.persistentDataPath, "phase2-windmill.json"));
            foreach (var screw in Screws) if (screw != null) _screwHomes[screw.Id] = new ScrewPose(screw.transform.localPosition, screw.transform.localRotation);
            foreach (var part in Parts) if (part != null)
            {
                part.HomePosition = part.transform.position; part.HomeRotation = part.transform.rotation;
                _partHomeMaterials[part.Id] = part.GetComponentsInChildren<Renderer>(true).Select(x => x.sharedMaterial).ToArray();
            }
            CaptureEssentialBounds();
            RestartImmediate();
        }

        void Start()
        {
            // Prefab instance links can settle after Awake when a scene is reloaded by the test runner.
            BindViews(); FitPlayfield(true); ReconcilePresentation();
        }

        void OnDisable()
        {
            if (_engine == null) return;
            StopAllCoroutines(); _busy = false; ReconcilePresentation();
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused || _engine == null) return;
            StopAllCoroutines(); _busy = false; ReconcilePresentation();
        }

        void OnApplicationFocus(bool focused)
        {
            if (focused && _engine != null) ReconcilePresentation();
        }

        void BindViews()
        {
            var screws = GetComponentsInChildren<ScrewView>(true); if (screws.Length > 0) Screws = screws.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            var parts = GetComponentsInChildren<PartView>(true); if (parts.Length > 0) Parts = parts.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            var sockets = GetComponentsInChildren<SocketView>(true); if (sockets.Length > 0) Sockets = sockets.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
        }

        void Update()
        {
            FitPlayfield(false);
            if (_completionPlayed && BladesRoot != null) BladesRoot.Rotate(0f, 0f, 28f * Time.unscaledDeltaTime, Space.Self);
            if (_paused || _busy || GameplayCamera == null) return;
            if (TryReadPointerPress(out var screenPosition, out var pointerId)) ProcessPointerPress(screenPosition, pointerId);
            UpdateAssemblyDrag();
            if (!string.IsNullOrEmpty(_selectedPart) && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) RotateSelected();
        }

        bool TryReadPointerPress(out Vector2 position, out int pointerId)
        {
            position = default; pointerId = -1;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            { position = Touchscreen.current.primaryTouch.position.ReadValue(); pointerId = Touchscreen.current.primaryTouch.touchId.ReadValue(); return true; }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            { position = Mouse.current.position.ReadValue(); return true; }
            return false;
        }

        bool PointerBlockedByInteractiveUi(Vector2 screenPosition)
        {
            if ((PausePanel != null && PausePanel.activeInHierarchy) || (SettingsPanel != null && SettingsPanel.activeInHierarchy) ||
                (RestartPanel != null && RestartPanel.activeInHierarchy) || (CompletionPanel != null && CompletionPanel.activeInHierarchy)) return true;
            if (EventSystem.current == null) return false;
            var data = new PointerEventData(EventSystem.current) { position = screenPosition };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            return hits.Any(hit => hit.gameObject.GetComponentInParent<Selectable>() != null);
        }

        void UpdateAssemblyDrag()
        {
            if (string.IsNullOrEmpty(_selectedPart)) return;
            var pressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
            var released = Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
            var position = Mouse.current != null ? Mouse.current.position.ReadValue() : default;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) { pressed = true; position = Touchscreen.current.primaryTouch.position.ReadValue(); }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) { released = true; position = Touchscreen.current.primaryTouch.position.ReadValue(); }
            var view = Parts.FirstOrDefault(x => x != null && x.Id == _selectedPart);
            if (pressed && view != null)
            {
                var ray = GameplayCamera.ScreenPointToRay(position); var plane = new Plane(Vector3.forward, new Vector3(0, 0, view.transform.position.z));
                if (plane.Raycast(ray, out var distance)) view.transform.position = ray.GetPoint(distance);
            }
            if (!released) return;
            var socket = InteractionHits(position).Select(hit => hit.collider.GetComponentInParent<SocketView>()).FirstOrDefault(x => x != null);
            if (socket != null) { AssembleSelected(socket.Id); return; }
            Status("That spot does not fit"); RefreshVisuals();
        }

        public bool ProcessPointerPress(Vector2 screenPosition, int pointerId = -1)
        {
            if (_paused || _busy || GameplayCamera == null || PointerBlockedByInteractiveUi(screenPosition)) return false;
            var trayIds = _engine.CaptureSnapshot().TrayPartIds;
            foreach (var hit in InteractionHits(screenPosition))
            {
                var part = hit.collider.GetComponentInParent<PartView>();
                if (part != null && trayIds.Contains(part.Id)) { SelectTrayPart(part.Id); return true; }
                var screw = hit.collider.GetComponentInParent<ScrewView>();
                if (screw != null) { SelectScrew(screw); return true; }
                var socket = hit.collider.GetComponentInParent<SocketView>();
                if (socket != null && !string.IsNullOrEmpty(_selectedPart)) { AssembleSelected(socket.Id); return true; }
            }
            return false;
        }

        RaycastHit[] InteractionHits(Vector2 screenPosition)
        {
            var mask = LayerMask.GetMask("GameplayInteraction");
            return Physics.RaycastAll(GameplayCamera.ScreenPointToRay(screenPosition), 100f, mask, QueryTriggerInteraction.Ignore)
                .OrderBy(hit => hit.distance).ThenBy(hit => hit.collider.name, StringComparer.Ordinal).ToArray();
        }

        public void SelectScrew(ScrewView view)
        {
            if (view == null || _busy) return;
            if (!_engine.CanRemoveScrew(view.Id, out var reason)) { Status(reason); StartCoroutine(Flash(view.TargetRenderer, BlockedMaterial)); return; }
            StartCoroutine(RemoveSequence(view));
        }

        IEnumerator RemoveSequence(ScrewView view)
        {
            var result = _engine.TryRemoveScrew(view.Id);
            if (!result.Success) { Status(result.Reason); yield break; }
            _busy = true; Status("Loosening brass screw…");
            if (ScrewParticles != null) { ScrewParticles.transform.position = view.transform.position; ScrewParticles.Play(true); }
            var pose = _screwHomes[view.Id]; var duration = ReducedMotion ? 0.08f : 0.42f;
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                var t = Mathf.Clamp01(elapsed / duration);
                view.transform.localRotation = pose.Rotation * Quaternion.AngleAxis(t * 540f, Vector3.up);
                view.transform.localPosition = pose.Position - (pose.Rotation * Vector3.up) * (0.32f * t);
                yield return null;
            }
            _busy = false; ReconcilePresentation();
        }

        public void SelectTrayPart(string partId)
        {
            if (!_engine.CaptureSnapshot().TrayPartIds.Contains(partId)) return;
            _selectedPart = partId; _selectedRotation = 0; _engine.SetInteractionMode(InteractionMode.Assembly); Status($"Place {partId} in its teal blueprint"); RefreshVisuals();
        }

        public void RotateSelected()
        {
            if (string.IsNullOrEmpty(_selectedPart)) return;
            _selectedRotation = (_selectedRotation + 90) % 360;
            var view = Parts.FirstOrDefault(x => x.Id == _selectedPart); if (view != null) view.transform.rotation = Quaternion.Euler(0f, 0f, _selectedRotation);
            Status($"Rotation {_selectedRotation}°");
        }

        public void AssembleSelected(string socketId)
        {
            if (string.IsNullOrEmpty(_selectedPart)) return;
            var result = _engine.TryAssemblePart(_selectedPart, socketId, _selectedRotation);
            if (!result.Success) { Status(result.Reason); return; }
            _selectedPart = string.Empty; _engine.SetInteractionMode(InteractionMode.Disassembly); Status("Perfect fit — tray slot freed"); RefreshVisuals();
        }

        public void Undo()
        {
            if (_busy || !_engine.TryUndo()) { Status("Nothing to undo"); return; }
            _selectedPart = string.Empty; _selectedRotation = 0; _hintedItem = string.Empty; ReconcilePresentation(); Status("Last move restored");
        }

        public void Hint()
        {
            if (!new PuzzleSolver().TryGetHint(_definition, _engine.CaptureSnapshot(), out var hint)) { Status("No legal hint available"); return; }
            _engine.RegisterHintUse(); _hintedItem = hint.ItemId;
            Status(hint.Kind == PuzzleActionKind.RemoveScrew ? $"Try screw {hint.ItemId}" : $"Place {hint.ItemId} next"); RefreshVisuals();
        }

        public void TogglePause() { _paused = !_paused; if (PausePanel != null) PausePanel.SetActive(_paused); Status(_paused ? "Paused" : "Back to the workbench"); }
        public void ToggleSettings() { if (SettingsPanel != null) SettingsPanel.SetActive(!SettingsPanel.activeSelf); }
        public void ToggleHaptics() { HapticsEnabled = !HapticsEnabled; Status(HapticsEnabled ? "Haptics enabled" : "Haptics disabled"); }
        public void ToggleReducedMotion() { ReducedMotion = !ReducedMotion; Status(ReducedMotion ? "Reduced motion enabled" : "Full motion enabled"); }
        public void CycleMasterVolume() { MasterVolume = MasterVolume > 0.75f ? 0.5f : MasterVolume > 0.25f ? 0f : 1f; AudioListener.volume = MasterVolume; Status($"Master volume {MasterVolume * 100:0}%"); }
        public void CycleMusicVolume() { MusicVolume = MusicVolume > 0.6f ? 0.3f : MusicVolume > 0.1f ? 0f : 0.8f; Status($"Music volume {MusicVolume * 100:0}%"); }
        public void CycleSfxVolume() { SfxVolume = SfxVolume > 0.75f ? 0.5f : SfxVolume > 0.25f ? 0f : 1f; Status($"SFX volume {SfxVolume * 100:0}%"); }
        public void RequestRestart() { if (RestartPanel != null) RestartPanel.SetActive(true); else RestartImmediate(); }
        public void CancelRestart() { if (RestartPanel != null) RestartPanel.SetActive(false); }
        public void ConfirmRestart() { RestartImmediate(); if (RestartPanel != null) RestartPanel.SetActive(false); }

        public void Save()
        {
            _save.Save(new GameSaveData { ActiveAuthoredLevelId = "MW-01", ActivePuzzleIdentity = _definition.Id, ActiveDifficulty = DifficultyBand.Builder, ActiveInteractionMode = _engine.Mode, ActivePuzzle = _engine.CaptureSnapshot(), ActiveSeedCode = SeedCode.Encode(new SeedIdentity(2, 7UL, CreationFamily.MeadowWindmill, DifficultyBand.Builder, SeedCode.CatalogHashFor(DeterministicGenerator.ContentCatalogId))), HapticsEnabled = HapticsEnabled, ReducedCameraMotion = ReducedMotion, MasterVolume = MasterVolume, MusicVolume = MusicVolume, SfxVolume = SfxVolume });
            Status("Workbench saved locally");
        }

        public void Load()
        {
            if (!_save.TryLoad(out var data, out var recovered) || data.ActivePuzzle == null) { Status("No valid saved workbench"); return; }
            if (!_engine.TryRestoreSnapshot(data.ActivePuzzle, out var reason)) { Status("Saved workbench rejected: " + reason); return; }
            HapticsEnabled = data.HapticsEnabled; ReducedMotion = data.ReducedCameraMotion; MasterVolume = data.MasterVolume; MusicVolume = data.MusicVolume; SfxVolume = data.SfxVolume; AudioListener.volume = MasterVolume;
            _selectedPart = string.Empty; _selectedRotation = 0; _hintedItem = string.Empty; ReconcilePresentation(); Status(recovered ? "Recovered previous save" : "Workbench restored");
        }

        void RestartImmediate()
        {
            _engine = new PuzzleEngine(_definition ?? CreateDefinition()); _selectedPart = string.Empty; _selectedRotation = 0; _completionPlayed = false; _hintedItem = string.Empty;
            if (CompletionPanel != null) CompletionPanel.SetActive(false); ReconcilePresentation(); Status("Remove a glowing brass screw");
        }

        void RestoreScrewTransforms()
        {
            foreach (var screw in Screws) if (screw != null && _screwHomes.TryGetValue(screw.Id, out var home)) { screw.transform.localPosition = home.Position; screw.transform.localRotation = home.Rotation; }
        }

        public void ReconcilePresentation() { RestoreScrewTransforms(); RefreshVisuals(); }

        void RefreshVisuals()
        {
            var state = _engine.CaptureSnapshot();
            foreach (var screw in Screws)
            {
                if (screw == null) continue; var removed = state.RemovedScrewIds.Contains(screw.Id); screw.gameObject.SetActive(!removed);
                if (!removed && screw.TargetRenderer != null) screw.TargetRenderer.sharedMaterial = screw.Id == _hintedItem ? HintMaterial : (_engine.CanRemoveScrew(screw.Id, out _) ? BrassMaterial : BlockedMaterial);
            }
            foreach (var view in Parts)
            {
                if (view == null) continue;
                var trayIndex = Array.IndexOf(state.TrayPartIds, view.Id);
                if (state.AssembledPartIds.Contains(view.Id) && view.SocketTarget != null) { view.gameObject.SetActive(true); view.transform.position = view.SocketTarget.position; view.transform.rotation = view.SocketTarget.rotation; }
                else if (trayIndex >= 0)
                {
                    view.gameObject.SetActive(true); view.transform.position = TrayWorldPosition(trayIndex);
                    view.transform.rotation = view.Id == _selectedPart ? Quaternion.Euler(0f, 0f, _selectedRotation) : Quaternion.identity;
                }
                else if (!state.ReleasedPartIds.Contains(view.Id)) { view.gameObject.SetActive(true); view.transform.position = view.HomePosition; view.transform.rotation = view.HomeRotation; }
                else view.gameObject.SetActive(false);
                ApplyPartMaterials(view, view.Id == _selectedPart ? SelectedMaterial : null);
            }
            foreach (var socket in Sockets) if (socket != null && socket.TargetRenderer != null) socket.TargetRenderer.enabled = !string.IsNullOrEmpty(_selectedPart) && string.Equals(socket.Id, "Socket-" + _selectedPart, StringComparison.Ordinal);
            if (TrayText != null) TrayText.text = $"ACTIVE TRAY  {state.TrayPartIds.Length}/3";
            if (SeedText != null) SeedText.text = "DEV • WINDMILL • 0007";
            Physics.SyncTransforms();
            if (state.Phase == PuzzlePhase.Complete && !_completionPlayed) StartCoroutine(CompleteOnce());
        }

        Vector3 TrayWorldPosition(int index)
        {
            var x = new[] { 0.28f, 0.5f, 0.72f }[Mathf.Clamp(index, 0, 2)];
            var ray = GameplayCamera.ViewportPointToRay(new Vector3(x, 0.23f, 0f));
            var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, -0.55f));
            return plane.Raycast(ray, out var distance) ? ray.GetPoint(distance) : Vector3.zero;
        }

        void ApplyPartMaterials(PartView view, Material selected)
        {
            var renderers = view.GetComponentsInChildren<Renderer>(true);
            if (!_partHomeMaterials.TryGetValue(view.Id, out var originals)) return;
            for (var i = 0; i < renderers.Length && i < originals.Length; i++) renderers[i].sharedMaterial = selected != null ? selected : originals[i];
        }

        void CaptureEssentialBounds()
        {
            _hasEssentialBounds = false;
            foreach (var renderer in Parts.Where(x => x != null).SelectMany(x => x.GetComponentsInChildren<Renderer>(true)).Concat(Screws.Where(x => x != null).SelectMany(x => x.GetComponentsInChildren<Renderer>(true))))
            {
                if (!_hasEssentialBounds) { _essentialBounds = renderer.bounds; _hasEssentialBounds = true; }
                else _essentialBounds.Encapsulate(renderer.bounds);
            }
        }

        void FitPlayfield(bool force)
        {
            if (GameplayCamera == null || PlayfieldFitter == null || !_hasEssentialBounds) return;
            var aspect = GameplayCamera.aspect;
            if (!force && Mathf.Abs(aspect - _fittedAspect) < 0.0001f) return;
            PlayfieldFitter.Fit(_essentialBounds, aspect); _fittedAspect = aspect;
            if (_engine != null) RefreshVisuals();
        }

        IEnumerator CompleteOnce()
        {
            _completionPlayed = true; Status("Windmill complete!"); if (CompletionPanel != null) CompletionPanel.SetActive(true);
            var start = GameplayCamera.transform.position; if (!ReducedMotion) for (var t = 0f; t < 0.5f; t += Time.unscaledDeltaTime) { GameplayCamera.transform.position = start + Vector3.forward * (Mathf.Sin(t * Mathf.PI * 2f) * 0.025f); yield return null; }
            GameplayCamera.transform.position = start;
        }

        IEnumerator Flash(Renderer renderer, Material material)
        {
            if (renderer == null || material == null) yield break; var original = renderer.sharedMaterial; renderer.sharedMaterial = material; yield return new WaitForSecondsRealtime(0.25f); renderer.sharedMaterial = original;
        }

        void Status(string message) { if (StatusText != null) StatusText.text = message; }

        static PuzzleDefinition CreateDefinition()
        {
            var screws = Enumerable.Range(1, 4).Select(i => new ScrewDefinition { Id = $"S{i}" }).ToArray();
            var parts = Enumerable.Range(1, 4).Select(i => new PartDefinition { Id = $"P{i}", CompatibilityGroup = "windmill-blade", TargetSocketId = $"Socket-P{i}", SecuringScrewIds = new[] { $"S{i}" }, AssemblyDependencyPartIds = i == 1 ? Array.Empty<string>() : new[] { $"P{i - 1}" }, AllowedRotations = new[] { (i - 1) * 90 }, RotationToleranceDegrees = 14f }).ToArray();
            var sockets = Enumerable.Range(1, 4).Select(i => new SocketDefinition { Id = $"Socket-P{i}", VisualTargetId = $"Ghost-P{i}", AcceptedCompatibilityGroups = new[] { "windmill-blade" }, AllowedPartIds = new[] { $"P{i}" }, AssemblyDependencyPartIds = i == 1 ? Array.Empty<string>() : new[] { $"P{i - 1}" }, AllowedRotations = new[] { (i - 1) * 90 }, RotationToleranceDegrees = 14f }).ToArray();
            return new PuzzleDefinition { Id = "MW-01-PHASE2", TrayCapacity = 3, Screws = screws, Parts = parts, Sockets = sockets };
        }
    }
}
