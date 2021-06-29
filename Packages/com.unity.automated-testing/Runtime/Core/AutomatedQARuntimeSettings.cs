using System.IO;
using UnityEngine;
using System.Collections.Generic;

namespace Unity.AutomatedQA
{
    public static class AutomatedQARuntimeSettings
    {
        public static readonly string GAMESIM_API_ENDPOINT = "https://api.prd.gamesimulation.unity3d.com";
        public static readonly string DEVICE_TESTING_API_ENDPOINT = "https://device-testing.prd.gamesimulation.unity3d.com";
        private static AutomatedQASettingsData settings
        {
            get
            {
                if (_settings == null || _settings != default(AutomatedQASettingsData))
                {
                    _settings = GetCustomSettingsData();
                }
                return _settings;
            }
        }
        private static AutomatedQASettingsData _settings;

        static AutomatedQARuntimeSettings()
        {
#if UNITY_EDITOR
            if (!Directory.Exists(Path.Combine(Application.dataPath, AutomatedQASettingsResourcesPath)))
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, AutomatedQASettingsResourcesPath));
            }
#endif
            // Handle required configs. Re-add them if deleted from config settings file.
            List<AutomationSet> setsToReAddToConfigSettingsFile = new List<AutomationSet>();

            string settingKey = "RecordingFolderName";
            AutomationSet set = settings.Configs.Find(c => c.Key == settingKey);
            if (set == null || set == default(AutomationSet))
            {
                set = new AutomationSet(settingKey, RecordingFolderName);
                setsToReAddToConfigSettingsFile.Add(set);
            }
            RecordingFolderName = set.Value.ToString();

            settingKey = "ActivatePlaybackVisualFx";
            set = settings.Configs.Find(c => c.Key == settingKey);
            if (set == null || set == default(AutomationSet))
            {
                set = new AutomationSet(settingKey, ActivatePlaybackVisualFx.ToString());
                setsToReAddToConfigSettingsFile.Add(set);
            }
            ActivatePlaybackVisualFx = bool.Parse(set.Value.ToString());

            settingKey = "ActivateClickFeedbackFx";
            set = settings.Configs.Find(c => c.Key == settingKey);
            if (set == null || set == default(AutomationSet))
            {
                set = new AutomationSet(settingKey, ActivateClickFeedbackFx.ToString());
                setsToReAddToConfigSettingsFile.Add(set);
            }
            ActivateClickFeedbackFx = bool.Parse(set.Value.ToString());

            settingKey = "ActivateDragFeedbackFx";
            set = settings.Configs.Find(c => c.Key == settingKey);
            if (set == null || set == default(AutomationSet))
            {
                set = new AutomationSet(settingKey, ActivateDragFeedbackFx.ToString());
                setsToReAddToConfigSettingsFile.Add(set);
            }
            ActivateDragFeedbackFx = bool.Parse(set.Value.ToString());

            settingKey = "ActivateHighlightFeedbackFx";
            set = settings.Configs.Find(c => c.Key == settingKey);
            if (set == null || set == default(AutomationSet))
            {
                set = new AutomationSet(settingKey, ActivateHighlightFeedbackFx.ToString());
                setsToReAddToConfigSettingsFile.Add(set);
            }
            ActivateHighlightFeedbackFx = bool.Parse(set.Value.ToString());

            settingKey = "EnableScreenshots";
            set = settings.Configs.Find(c => c.Key == settingKey);
            if (set == null || set == default(AutomationSet))
            {
                set = new AutomationSet(settingKey, EnableScreenshots.ToString());
                setsToReAddToConfigSettingsFile.Add(set);
            }
            EnableScreenshots = bool.Parse(set.Value.ToString());

            settingKey = "UseDynamicWaits";
            set = settings.Configs.Find(c => c.Key == settingKey);
            if (set == null || set == default(AutomationSet))
            {
                set = new AutomationSet(settingKey, UseDynamicWaits.ToString());
                setsToReAddToConfigSettingsFile.Add(set);
            }
            UseDynamicWaits = bool.Parse(set.Value.ToString());

            settingKey = "DynamicWaitTimeout";
            set = settings.Configs.Find(c => c.Key == settingKey);
            if (set == null || set == default(AutomationSet))
            {
                set = new AutomationSet(settingKey, DynamicWaitTimeout.ToString());
                setsToReAddToConfigSettingsFile.Add(set);
            }
            DynamicWaitTimeout = float.Parse(set.Value.ToString());

            settingKey = "DynamicLoadSceneTimeout";
            set = settings.Configs.Find(c => c.Key == settingKey);
            if (set == null || set == default(AutomationSet))
            {
                set = new AutomationSet(settingKey, DynamicLoadSceneTimeout.ToString());
                setsToReAddToConfigSettingsFile.Add(set);
            }
            DynamicLoadSceneTimeout = float.Parse(set.Value.ToString());

            settingKey = "EnableDebugLogging";
            set = settings.Configs.Find(c => c.Key == settingKey);
            if (set == null || set == default(AutomationSet))
            {
                set = new AutomationSet(settingKey, EnableDebugLogging.ToString());
                setsToReAddToConfigSettingsFile.Add(set);
            }
            EnableDebugLogging = bool.Parse(set.Value.ToString());

#if UNITY_EDITOR
            // Add back any required configs that were deleted by the user.
            if (setsToReAddToConfigSettingsFile.Any()) 
            {
                AutomatedQASettingsData newConfig = new AutomatedQASettingsData();
                newConfig.Configs.AddRange(setsToReAddToConfigSettingsFile);
                newConfig.Configs.AddRange(settings.Configs);
                File.WriteAllText(Path.Combine(Application.dataPath, AutomatedQASettingsResourcesPath, AutomatedQaSettingsFileName), JsonUtility.ToJson(newConfig));
            }
#endif
        }

        /// <summary>
        /// Folder on device where we store Automated QA temp and customization data.
        /// </summary>
        public static string PersistentDataPath
        {
            get
            {
                if (_persistentDataPath == null)
                {
                    _persistentDataPath = Path.Combine(Application.persistentDataPath, PackageAssetsFolderName);
                }
                return _persistentDataPath;
            }
            set
            {
                _persistentDataPath = value;
            }
        }
        private static string _persistentDataPath;

        /// <summary>
        /// Name of the Assets data path that our files are stored under.
        /// </summary>
        public static string PackageAssetsFolderPath
        {
            get
            {
                if (string.IsNullOrEmpty(_packageAssetsFolderPath))
                {
                    _packageAssetsFolderPath = Path.Combine(Application.dataPath, PackageAssetsFolderName);
                }
                return _packageAssetsFolderPath;
            }
            set
            {
                _packageAssetsFolderPath = value;
            }
        }
        private static string _packageAssetsFolderPath;

        /// <summary>
        /// Name of the Assets data path that our files are stored under.
        /// </summary>
        public static string PackageAssetsFolderName
        {
            get
            {
                if (string.IsNullOrEmpty(_packageAssetsFolderName))
                {
                    _packageAssetsFolderName = "AutomatedQA";
                }
                return _packageAssetsFolderName;
            }
            set
            {
                _packageAssetsFolderName = value;
            }
        }
        private static string _packageAssetsFolderName;

        /// <summary>
        /// Full path to folder where we store recording files.
        /// </summary>
        public static string RecordingDataPath
        {
            get
            {
                if (string.IsNullOrEmpty(_recordingDataPath))
                {
                    _recordingDataPath = Path.Combine(Application.dataPath, RecordingFolderName);
                }
                return _recordingDataPath;
            }
            set
            {
                _recordingDataPath = value;
            }
        }
        private static string _recordingDataPath;

        /// <summary>
        /// Name of folder under Assets, including Assets path, where we store recording files.
        /// </summary>
        public static string RecordingFolderNameWithAssetPath
        {
            get
            {
                return $"Assets/{RecordingFolderName}";
            }
        }

        /// <summary>
        /// Name of folder under Assets where we store recording files.
        /// </summary>
        public static string RecordingFolderName
        {
            get
            {
                if (string.IsNullOrEmpty(_recordingFolderName))
                {
                    _recordingFolderName = "Recordings";
                }
                return _recordingFolderName;
            }
            set
            {
                _recordingFolderName = value;
            }
        }
        private static string _recordingFolderName;

        /// <summary>
        /// Name of folder where we tests generated from recordings.
        /// </summary>
        public static string GeneratedTestsFolderName
        {
            get
            {
                if (string.IsNullOrEmpty(_generatedTestsFolderName))
                {
                    _generatedTestsFolderName = "GeneratedTests";
                }
                return _generatedTestsFolderName;
            }
            set
            {
                _generatedTestsFolderName = value;
            }
        }
        private static string _generatedTestsFolderName;
        
        /// <summary>
        /// Enable or disable visual Fx feedback for actions taken during playback of recordings.
        /// If true, check individual feedback booleans to see if a subset will be activated. 
        /// </summary>
        public static bool ActivatePlaybackVisualFx
        {
            get
            {
                return _activatePlaybackVisualFx;
            }
            set
            {
                _activatePlaybackVisualFx = value;
            }
        }
        private static bool _activatePlaybackVisualFx = true;

        /// <summary>
        /// Activates ripple effect on point of click during playback of recordings.
        /// </summary>
        public static bool ActivateClickFeedbackFx
        {
            get
            {
                return _activateClickFeedbackFx;
            }
            set
            {
                _activateClickFeedbackFx = value;
            }
        }
        private static bool _activateClickFeedbackFx = true;

        /// <summary>
        /// Activates drag effect between drag start and drag release during playback of recordings.
        /// </summary>
        public static bool ActivateDragFeedbackFx
        {
            get
            {
                return _activateDragFeedbackFx;
            }
            set
            {
                _activateDragFeedbackFx = value;
            }
        }
        private static bool _activateDragFeedbackFx = true;

        /// <summary>
        /// Activates highlight effect on point of click during playback of recordings.
        /// </summary>
        public static bool ActivateHighlightFeedbackFx
        {
            get
            {
                return _activateHighlightFeedbackFx;
            }
            set
            {
                _activateHighlightFeedbackFx = value;
            }
        }
        private static bool _activateHighlightFeedbackFx = true;

        /// <summary>
        /// Allows screenshots to be recorded during test run. These are used to show screenshots in reports.
        /// </summary>
        public static bool EnableScreenshots
        {
            get
            {
                return _enableScreenshots;
            }
            set
            {
                _enableScreenshots = value;
            }
        }
        private static bool _enableScreenshots = true;

        /// <summary>
        /// Adds extra debug messages during recording creation and playback.
        /// </summary>
        public static bool EnableDebugLogging
        {
            get
            {
                return _enableDebugLogging;
            }
            set
            {
                _enableDebugLogging = value;
            }
        }
        private static bool _enableDebugLogging = true;

        public static BuildType buildType
        {
            get
            {
#if AQA_BUILD_TYPE_FULL
                return BuildType.FullBuild;
#else
                return BuildType.UnityTestRunner;
#endif
            }
        }

        public static HostPlatform hostPlatform
        {
            get
            {
#if AQA_PLATFORM_CLOUD
                return HostPlatform.Cloud;
#else
                return HostPlatform.Local;
#endif
            }
        }

        public static RecordingFileStorage recordingFileStorage
        {
            get
            {
                // TODO use a runtime config file instead of preprocessor defines?
#if AQA_RECORDING_STORAGE_CLOUD
                return RecordingFileStorage.Cloud;
#else
                return RecordingFileStorage.Local;
#endif
            }
        }

        /// <summary>
        /// Wait for elements to become interactable before trying to interact with them (as opposed to waiting for the original recorded timeDelta period before executing an action).
        /// </summary>
        public static bool UseDynamicWaits
        {
            get
            {
                return _useDynamicWaits;
            }
            set
            {
                _useDynamicWaits = value;
            }
        }
        private static bool _useDynamicWaits = true;

        /// <summary>
        /// Period of time to wait for a target GameObject to be ready while waiting to perform the next action in test playback.
        /// </summary>
        public static float DynamicWaitTimeout
        {
            get
            {
                if (_dynamicWaitTimeout < 0.001f)
                {
                    _dynamicWaitTimeout = 10f;
                }
                return _dynamicWaitTimeout;
            }
            set
            {
                _dynamicWaitTimeout = value;
            }
        }
        private static float _dynamicWaitTimeout = 0f;

        /// <summary>
        /// Period of time to wait for scene load while waiting to perform the next action in test playback.
        /// </summary>
        public static float DynamicLoadSceneTimeout
        {
            get
            {
                if (_dynamicLoadSceneTimeout < 0.001f)
                {
                    _dynamicLoadSceneTimeout = 30f;
                }
                return _dynamicLoadSceneTimeout;
            }
            set
            {
                _dynamicLoadSceneTimeout = value;
            }
        }
        private static float _dynamicLoadSceneTimeout = 0f;

        private static TextAsset configTextAsset {
            get {
                if (_configTextAsset == null)
                    RefreshConfig();
                return _configTextAsset;
            }
        }
        private static TextAsset _configTextAsset;

        /// <summary>
        /// Resources are cached. Changes made to them in run time will not be seen until reload. Since edits to configs won't happen outside of editor, use Resources.Load outside of editor and File.ReadAllText in editor.
        /// </summary>
        public static void RefreshConfig() {
#if UNITY_EDITOR
            string path = Path.Combine(Application.dataPath, AutomatedQASettingsResourcesPath, AutomatedQaSettingsFileName);
            if (!File.Exists(path))
            {
                _configTextAsset = null;
                return;
            }
            _configTextAsset = new TextAsset(File.ReadAllText(path));
#else
            _configTextAsset = Resources.Load<TextAsset>(Path.GetFileNameWithoutExtension(AutomatedQaSettingsFileName));
#endif
        }

        public static AutomatedQASettingsData GetCustomSettingsData()
        {
            if (configTextAsset == null)
            {
                AutomatedQASettingsData configCategories = new AutomatedQASettingsData();
                configCategories.Configs.Add(new AutomationSet("EnableDebugLogging", EnableDebugLogging.ToString()));
                configCategories.Configs.Add(new AutomationSet("EnableScreenshots", EnableScreenshots.ToString()));
                configCategories.Configs.Add(new AutomationSet("RecordingFolderName", RecordingFolderName));
                configCategories.Configs.Add(new AutomationSet("ActivatePlaybackVisualFx", ActivatePlaybackVisualFx.ToString()));
                configCategories.Configs.Add(new AutomationSet("ActivateClickFeedbackFx", ActivateClickFeedbackFx.ToString()));
                configCategories.Configs.Add(new AutomationSet("ActivateDragFeedbackFx", ActivateDragFeedbackFx.ToString()));
                configCategories.Configs.Add(new AutomationSet("ActivateHighlightFeedbackFx", ActivateHighlightFeedbackFx.ToString()));
                configCategories.Configs.Add(new AutomationSet("UseDynamicWaits", UseDynamicWaits.ToString()));
                configCategories.Configs.Add(new AutomationSet("DynamicWaitTimeout", DynamicWaitTimeout.ToString()));
                configCategories.Configs.Add(new AutomationSet("DynamicLoadSceneTimeout", DynamicLoadSceneTimeout.ToString()));

#if UNITY_EDITOR
                File.WriteAllText(Path.Combine(Application.dataPath, AutomatedQASettingsResourcesPath, AutomatedQaSettingsFileName), JsonUtility.ToJson(configCategories));
#endif
                return configCategories;
            }
            return JsonUtility.FromJson<AutomatedQASettingsData>(configTextAsset.text);
        }

        public static string GetStringFromCustomSettings(string key)
        {
            AutomationSet keyVal = settings.Configs.Find(x => x.Key == key);
            if (keyVal == default(AutomationSet) || string.IsNullOrEmpty(keyVal.Key))
            {
                Debug.LogError($"Key requested ({key}) which is not defined in the settings file or is invalid.{(hostPlatform == HostPlatform.Cloud ? " Make sure you are supplying the expected settings config file name to DeviceFarmConfig." : string.Empty)}");
            }
            return keyVal.Value;
        }

        public static int GetIntFromCustomSettings(string key)
        {
            AutomationSet keyVal = settings.Configs.Find(x => x.Key == key);
            int val = 0;
            bool isInt = keyVal == default(AutomationSet) ? false : int.TryParse(keyVal.Value, out val);
            if (!isInt)
            {
                Debug.LogError($"Key requested ({key}) which is not defined in the settings file or is invalid.{(hostPlatform == HostPlatform.Cloud ? " Make sure you are supplying the expected settings config file name to DeviceFarmConfig." : string.Empty)}");
            }
            return val;
        }

        public static float GetFloatFromCustomSettings(string key)
        {
            AutomationSet keyVal = settings.Configs.Find(x => x.Key == key);
            float val = 0;
            bool isFloat = keyVal == default(AutomationSet) ? false : float.TryParse(keyVal.Value, out val);
            if (!isFloat)
            {
                Debug.LogError($"Key requested ({key}) which is not defined in the settings file or is invalid.{(hostPlatform == HostPlatform.Cloud ? " Make sure you are supplying the expected settings config file name to DeviceFarmConfig." : string.Empty)}");
            }
            return val;
        }

        public static bool GetBooleanFromCustomSettings(string key)
        {
            AutomationSet keyVal = settings.Configs.Find(x => x.Key == key);
            bool returnVal = false;
            if (keyVal == default(AutomationSet) || !bool.TryParse(keyVal.Value, out returnVal))
            {
                Debug.LogError($"Key requested ({key}) which is not defined in the settings file or is invalid.{(hostPlatform == HostPlatform.Cloud ? " Make sure you are supplying the expected settings config file name to DeviceFarmConfig." : string.Empty)}");
            }
            return returnVal;
        }

        public static string AutomatedQaSettingsFileName
        {
            get
            {
                if (string.IsNullOrEmpty(_automatedQaSettingsFileName))
                {
                    _automatedQaSettingsFileName = "AutomatedQASettings.json";
                }
                return _automatedQaSettingsFileName;
            }
            set
            {
                _automatedQaSettingsFileName = value;
            }
        }
        private static string _automatedQaSettingsFileName;

        /// <summary>
        /// Name of the relative data path that our files are stored under.
        /// </summary>
        public static string AutomatedQASettingsResourcesPath
        {
            get
            {
                if (string.IsNullOrEmpty(_automatedQASettingsResourcesPath))
                {
                    _automatedQASettingsResourcesPath = Path.Combine(PackageAssetsFolderName, "Resources");
                }
                return _automatedQASettingsResourcesPath;
            }
            set
            {
                _automatedQASettingsResourcesPath = value;
            }
        }
        private static string _automatedQASettingsResourcesPath;

        [System.Serializable]
        public class AutomatedQASettingsData
        {
            public AutomatedQASettingsData()
            {
                Configs = new List<AutomationSet>();
            }
            public List<AutomationSet> Configs;
        }

        [System.Serializable]
        public class AutomationSet
        {
            public AutomationSet(string Key, string Value)
            {
                this.Key = Key;
                this.Value = Value;
            }
            public string Key;
            public string Value;
        }
    }
}