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
        readonly Dictionary<string, Vector3> _screwHomes = new Dictionary<string, Vector3>();

        public PuzzleEngine Engine => _engine;
        public bool IsPaused => _paused;
        public bool CompletionPlayed => _completionPlayed;

        void Awake()
        {
            BindViews();
            _definition = CreateDefinition();
            _save = new AtomicJsonSaveService(Path.Combine(Application.persistentDataPath, "phase2-windmill.json"));
            foreach (var screw in Screws) if (screw != null) _screwHomes[screw.Id] = screw.transform.localPosition;
            foreach (var part in Parts) if (part != null) { part.HomePosition = part.transform.position; part.HomeRotation = part.transform.rotation; }
            RestartImmediate();
        }

        void Start()
        {
            // Prefab instance links can settle after Awake when a scene is reloaded by the test runner.
            BindViews(); RestoreScrewTransforms(); RefreshVisuals();
        }

        void BindViews()
        {
            var screws = GetComponentsInChildren<ScrewView>(true); if (screws.Length > 0) Screws = screws.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            var parts = GetComponentsInChildren<PartView>(true); if (parts.Length > 0) Parts = parts.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            var sockets = GetComponentsInChildren<SocketView>(true); if (sockets.Length > 0) Sockets = sockets.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
        }

        void Update()
        {
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

        static bool PointerOverUi(int pointerId) => EventSystem.current != null && (pointerId < 0 ? EventSystem.current.IsPointerOverGameObject() : EventSystem.current.IsPointerOverGameObject(pointerId));

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
            if (Physics.Raycast(GameplayCamera.ScreenPointToRay(position), out var hit, 100f, ~0, QueryTriggerInteraction.Collide))
            {
                var socket = hit.collider.GetComponentInParent<SocketView>(); if (socket != null) { AssembleSelected(socket.Id); return; }
            }
            Status("That spot does not fit"); RefreshVisuals();
        }

        public bool ProcessPointerPress(Vector2 screenPosition, int pointerId = -1)
        {
            if (_paused || _busy || GameplayCamera == null || PointerOverUi(pointerId)) return false;
            if (!Physics.Raycast(GameplayCamera.ScreenPointToRay(screenPosition), out var hit, 100f, ~0, QueryTriggerInteraction.Collide)) return false;
            var screw = hit.collider.GetComponentInParent<ScrewView>();
            if (screw != null) { SelectScrew(screw); return true; }
            var part = hit.collider.GetComponentInParent<PartView>();
            if (part != null && _engine.CaptureSnapshot().TrayPartIds.Contains(part.Id)) { SelectTrayPart(part.Id); return true; }
            var socket = hit.collider.GetComponentInParent<SocketView>();
            if (socket != null && !string.IsNullOrEmpty(_selectedPart)) { AssembleSelected(socket.Id); return true; }
            return false;
        }

        public void SelectScrew(ScrewView view)
        {
            if (view == null || _busy) return;
            if (!_engine.CanRemoveScrew(view.Id, out var reason)) { Status(reason); StartCoroutine(Flash(view.TargetRenderer, BlockedMaterial)); return; }
            StartCoroutine(RemoveSequence(view));
        }

        IEnumerator RemoveSequence(ScrewView view)
        {
            _busy = true; Status("Loosening brass screw…");
            if (ScrewParticles != null) { ScrewParticles.transform.position = view.transform.position; ScrewParticles.Play(true); }
            var start = view.transform.localPosition; var duration = ReducedMotion ? 0.08f : 0.42f;
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                var t = Mathf.Clamp01(elapsed / duration); view.transform.localRotation = Quaternion.Euler(0f, t * 540f, 0f); view.transform.localPosition = start + Vector3.up * (0.32f * t); yield return null;
            }
            var result = _engine.TryRemoveScrew(view.Id);
            if (!result.Success) { view.transform.localPosition = start; Status(result.Reason); }
            _busy = false; RefreshVisuals();
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
            _selectedPart = string.Empty; RestoreScrewTransforms(); RefreshVisuals(); Status("Last move restored");
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
            RestoreScrewTransforms(); RefreshVisuals(); Status(recovered ? "Recovered previous save" : "Workbench restored");
        }

        void RestartImmediate()
        {
            _engine = new PuzzleEngine(_definition ?? CreateDefinition()); _selectedPart = string.Empty; _selectedRotation = 0; _completionPlayed = false; _hintedItem = string.Empty;
            if (CompletionPanel != null) CompletionPanel.SetActive(false); RestoreScrewTransforms(); RefreshVisuals(); Status("Remove a glowing brass screw");
        }

        void RestoreScrewTransforms()
        {
            foreach (var screw in Screws) if (screw != null && _screwHomes.TryGetValue(screw.Id, out var home)) { screw.transform.localPosition = home; screw.transform.localRotation = Quaternion.identity; }
        }

        void RefreshVisuals()
        {
            var state = _engine.CaptureSnapshot();
            foreach (var screw in Screws)
            {
                if (screw == null) continue; var removed = state.RemovedScrewIds.Contains(screw.Id); screw.gameObject.SetActive(!removed);
                if (!removed && screw.TargetRenderer != null) screw.TargetRenderer.sharedMaterial = screw.Id == _hintedItem ? HintMaterial : (_engine.CanRemoveScrew(screw.Id, out _) ? BrassMaterial : BlockedMaterial);
            }
            for (var i = 0; i < state.TrayPartIds.Length; i++) { var view = Parts.FirstOrDefault(x => x.Id == state.TrayPartIds[i]); if (view != null && view.TrayTarget != null) { view.gameObject.SetActive(true); view.transform.position = view.TrayTarget.position; view.transform.rotation = view.TrayTarget.rotation; } }
            foreach (var view in Parts)
            {
                if (view == null) continue;
                if (state.AssembledPartIds.Contains(view.Id) && view.SocketTarget != null) { view.gameObject.SetActive(true); view.transform.position = view.SocketTarget.position; view.transform.rotation = view.SocketTarget.rotation; }
                else if (!state.ReleasedPartIds.Contains(view.Id)) { view.gameObject.SetActive(true); view.transform.position = view.HomePosition; view.transform.rotation = view.HomeRotation; }
            }
            foreach (var socket in Sockets) if (socket != null && socket.TargetRenderer != null) socket.TargetRenderer.enabled = !string.IsNullOrEmpty(_selectedPart) && string.Equals(socket.Id, "Socket-" + _selectedPart, StringComparison.Ordinal);
            if (TrayText != null) TrayText.text = $"ACTIVE TRAY  {state.TrayPartIds.Length}/3";
            if (SeedText != null) SeedText.text = "DEV • WINDMILL • 0007";
            if (state.Phase == PuzzlePhase.Complete && !_completionPlayed) StartCoroutine(CompleteOnce());
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
