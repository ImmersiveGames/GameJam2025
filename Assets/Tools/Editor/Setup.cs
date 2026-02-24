using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using static System.IO.Path;
using static UnityEditor.AssetDatabase;

namespace Tools.Editor {
    [DefaultExecutionOrder(-200)]
    public static class Setup {
        [MenuItem("Tools/Setup/Create Default Folders")]
        public static void CreateDefaultFolders() {
            Folders.CreateDefault("_ImmersiveGames", 
                "Animations", "Arts", "Materials", "Prefabs", "Scenes", "Scripts/ScriptableObjects", "Scripts/UI", "Settings", "Sounds/BGM", "Sounds/SFX");
            Refresh();
        }

        [MenuItem("Tools/Setup/Import My Favorite Assets")]
        public static void ImportMyFavoriteAssets() {
            Assets.ImportAsset("DOTween HOTween v2.unitypackage", "Demigiant/Editor ExtensionsAnimation");
            Assets.ImportAsset("Better Hierarchy.unitypackage", "Toaster Head/Editor ExtensionsUtilities");
            Assets.ImportAsset("More Effective Coroutines FREE.unitypackage", "Trinary Software/ScriptingAnimation");
            Assets.ImportAsset("Replace Selected.unitypackage", "Staggart Creations/Editor ExtensionsUtilities");
        }
        
        [MenuItem("Tools/Setup/Import Pipichus")]
        public static void ImportMyAltAssets() {
            Assets.ImportAltAsset("Odin Inspector and Serializer v3.3.1.11.unitypackage", "Odin Inspector and Serializer");
            Assets.ImportAltAsset("Odin Validator v3.3.1.11.unitypackage", "Odin Validator");
        }
        
        [MenuItem("Tools/Setup/Import My Favorite Assets Prototype")]
        public static void ImportMyPrototypeAssets() {
            Assets.ImportAsset("Gridbox Prototype Materials.unitypackage", "Ciathyza/Textures Materials");
            Assets.ImportAsset("Grid Master.unitypackage", "Decimate/Shaders");
        }

        [MenuItem("Tools/Setup/Install Netcode for GameObjects")]
        public static void InstallNetcodeForGameObjects() {
            Packages.InstallPackages(new[] {
                "com.unity.multiplayer.tools",
                "com.unity.netcode.gameobjects"
            });
        }

        [MenuItem("Tools/Setup/Install Unity AI Navigation")]
        public static void InstallUnityAINavigation() {
            Packages.InstallPackages(new[] {
                "com.unity.ai.navigation"
            });
        }

        [MenuItem("Tools/Setup/Install My Favorite Open Source")]
        public static void InstallOpenSource() {
            Packages.InstallPackages(new[] {
                "git+https://github.com/starikcetin/Eflatun.SceneReference.git#upm",
                "git+https://github.com/Thundernerd/Unity3D-NSubstitute.git",
                "git+https://github.com/adammyhre/Unity-Improved-Timers.git"
            });
        }

        private static class Folders {
            public static void CreateDefault(string root, params string[] folders) {
                string fullPath = Path.Combine(Application.dataPath, root);
                if (!Directory.Exists(fullPath)) {
                    Directory.CreateDirectory(fullPath);
                }
                foreach (string folder in folders) {
                    CreateSubFolders(fullPath, folder);
                }
            }
    
            private static void CreateSubFolders(string rootPath, string folderHierarchy) {
                string[] folders = folderHierarchy.Split('/');
                string currentPath = rootPath;
                foreach (string folder in folders) {
                    currentPath = Path.Combine(currentPath, folder);
                    if (!Directory.Exists(currentPath)) {
                        Directory.CreateDirectory(currentPath);
                    }
                }
            }
        }

        private static class Packages {
            private static AddRequest _request;
            private static readonly Queue<string> PackagesToInstall = new();

            public static void InstallPackages(string[] packages) {
                foreach (string package in packages) {
                    PackagesToInstall.Enqueue(package);
                }

                // Start the installation of the first package
                if (PackagesToInstall.Count <= 0) return;
                _request = Client.Add(PackagesToInstall.Dequeue());
                EditorApplication.update += Progress;
            }

            private static async void Progress() {
                if (!_request.IsCompleted) return;
                switch (_request.Status) {
                    case StatusCode.Success:
                        Debug.Log("Installed: " + _request.Result.packageId);
                        break;
                    case >= StatusCode.Failure:
                        Debug.Log(_request.Error.message);
                        break;
                }

                EditorApplication.update -= Progress;

                // If there are more packages to install, start the next one
                if (PackagesToInstall.Count <= 0) return;
                // Add delay before next package installation
                await Task.Delay(1000);
                _request = Client.Add(PackagesToInstall.Dequeue());
                EditorApplication.update += Progress;
            }
        }

        private static class Assets {
            public static void ImportAsset(string asset, string subfolder,
                string rootFolder = "C:/Users/renat/AppData/Roaming/Unity/Asset Store-5.x") {
                ImportPackage(Combine(rootFolder, subfolder, asset), false);
            }
            public static void ImportAltAsset(string asset, string subfolder,
                string rootFolder = "D:/Download/UNITY ASSETS") {
                ImportPackage(Combine(rootFolder, subfolder, asset), false);
            }
        }
    }
}