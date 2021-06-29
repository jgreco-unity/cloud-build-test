using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.RecordedPlayback.Editor;

namespace Unity.AutomatedQA.Editor
{
    public static class AutomatedQAEditorSettings
    {
        internal const string k_PackageName = "com.unity.automated-testing";
        
        public static class MenuItems
        {
            public const int RecordedPlayback = 100;
            public const int GeneratedRecordedTests = 101;
            public const int RecordingUpload = 102;
            public const int CloudTestRunner = 103;
            
            public const int CreateAutomatedRun = 200;
            public const int CompositeRecordings = 201;
            public const int CodeGeneration = 201;
        }
        
        [Serializable]
        internal class SettingsData
        {
            [SerializeField]
            internal BuildType _buildType =  BuildType.UnityTestRunner;
            [SerializeField]
            internal HostPlatform _hostPlatform =  HostPlatform.Local;
            [SerializeField]
            internal RecordingFileStorage _recordingFileStorage = RecordingFileStorage.Local;
            
            internal SettingsData()
            {
               Load();
            }

            internal string GetFilePath()
            {
                return Path.Combine(Application.dataPath, "AutomatedQA/EditorSettings.json");
            }
            
            internal void Load()
            {
                if(!File.Exists(GetFilePath()))
                {
                    Save();
                }

                string json = File.ReadAllText(GetFilePath());
                JsonUtility.FromJsonOverwrite(json, this);
            }

            internal void Save()
            {
                var destDir = Path.GetDirectoryName(GetFilePath());
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
                
                string json = JsonUtility.ToJson(this);
                File.WriteAllText(GetFilePath(), json);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        private static SettingsData settingsData = new SettingsData();

        public static BuildType buildType
        {
            get => settingsData._buildType;
            set
            {
                settingsData._buildType = value;
                settingsData.Save();
            }
        }

        public static HostPlatform hostPlatform
        {
            get => settingsData._hostPlatform;
            set
            {
                settingsData._hostPlatform = value;
                settingsData.Save();
            }
        }
        
        public static RecordingFileStorage recordingFileStorage
        {
            get => settingsData._recordingFileStorage;
            set
            {
                settingsData._recordingFileStorage = value;
                settingsData.Save();
            }
        }
        
        private static string GetBuildFlag(this Enum value)
        {
            FieldInfo fieldInfo = value.GetType().GetField(value.ToString());
            if (fieldInfo == null) return null;
            var attribute = (BuildFlagAttribute)fieldInfo.GetCustomAttribute(typeof(BuildFlagAttribute));
            return attribute.buildFlag;
        }
        
        private static List<string> GetAllBuildFlags()
        {
            var results = new List<string>();
            
            Assembly[] assems = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly a in assems)
            {
                foreach (Type t in a.GetTypes())
                {
                    foreach (var f in t.GetFields())
                    {
                        foreach (var attribute in f.GetCustomAttributes())
                        {
                            var rtAttr = attribute as BuildFlagAttribute;
                            if (rtAttr != null)
                            {
                                results.Add(rtAttr.buildFlag);
                            }
                        }
                    }
                }
            }

            return results;
        }

        public static void ClearBuildFlags(BuildTargetGroup targetGroup)
        {
            var allFlags = GetAllBuildFlags();
            var allDefined = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Split(';').ToList();

            foreach (var f in allFlags)
            {
                allDefined.Remove(f);
            }
           
            PlayerSettings.SetScriptingDefineSymbolsForGroup (
                EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join (";", allDefined.ToArray()));
        }

        public static void ApplyBuildFlags(BuildTargetGroup targetGroup)
        {
            ClearBuildFlags(targetGroup);
            
            string _projectScriptingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            var allDefines = _projectScriptingDefines.Split(';').ToList();
            
            allDefines.Add(buildType.GetBuildFlag());
            allDefines.Add(hostPlatform.GetBuildFlag());
            allDefines.Add(recordingFileStorage.GetBuildFlag());

            PlayerSettings.SetScriptingDefineSymbolsForGroup (
                EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join (";", allDefines.ToArray()));
        }
    }
}