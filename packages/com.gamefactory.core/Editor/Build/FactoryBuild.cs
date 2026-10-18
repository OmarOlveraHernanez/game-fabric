using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GameFactory.Core.Editor
{
    /// <summary>Identidad del juego, leida de factory.json en la raiz del proyecto Unity.</summary>
    [Serializable]
    public class FactoryConfig
    {
        public string appId;
        public string productName;
        public string companyName;
        public string version;
        public int versionCode;
        public string orientation; // "landscape" (defecto) o "portrait"
    }

    /// <summary>
    /// Builds de Android por linea de comandos (ver tools/build.ps1).
    ///   -executeMethod GameFactory.Core.Editor.FactoryBuild.BuildDevApk      [-buildOutput ruta.apk]
    ///   -executeMethod GameFactory.Core.Editor.FactoryBuild.BuildReleaseAab  [-buildOutput ruta.aab]
    /// El build de lanzamiento lee la firma de variables de entorno:
    ///   GF_KEYSTORE_PATH, GF_KEYSTORE_PASS, GF_KEY_ALIAS, GF_KEY_PASS
    /// </summary>
    public static class FactoryBuild
    {
        public static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        public static FactoryConfig LoadConfig()
        {
            string path = Path.Combine(ProjectRoot, "factory.json");
            if (!File.Exists(path)) throw new FileNotFoundException("No existe factory.json en la raiz del proyecto", path);
            var cfg = JsonUtility.FromJson<FactoryConfig>(File.ReadAllText(path));
            if (string.IsNullOrWhiteSpace(cfg.appId)) throw new Exception("factory.json: falta 'appId'");
            if (string.IsNullOrWhiteSpace(cfg.productName)) throw new Exception("factory.json: falta 'productName'");
            if (string.IsNullOrWhiteSpace(cfg.version)) cfg.version = "0.1.0";
            return cfg;
        }

        public static void ApplyConfig(FactoryConfig cfg)
        {
            PlayerSettings.companyName = cfg.companyName;
            PlayerSettings.productName = cfg.productName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, cfg.appId);
            PlayerSettings.bundleVersion = cfg.version;
            PlayerSettings.Android.bundleVersionCode = Math.Max(1, cfg.versionCode);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            ApplyOrientation(cfg.orientation);
            AssetDatabase.SaveAssets();
        }

        static void ApplyOrientation(string orientation)
        {
            bool portrait = string.Equals(orientation, "portrait", StringComparison.OrdinalIgnoreCase);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = portrait;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = !portrait;
            PlayerSettings.allowedAutorotateToLandscapeRight = !portrait;
        }

        [MenuItem("Game Factory/Aplicar factory.json")]
        public static void ApplyConfigMenu()
        {
            var cfg = LoadConfig();
            ApplyConfig(cfg);
            Debug.Log($"[GameFactory] factory.json aplicado: {cfg.productName} ({cfg.appId}) v{cfg.version}");
        }

        [MenuItem("Game Factory/Build APK de desarrollo")]
        public static void BuildDevApk() => Build(release: false);

        public static void BuildReleaseAab() => Build(release: true);

        static void Build(bool release)
        {
            try
            {
                var cfg = LoadConfig();
                ApplyConfig(cfg);

                string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                if (scenes.Length == 0) throw new Exception("No hay escenas habilitadas en Build Settings / Build Profiles.");

                string fileName = $"{Sanitize(cfg.productName)}-{cfg.version}-{(release ? "release.aab" : "dev.apk")}";
                string output = GetArg("-buildOutput")
                    ?? Path.GetFullPath(Path.Combine(ProjectRoot, "..", "..", "builds", Sanitize(cfg.productName), fileName));
                Directory.CreateDirectory(Path.GetDirectoryName(output));

                EditorUserBuildSettings.buildAppBundle = release;
                // Solo ARM64: Unity 6.3 ya no admite x86_64 en Android (solo Magic Leap).
                // El emulador x86_64 (Android 11+) traduce ARM64 automaticamente.
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

                if (release) ConfigureSigning();
                else PlayerSettings.Android.useCustomKeystore = false;

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = output,
                    target = BuildTarget.Android,
                    options = release ? BuildOptions.None : BuildOptions.Development
                };

                Debug.Log($"[GameFactory] Build {(release ? "RELEASE AAB" : "DEV APK")} -> {output}");
                BuildReport report = BuildPipeline.BuildPlayer(options);
                var s = report.summary;
                if (s.result != BuildResult.Succeeded)
                    throw new Exception($"Build fallido: {s.result}, errores: {s.totalErrors}");

                Debug.Log($"[GameFactory] BUILD OK: {output} ({s.totalSize / (1024f * 1024f):0.0} MB, {s.totalTime.TotalSeconds:0}s)");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameFactory] BUILD ERROR: {e.Message}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        static void ConfigureSigning()
        {
            string ks = Environment.GetEnvironmentVariable("GF_KEYSTORE_PATH");
            string ksPass = Environment.GetEnvironmentVariable("GF_KEYSTORE_PASS");
            string alias = Environment.GetEnvironmentVariable("GF_KEY_ALIAS");
            string aliasPass = Environment.GetEnvironmentVariable("GF_KEY_PASS");
            if (new[] { ks, ksPass, alias, aliasPass }.Any(string.IsNullOrEmpty) || !File.Exists(ks))
                throw new Exception("Faltan variables de firma (GF_KEYSTORE_PATH/PASS, GF_KEY_ALIAS/PASS) o el keystore no existe.");

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = ks;
            PlayerSettings.Android.keystorePass = ksPass;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = aliasPass;
        }

        static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        static string Sanitize(string s) => new string(s.Where(char.IsLetterOrDigit).ToArray());
    }
}
