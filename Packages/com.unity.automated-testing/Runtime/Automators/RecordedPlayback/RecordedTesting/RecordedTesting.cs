using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.AutomatedQA;
using Unity.RecordedPlayback;
using Unity.RecordedTesting.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

namespace Unity.RecordedTesting
{

    public class TestRecordingData
    {
        public string testMethod;
        public string recording;

        public TestRecordingData(string fullName, string recording)
        {
            this.testMethod = fullName;
            this.recording = recording;
        }
    }

    public static class RecordedTesting
    {

        public static string GetLocalRecordingFile(string testName) 
        {
            //TODO: Cache this ?
            foreach (var testdata in GetAllRecordedTests())
            {
                if (testdata.testMethod == testName)
                {
                    return testdata.recording;
                }
            }

            Debug.LogError($"Recording file not found for test {testName}");
            return null;
        }

        public static List<TestRecordingData> GetAllRecordedTests()
        {
            var results = new List<TestRecordingData>();
            
            Assembly[] assems = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly a in assems)
            {
                foreach (Type t in a.GetTypes())
                {
                    foreach (MethodInfo m in t.GetMethods())
                    {
                        foreach (var attribute in m.GetCustomAttributes())
                        {
                            var fullName = t.FullName + "." + m.Name;
                            var rtAttr = attribute as RecordedTestAttribute;
                            if (rtAttr != null)
                            {
                                results.Add(new TestRecordingData(fullName, rtAttr.GetRecording()));
                            }
                        }
                    }
                }
            }

            return results;
        }
        
        /// <summary>
        /// Setup test with cloud recordings and prepare play back
        /// </summary>
        /// <param name="testName"></param>
        public static void SetupCloudUTFTests(string testName)
        {
            if (AutomatedQARuntimeSettings.hostPlatform == HostPlatform.Cloud && 
                AutomatedQARuntimeSettings.buildType == BuildType.UnityTestRunner)
            {
                // testName and recordingName have a 1-1 mapping.
                RecordedPlaybackPersistentData.SetRecordingMode(RecordingMode.Playback);
                RuntimeClient.DownloadRecording(testName, RecordedPlaybackPersistentData.GetRecordingDataFilePath());
            }

        }

        /// <summary>
        /// Set up the test with recording data and prepare to play back the recording
        /// </summary>
        /// <param name="testName">
        /// The fully qualified name of the test method (NUnit.Framework.TestContext.CurrentContext.Test.FullName)
        /// </param>
        public static void SetupRecordedTest(string testName)
        {
            RecordedPlaybackPersistentData.SetRecordingMode(RecordingMode.Playback);
            ReportingManager.CurrentTestName = testName;
#if !UNITY_EDITOR
            // Copy recording data from Resources
            var resourcePath = GetLocalRecordingFile(testName);
            var baseDir = Path.GetDirectoryName(resourcePath) ?? "";
            var recordingFile = CreateFileFromResource(resourcePath, RecordedPlaybackPersistentData.kRecordedPlaybackFilename);
            var segments = RecordedPlaybackPersistentData.GetSegmentFiles(recordingFile);
            foreach (var segment in segments)
            {
                CreateFileFromResource(Path.Combine(baseDir, segment), segment);
            }
#else
            // Copy recording data from asset
            string sourcePath = Path.Combine(Application.dataPath, GetLocalRecordingFile(testName));
            RecordedPlaybackPersistentData.SetRecordingDataFromFile(sourcePath);
#endif
        }

        private static string CreateFileFromResource(string resourcePath, string fileName)
        {
            var resource = Path.Combine(Path.GetDirectoryName(resourcePath), Path.GetFileNameWithoutExtension(resourcePath));
            var recording = Resources.Load<TextAsset>(resource);
            if (recording != null)
            {
                string destPath = Path.Combine(AutomatedQARuntimeSettings.PersistentDataPath, fileName);
                File.WriteAllText(destPath, recording.text);
                Debug.Log($"Copied recording file {resourcePath} to {destPath}");
                return destPath;
            }
            else
            {
                Debug.LogError($"Could not load recording {resourcePath}");
            }

            return null;
        }
        
        private static bool IsPlaybackCompleted()
        {
            return RecordedPlaybackController.Exists() && RecordedPlaybackController.Instance.IsPlaybackCompleted();
        }

        public static IEnumerator TestPlayToEnd()
        {
            while (!IsPlaybackCompleted())
            {
                yield return null;
            }
        }
    }
}