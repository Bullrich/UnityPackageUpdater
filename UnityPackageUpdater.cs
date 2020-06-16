using System.Collections.Generic;
using System.IO;
using System.Linq;
using PackageUpdater.JsonWrapper;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Halodi.PackageCreator
{
    internal class UnityPackageUpdater : EditorWindow
    {
        private delegate void ListAction(PackageCollection collection, bool blocking);

        private static ListRequest listRequest;
        private Dictionary<PackageUpdateInformation, bool> packagesToUpdate;

        [MenuItem("Window/Tools/Update Packages")]
        private static void GetPackageInfo()
        {
            var window = (UnityPackageUpdater) GetWindow(typeof(UnityPackageUpdater), false, "Package Updater");
            window.Show();
        }

        private void OnEnable()
        {
            listRequest = Client.List();
            EditorApplication.update += UpdateRequest;
        }

        private void UpdateRequest()
        {
            if (listRequest == null || listRequest.IsCompleted)
            {
                EditorApplication.update -= UpdateRequest;
                if (listRequest != null && listRequest.Status == StatusCode.Success)
                {
                    PackagesFetched(listRequest.Result);
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Packages available to update", EditorStyles.boldLabel);

            if (packagesToUpdate == null)
            {
                GUILayout.Label("Fetching packages!", EditorStyles.boldLabel);
                return;
            }

            var packageKeys = packagesToUpdate.Keys.ToList();

            foreach (var package in packageKeys)
            {
                GUILayout.Label(package.PackageName, EditorStyles.boldLabel);
                var updateLabel = $"{package.CurrentVersion} -> {package.NewVersion}";
                packagesToUpdate[package] = EditorGUILayout.ToggleLeft(updateLabel, packagesToUpdate[package]);
            }

            EditorGUI.BeginDisabledGroup(packagesToUpdate.Values.ToList().All(v => !v));
            if (GUILayout.Button("Update packages"))
            {
                UpdatePackages(packagesToUpdate.Where(ptu => ptu.Value).Select(ptu => ptu.Key).ToList());
            }
            EditorGUI.EndDisabledGroup();
        }

        private void PackagesFetched(PackageCollection collection)
        {
            packagesToUpdate = new Dictionary<PackageUpdateInformation, bool>();
            foreach (var pi in collection)
            {
                if (pi.source == PackageSource.Registry && pi.version != pi.versions.latestCompatible)
                {
                    if (!pi.versions.latestCompatible.Contains("preview"))
                    {
                        packagesToUpdate.Add(new PackageUpdateInformation(pi), true);
                    }
                    else if (pi.version.Contains("preview"))
                    {
                        packagesToUpdate.Add(new PackageUpdateInformation(pi), false);
                    }
                }
            }
        }

        private static void UpdatePackages(IReadOnlyList<PackageUpdateInformation> updates)
        {
            var parser = new JsonNetParser();
            var project = new DirectoryInfo(Application.dataPath).Parent;
            var manifest = Path.Combine(project.FullName, "Packages/manifest.json");
            var manifestJson = File.ReadAllText(manifest);
            var manifestObject = parser.Parse(manifestJson);
            Debug.Log("Manifest before: " + manifestJson);
            var dependencies = manifestObject.Get<JsonDictionary>("dependencies");

            foreach (var package in updates)
            {
                dependencies[package.PackageName] = package.NewVersion;
            }

            var updatedManifest = parser.Serialize(manifestObject);

            Debug.Log("Manifest after: " + updatedManifest);
            Debug.Log(GenerateUpdateText(updates));
            File.WriteAllText(manifest, updatedManifest);
            var window = (UnityPackageUpdater) GetWindow(typeof(UnityPackageUpdater), false, "Package Updater");
            window.Close();
            AssetDatabase.Refresh();
        }

        private static string GenerateUpdateText(IEnumerable<PackageUpdateInformation> updates)
        {
            var log = "Updating the following packages:\n";
            foreach (var package in updates)
            {
                log += $"{package.PackageName}: {package.CurrentVersion} -> {package.NewVersion}";
            }

            return log;
        }

        private struct PackageUpdateInformation
        {
            public readonly string CurrentVersion;
            public readonly string NewVersion;
            public readonly string PackageName;

            public PackageUpdateInformation(PackageInfo info)
            {
                CurrentVersion = info.version;
                NewVersion = info.versions.latestCompatible;
                PackageName = info.name;
            }
        }
    }
}