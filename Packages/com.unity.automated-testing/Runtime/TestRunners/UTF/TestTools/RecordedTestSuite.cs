#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using System;
using System.Collections;
using Unity.AutomatedQA;
using Unity.AutomatedQA.TestTools;
using Unity.RecordedPlayback;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static UnityEngine.EventSystems.RecordingInputModule;

namespace Unity.RecordedTesting.TestTools
{
    public abstract class RecordedTestSuite : AutomatedTestSuite
    {
        [UnitySetUp]
        public override IEnumerator Setup()
        {
            yield return base.Setup();
            
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            ReportingManager.InitializeReport();

            if (AutomatedQARuntimeSettings.hostPlatform == HostPlatform.Cloud &&
                AutomatedQARuntimeSettings.buildType == BuildType.UnityTestRunner)
            {
                RecordedTesting.SetupCloudUTFTests(testName);
            }
            else
            {
                RecordedTesting.SetupRecordedTest(testName);
            }

            // Start playback
            CentralAutomationController.Instance.Reset();
            CentralAutomationController.Instance.AddAutomator<RecordedPlaybackAutomator>(new RecordedPlaybackAutomatorConfig
            {
                loadEntryScene = true,
            });
            CentralAutomationController.Instance.Run();

            // wait for playback to start
            while (!RecordedPlaybackController.Exists() || !RecordedPlaybackController.Instance.IsInitialized())
            {
                yield return null;
            }

        }

        
    }

}
#endif