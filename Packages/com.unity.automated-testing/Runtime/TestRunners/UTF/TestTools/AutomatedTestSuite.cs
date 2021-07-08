#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.AutomatedQA.TestTools
{
    public abstract class AutomatedTestSuite
    {
        private int sceneCount = 0;
        protected string testName;

        [UnitySetUp]
        public virtual IEnumerator Setup()
        {
            testName = NUnit.Framework.TestContext.CurrentContext.Test.FullName;
            yield return null;
        }

        [UnityTearDown]
        public virtual IEnumerator UnityTearDown()
        {
            if (CentralAutomationController.Exists())
            {
                CentralAutomationController.Instance.Reset();
            }

            if (RecordedTesting.RecordedTesting.IsRecordedTest(testName))
            {
                ReportingManager.CreateMonitoringService();
            }
            
            var emptyScene = SceneManager.CreateScene("emptyscene" + sceneCount++);
            SceneManager.SetActiveScene(emptyScene);
            yield return UnloadScenesExcept(emptyScene.name);
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

        protected AutomatedRun GetAutomatedRun(string assetPath, string resourceName)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<AutomatedRun>(assetPath);
#else
            return Resources.Load<AutomatedRun>(Path.Combine("AutomatedRuns", resourceName));
#endif
        }
        
        protected IEnumerator LaunchAutomatedRun(AutomatedRun myRun)
        {
            ReportingManager.CurrentTestName = myRun.name;
            ReportingManager.IsAutomatorTest = true;
            // Run automation until complete
            CentralAutomationController controller = CentralAutomationController.Instance;
            controller.Run(myRun.config);
            while (!controller.IsAutomationComplete())
            {
                yield return null;
            }

        }
    }

}
#endif