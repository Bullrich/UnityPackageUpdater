using System.Collections.Generic;
using System.IO;
using System.Linq;
using PackageUpdater.JsonWrapper;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace PackageUpdater
{
    public class UnityPackageUpdater : EditorWindow
    {
        private ListRequest listRequest;
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
                    packagesToUpdate = PackagesFetched(listRequest.Result);
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Packages available to update", EditorStyles.boldLabel);

            if (packagesToUpdate == null)
            {
                EditorGUILayout.HelpBox("Fetching packages!", MessageType.Info);
                return;
            }
            else if (packagesToUpdate.Count == 0)
            {
                GUILayout.Label("All packages are up to date!", EditorStyles.boldLabel);
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

        public static Dictionary<PackageUpdateInformation, bool> PackagesFetched(PackageCollection collection)
        {
            var availableUpdates = new Dictionary<PackageUpdateInformation, bool>();
            foreach (var packageInfo in collection)
            {
                if (packageInfo.source == PackageSource.Registry &&
                    packageInfo.version != packageInfo.versions.latestCompatible)
                {
                    if (!packageInfo.versions.latestCompatible.Contains("preview"))
                    {
                        availableUpdates.Add(new PackageUpdateInformation(packageInfo), true);
                    }
                    else if (packageInfo.version.Contains("preview"))
                    {
                        availableUpdates.Add(new PackageUpdateInformation(packageInfo), false);
                    }
                }
            }

            return availableUpdates;
        }

        private static void UpdatePackages(IReadOnlyList<PackageUpdateInformation> updates)
        {
            var parser = new JsonNetParser();
            var project = new DirectoryInfo(Application.dataPath).Parent;
            var manifest = Path.Combine(project.FullName, "Packages/manifest.json");
            var manifestJson = File.ReadAllText(manifest);
            var manifestObject = parser.Parse(manifestJson);
            var dependencies = manifestObject.Get<JsonDictionary>("dependencies");

            foreach (var package in updates)
            {
                dependencies[package.PackageName] = package.NewVersion;
            }

            var updatedManifest = parser.Serialize(manifestObject);
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
                log += $"{package.PackageName}: {package.CurrentVersion} -> {package.NewVersion}\n";
            }

            return log;
        }

        public readonly struct PackageUpdateInformation
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