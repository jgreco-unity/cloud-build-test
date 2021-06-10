using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Unity.AutomatedQA;
using Unity.RecordedPlayback;
using Unity.RecordedTesting;
using UnityEditor;
using Unity.RecordedTesting.Runtime;
using UnityEngine;

namespace Unity.RecordedTesting
{
    
    public class CloudTesting : MonoBehaviour
    {

        
        public class TestName
        {
            public string fullName { get; private set; }
            public string funcName { get; private set; }
            public string typeName { get; private set; }
            public string namespaceName { get; private set; }

            public TestName(string testName)
            {
                this.fullName = testName;

                var matches = Regex.Matches(testName, @"[^.][a-zA-Z0-9-_]+");
                try
                {
                    this.funcName = matches[matches.Count - 1].Value;
                    this.typeName = matches[matches.Count - 2].Value;
                    this.namespaceName = matches.Count == 3 ? matches[matches.Count - 3].Value : null;
                }
                catch (IndexOutOfRangeException)
                {
                    Debug.LogError($"Invalid testName: {testName}");
                    this.funcName = null;
                    this.typeName = null;
                    this.namespaceName = null;
                }
            }
        }

        public class TestFunction
        {
            private IEnumerator func = null;

            public TestFunction(IEnumerator func)
            {
                this.func = func;
            }

            public IEnumerator Run()
            {
                return func;
            }
        }

        private static CloudTesting _instance;

        public static CloudTesting Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = FindObjectOfType<CloudTesting>();
                }

                if (!_instance)
                {
                    _instance = new GameObject($"#RecordedTesting").AddComponent<CloudTesting>();
                }

                return _instance;
            }
        }
        

        void Start()
        {
            if (AutomatedQARuntimeSettings.hostPlatform == HostPlatform.Cloud && 
                AutomatedQARuntimeSettings.buildType == BuildType.FullBuild)
            {
                DontDestroyOnLoad(this.gameObject);
                RecordedPlaybackPersistentData.SetRecordingMode(RecordingMode.Playback);
                DeviceFarmConfig dfConf = CloudTestManager.Instance.GetDeviceFarmConfig();
                Application.quitting += () =>
                {
# if UNITY_EDITOR
                    Debug.Log($"Counters generated - {CloudTestManager.Instance.GetTestResults().ToString()}");
#else
                CloudTestManager.UploadCounters();
#endif
                    RuntimeClient.LogTestCompletion(dfConf.testName);
                };

                // Optionally us a settings json file other than the default.
                TextAsset settings = Resources.Load<TextAsset>(Path.GetFileNameWithoutExtension(dfConf.settingsFileToLoad));
                if (!string.IsNullOrEmpty(dfConf.settingsFileToLoad) && settings != null && !string.IsNullOrEmpty(settings.text))
                {
                    Debug.Log($"Updating default Automated QA settings file to {dfConf.settingsFileToLoad}");
                    AutomatedQARuntimeSettings.AutomatedQaSettingsFileName = dfConf.settingsFileToLoad;
                    AutomatedQARuntimeSettings.RefreshConfig();
                }
                RunTest(dfConf.testName);
            }

        }

        public static void RunTest(string testName)
        {
            Instance.StartCoroutine(Instance.DoRunTest(testName));
        }

        private IEnumerator DoRunTest(string testName)
        {
            string recordingFileName = GetRecordingFileName(testName);
            RuntimeClient.DownloadRecording(recordingFileName,
                RecordedPlaybackPersistentData.GetRecordingDataFilePath());
            
            CentralAutomationController.Instance.Reset();
            CentralAutomationController.Instance.AddAutomator<RecordedPlaybackAutomator>();
            CentralAutomationController.Instance.Run();


            var test = GetTest(testName);
            if (test != null)
            {
                Debug.Log($"Running test {testName}");
                yield return StartCoroutine(test.Run());
            }
        }

        public static string GetRecordingFileName(string testname)
        {
            return testname;
        }

        public static TestFunction GetTest(string testName)
        {
            TestName splitName = new TestName(testName);

            Assembly[] assems = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly a in assems)
            {
                foreach (Type t in a.GetTypes())
                {
                    foreach (MethodInfo m in t.GetMethods())
                    {
                        foreach (var attribute in m.GetCustomAttributes())
                        {
                            if (attribute is RecordedTestAttribute &&
                                m.Name == splitName.funcName &&
                                t.Name == splitName.typeName &&
                                t.Namespace == splitName.namespaceName
                            )
                            {
                                object testObjInst = Activator.CreateInstance(t);
                                if (testObjInst != null)
                                {
                                    return new TestFunction((IEnumerator) m.Invoke(testObjInst, null));
                                }
                            }
                        }
                    }
                }
            }

            Debug.LogError($"Test not found: {testName}");
            return null;
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public static void AssertTrue(string metricName, bool condition)
        {
            SetResult(metricName, condition);
        }

        public static void AssertFalse(string metricName, bool condition)
        {
            SetResult(metricName, !condition);
        }

        private static void SetResult(string testName, bool passed)
        {
            CloudTestManager.Instance.SetCounter(testName, passed ? 1 : 0);
            Debug.Log("Assert result: " + testName + ": " + (passed ? "Pass" : "Fail"));
        }
    }
    
    
   
}