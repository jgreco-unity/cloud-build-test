using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SettingsManagement;

namespace Unity.AutomatedQA.Editor
{
    public static class AutomatedQAEditorSettings
    {
        internal const string k_PackageName = "com.unity.automated-testing";
        
        private static Settings s_Instance;
        internal static Settings instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new Settings(k_PackageName);

                return s_Instance;
            }
        }

        public static class MenuItems
        {
            public const int RecordedPlayback = 100;
            public const int GeneratedRecordedTests = 101;
            public const int RecordingUpload = 102;
            public const int CloudTestRunner = 103;
            
            public const int CreateAutomatedRun = 200;
            public const int CompositeRecordings = 201;
        }

        public static BuildType buildType
        {
            get => instance.Get<BuildType>("buildType", SettingsScope.Project, BuildType.UnityTestRunner);
            set => instance.Set<BuildType>("buildType", value, SettingsScope.Project);
        }
        
        public static HostPlatform hostPlatform
        {
            get => instance.Get<HostPlatform>("hostPlatform", SettingsScope.Project, HostPlatform.Local);
            set => instance.Set<HostPlatform>("hostPlatform", value, SettingsScope.Project);
        }
        
        public static RecordingFileStorage recordingFileStorage
        {
            get => instance.Get<RecordingFileStorage>("recordingFileStorage", SettingsScope.Project, RecordingFileStorage.Local);
            set => instance.Set<RecordingFileStorage>("recordingFileStorage", value, SettingsScope.Project);
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
           
            PlayerSettings.SetScriptingDefineSymbolsForGroup (targetGroup,string.Join (";", allDefined.ToArray()));
        }

        public static void ApplyBuildFlags(BuildTargetGroup targetGroup)
        {
            ClearBuildFlags(targetGroup);
            
            string _projectScriptingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            var allDefines = _projectScriptingDefines.Split(';').ToList();
            
            allDefines.Add(buildType.GetBuildFlag());
            allDefines.Add(hostPlatform.GetBuildFlag());
            allDefines.Add(recordingFileStorage.GetBuildFlag());

            PlayerSettings.SetScriptingDefineSymbolsForGroup (targetGroup, string.Join (";", allDefines.ToArray()));
        }
    }
}