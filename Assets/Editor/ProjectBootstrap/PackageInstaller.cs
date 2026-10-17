using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ProjectBootstrap
{
    public static class PackageInstaller
    {
        static readonly string[] PackagesToAdd = { };
        static readonly string[] PackagesToRemove =
        {
            "com.unity.ide.rider",
            "com.unity.ide.visualstudio",
            "com.unity.collab-proxy"
        };
        const double TimeoutSeconds = 600;
        static AddAndRemoveRequest _request;
        static double _deadline;

        public static void Install()
        {
            Debug.Log($"[PackageInstaller] Removing optional template packages: {string.Join(", ", PackagesToRemove)}");
            _request = Client.AddAndRemove(PackagesToAdd, PackagesToRemove);
            _deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (_request == null) return;
            if (!_request.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup <= _deadline) return;
                EditorApplication.update -= Poll;
                Debug.LogError("[PackageInstaller] Timed out waiting for UPM.");
                EditorApplication.Exit(2);
                return;
            }
            EditorApplication.update -= Poll;
            if (_request.Status == StatusCode.Success)
            {
                Debug.Log($"[PackageInstaller] Resolved: {string.Join(", ", _request.Result.Select(p => $"{p.name}@{p.version}"))}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[PackageInstaller] Failed: {_request.Error?.message}");
                EditorApplication.Exit(1);
            }
        }
    }
}
