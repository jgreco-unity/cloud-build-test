#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using Unity.AutomatedQA;
using Unity.RecordedPlayback;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.RecordedTesting.TestTools
{
    public abstract class RecordedTestSuite
    {
        private int sceneCount = 0;

        [UnitySetUp]
        public virtual IEnumerator Setup()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            if (AutomatedQARuntimeSettings.hostPlatform == HostPlatform.Cloud &&
                AutomatedQARuntimeSettings.buildType == BuildType.UnityTestRunner)
            {
                RecordedTesting.SetupCloudUTFTests(NUnit.Framework.TestContext.CurrentContext.Test.FullName);
            }
            else
            {
                RecordedTesting.SetupRecordedTest(NUnit.Framework.TestContext.CurrentContext.Test.FullName);
            }

            // Load scene
            var recordingData = RecordedPlaybackPersistentData.GetRecordingData<RecordingInputModule.InputModuleRecordingData>();
            RecordedPlaybackPersistentData.RecordedResolution = recordingData.recordedResolution;
            RecordedPlaybackPersistentData.RecordedAspectRatio = recordingData.recordedAspectRatio;
            yield return LoadTestScene(recordingData);

            // Start playback
            CentralAutomationController.Instance.Reset();
            CentralAutomationController.Instance.AddAutomator<RecordedPlaybackAutomator>();
            CentralAutomationController.Instance.Run();

        }

        [UnityTearDown]
        public virtual IEnumerator UnityTearDown()
        {
            if (CentralAutomationController.Exists())
            {
                CentralAutomationController.Instance.Reset();
            }
            ReportingManager.CreateMonitoringService();
            var emptyScene = SceneManager.CreateScene("emptyscene" + sceneCount++);
            SceneManager.SetActiveScene(emptyScene);
            yield return UnloadScenesExcept(emptyScene.name);
        }

        public IEnumerator LoadTestScene(RecordingInputModule.InputModuleRecordingData recordingData)
        {
            Debug.Log("Load Scene");
            var loadSceneAsync = SceneManager.LoadSceneAsync(recordingData.entryScene);
            while (!loadSceneAsync.isDone)
            {
                yield return null;
            }

            yield return WaitForFirstActiveScene(recordingData, 60);
        }

        public IEnumerator WaitForFirstActiveScene(RecordingInputModule.InputModuleRecordingData recordingData, int timeoutSecs)
        {
            var touchData = recordingData.GetAllTouchData();
            if (touchData.Count > 0)
            {
                var startTime = DateTime.UtcNow;
                var firstActionScene = touchData[0].scene;
                if (!string.IsNullOrEmpty(firstActionScene) && SceneManager.GetActiveScene().name != firstActionScene)
                {
                    Debug.Log($"Waiting for scene {firstActionScene} to load");
                }
                while (!string.IsNullOrEmpty(firstActionScene) && SceneManager.GetActiveScene().name != firstActionScene)
                {
                    Debug.Log(DateTime.UtcNow.Subtract(startTime).TotalSeconds);
                    if (DateTime.UtcNow.Subtract(startTime).TotalSeconds >= timeoutSecs)
                    {
                        Debug.LogError($"Timeout wile waiting for scene {firstActionScene} to load");
                        break;
                    }
                    yield return new WaitForSeconds(1);
                }
            }
        }
        
        private IEnumerable UnloadScenesExcept(string sceneName) {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name != sceneName)
                {
                    var unloadSceneAsync = SceneManager.UnloadSceneAsync(scene);
                    while (!unloadSceneAsync.isDone)
                    {
                        yield return null;
                    }
                }
            }
        }
    }

}
#endif