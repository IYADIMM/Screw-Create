using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScrewAndCreate.Core;
using UnityEditor;
using UnityEngine;

namespace ScrewAndCreate.Editor.VerticalSlice
{
    public static class Phase2SeedBatchReport
    {
        [Serializable]
        sealed class Report
        {
            public string GeneratedUtc = string.Empty;
            public int GeneratorVersion;
            public string ContentCatalogId = string.Empty;
            public int Requested;
            public int Accepted;
            public int Rejected;
            public int Failures;
            public double AverageSolverStates;
            public int MaximumSolverStates;
            public int ReproducibilityFailures;
            public string[] RejectionReasons = Array.Empty<string>();
            public string Coverage = string.Empty;
        }

        public static void RunFromCommandLine()
        {
            var generator = new DeterministicGenerator(); var accepted = 0; var failures = 0; var reproducibility = 0; long totalStates = 0; var maxStates = 0; var reasons = new List<string>();
            for (ulong seed = 0; seed < 1000; seed++)
            {
                var family = (CreationFamily)(seed % 3); var band = (DifficultyBand)((seed / 3) % 5);
                var first = generator.Generate(seed, family, band); var second = generator.Generate(seed, family, band);
                reasons.AddRange(first.RejectionReasons);
                if (!first.Verification.Solved) { failures++; continue; }
                accepted++; totalStates += first.Verification.StatesVisited; maxStates = Math.Max(maxStates, first.Verification.StatesVisited);
                if (Signature(first) != Signature(second)) reproducibility++;
            }
            var report = new Report
            {
                GeneratedUtc = DateTime.UtcNow.ToString("O"), GeneratorVersion = DeterministicGenerator.Version, ContentCatalogId = DeterministicGenerator.ContentCatalogId,
                Requested = 1000, Accepted = accepted, Rejected = 1000 - accepted, Failures = failures,
                AverageSolverStates = accepted == 0 ? 0 : (double)totalStates / accepted, MaximumSolverStates = maxStates, ReproducibilityFailures = reproducibility,
                RejectionReasons = reasons.GroupBy(x => x).Select(x => $"{x.Key}:{x.Count()}").OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                Coverage = "All three creation families and all five difficulty bands"
            };
            File.WriteAllText(Path.GetFullPath("Docs/Phase2SeedBatchReport.json"), JsonUtility.ToJson(report, true));
            Debug.Log($"PHASE2_SEED_BATCH accepted={accepted} rejected={1000 - accepted} failures={failures} averageStates={report.AverageSolverStates:0.000} maxStates={maxStates} reproducibilityFailures={reproducibility}");
            EditorApplication.Exit(failures == 0 && reproducibility == 0 ? 0 : 2);
        }

        static string Signature(GeneratedPuzzle puzzle) => puzzle.Definition.Id + "|" + string.Join("|", puzzle.Definition.Parts.Select((p, i) =>
            $"{p.Id}:{string.Join(",", p.AssemblyDependencyPartIds)}:{string.Join(",", p.AllowedRotations)}:{p.CompatibilityGroup}:{string.Join(",", puzzle.Definition.Screws[i].RequiredRemovedScrewIds)}"));
    }
}
