using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Unity.AutomatedQA;
using Unity.AutomatedQA.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.RecordedTesting.Editor
{
    public static class RecordedTestCreator
    {
        private static  string TestType = "RecordedTests";
        private static string GeneratedTestScriptTemplatePath =>
            $"{TestCreatorUtils.ScriptTemplatePath}C# Script-GeneratedRecordedTests.cs.txt";

        [MenuItem("Automated QA/Generate Recorded Tests", priority=AutomatedQAEditorSettings.MenuItems.GeneratedRecordedTests)]
        public static void GenerateRecordedTests()
        {
            DeleteGeneratedTests();
            TestCreatorUtils.CreateTestAssemblyFolder();
            TestCreatorUtils.CreateTestAssembly();
            TestCreatorUtils.CreateTestScriptFolder(TestType);
            CreateRecordedTestScripts();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.ClearProgressBar();
        }

        private static void CreateRecordedTestScripts()
        {
            EditorUtility.DisplayProgressBar("Generate Recorded Tests", "Create Recorded Test Scripts", 0);

            string templateContent = File.ReadAllText(GeneratedTestScriptTemplatePath);
            var paths = GetAllRecordingAssetPaths();
            for(int i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                EditorUtility.DisplayProgressBar("Generate Recorded Tests", $"Create Recorded Test Scripts: {path}", 1.0f*i/paths.Count);

                var recordingFilePath = path.Replace($"Assets/", "");
                var testClassName = GetClassNameForRecording(recordingFilePath);
                string content = templateContent
                    .Replace("#RECORDING_NAME#", testClassName)
                    .Replace("#RECORDING_FILE#", recordingFilePath);
                
                File.WriteAllText(
                    Path.Combine(Application.dataPath, 
                        TestCreatorUtils.AutomatedTestingFolderName, 
                        TestCreatorUtils.GeneratedTestsFolderName,
                        TestType,
                        $"{testClassName}.cs"),
                    content);
            }

        }

        private static string GetClassNameForRecording(string recordingFilePath)
        {
            var testClassName = Path.GetFileNameWithoutExtension(recordingFilePath);
            testClassName = Regex.Replace(testClassName, @"[\W_]+", "_");
            testClassName = "RecordedTest_" + testClassName;
            return testClassName;
        }

        private static List<string> GetAllRecordingAssetPaths()
        {
            var results = AssetDatabase.FindAssets("*", new[] { Path.Combine("Assets", AutomatedQARuntimeSettings.RecordingFolderPath) }).ToList();
            for (int i = 0; i < results.Count; i++)
            {
                results[i] = AssetDatabase.GUIDToAssetPath(results[i]);
            }
            results.Sort((a, b) => Convert.ToInt32((File.GetCreationTime(b) - File.GetCreationTime(a) ).TotalSeconds)); 
            return results;
        }
        
        private static void DeleteGeneratedTests()
        {
            EditorUtility.DisplayProgressBar("Generate Tests", "Delete Generated Tests", 0);

            var fullAsmPath = Path.Combine(Application.dataPath, 
                TestCreatorUtils.AutomatedTestingFolderName, 
                TestCreatorUtils.GeneratedTestsFolderName,
                TestType);
            FileUtil.DeleteFileOrDirectory(fullAsmPath);
        }
    }

}
