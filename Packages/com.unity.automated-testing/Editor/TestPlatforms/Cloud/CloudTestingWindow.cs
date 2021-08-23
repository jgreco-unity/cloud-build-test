using System;
using System.Collections.Generic;
using TestPlatforms.Cloud;
using Unity.AutomatedQA;
using Unity.AutomatedQA.Editor;
using Unity.CloudTesting.Editor;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Unity.RecordedTesting.Editor
{

    public class CloudTestingWindow : EditorWindow
    {
        public static readonly string[] SupportedBuildTargets = { BuildTarget.Android.ToString(), BuildTarget.iOS.ToString() };
        public static ICloudTestClient Client = new CloudTestClient();

        private List<string> allCloudTests = new List<string>();
        private JobStatusResponse jobStatus = new JobStatusResponse();
        private UploadUrlResponse uploadInfo = new UploadUrlResponse();
        private BundleUpload _bundleUpload = new BundleUpload();
        private DateTime lastRefresh = DateTime.UtcNow;
        private int buildTargetIndex = 0;
        private BuildTarget originalBuildTarget;
        private BuildTarget buildTarget;
        private bool createBuild = false;


        [MenuItem("Automated QA/Cloud Test Runner...", priority=AutomatedQAEditorSettings.MenuItems.CloudTestRunner)]
        public static void ShowWindow()
        {
            CloudTestingWindow wnd = GetWindow<CloudTestingWindow>();
            wnd.titleContent = new GUIContent("Cloud Test Runner");
            wnd.Show();
        }

        private void OnEnable()
        {
            var editorBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
            if (SupportedBuildTargets.Contains(editorBuildTarget))
            {
                buildTargetIndex = Array.IndexOf(SupportedBuildTargets, editorBuildTarget);
            }
            allCloudTests.Clear();
            var cloudDict = CloudTestPipeline.GetCloudTests();
            foreach (var testlist in cloudDict)
            {
                allCloudTests.AddRange(testlist.Value);
            }
        }

        private void OnGUI()
        {
            GUITestList();
            GUILayout.FlexibleSpace();
            GUIEmailUs();
            GUIPlatformSelect();
            GUIBuild();

            GUIRunButton();
            GUILayout.FlexibleSpace();

            GUIResults();
            GUILayout.FlexibleSpace();
        }

        void GUITestList()
        {
            EditorGUILayout.LabelField("Tests", EditorStyles.boldLabel);
            EditorGUILayout.BeginScrollView(Vector2.zero);

            foreach (var test in allCloudTests)
            {
                EditorGUILayout.LabelField(test.ToString());
            }
            EditorGUILayout.EndScrollView();
        }

        void GUIEmailUs()
        {
            if (GUILayout.Button("Please email us at AutomatedQA@unity3d.com for information on increasing your usage limit.", EditorStyles.linkLabel))
            {
                Application.OpenURL("mailto:AutomatedQA@unity3d.com");
            }
        }

        void GUIPlatformSelect()
        {
            buildTargetIndex = EditorGUILayout.Popup("Target Platform", buildTargetIndex, SupportedBuildTargets);
        }

        void GUIBuild()
        {
            var buildTargetStr = SupportedBuildTargets[buildTargetIndex];
            var msg = "Usage of the Unity editor will be blocked until the build process is complete.";
            if (buildTargetStr != EditorUserBuildSettings.activeBuildTarget.ToString())
            {
                msg += $"\n\nActive build target {EditorUserBuildSettings.activeBuildTarget} does not match the selected target {buildTargetStr} which will increase the compilation time.";
            }
            if (GUILayout.Button("Build & Upload") && EditorUtility.DisplayDialog("Confirm Build", msg,
                "Continue", "Cancel"))
            {
                buildTarget = (BuildTarget) Enum.Parse(typeof(BuildTarget), buildTargetStr);
                DoBuildAndUpload();
            }
        }

        private void Update()
        {
            if (createBuild && !EditorApplication.isCompiling)
            {
                createBuild = false;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS));
                _bundleUpload.buildName = $"CloudBundle{CloudTestConfig.BuildFileExtension}";
                _bundleUpload.buildPath = CloudTestConfig.BuildPath;
                CloudTestBuilder.CreateBuild(buildTarget);
                CloudTestPipeline.testBuildFinished += OnBuildFinish;
            }
        }

        void GUIRunButton()
        {
            uploadInfo.id = EditorGUILayout.TextField("Build id", uploadInfo.id);
            if (GUILayout.Button("Run on Device Farm"))
            {
                RunOnDeviceFarm(uploadInfo.id);
            }
        }

        void GUIResults()
        {
            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);
            GUIJobStatus();
            GUIGetResultsButton();
        }

        void GUIJobStatus()
        {
            jobStatus.jobId = EditorGUILayout.TextField("jobId", jobStatus.jobId);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("status", jobStatus.status);
            EditorGUI.BeginDisabledGroup((DateTime.UtcNow - lastRefresh).TotalSeconds < 1);
            if (GUILayout.Button("Refresh"))
            {
                RefreshJobStatus();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        void GUIGetResultsButton()
        {
            EditorGUI.BeginDisabledGroup(jobStatus.status != "COMPLETED");
            if (GUILayout.Button("Get Test Results"))
            {
                RefreshJobStatus();
                if (jobStatus.status != "COMPLETED")
                {
                    Debug.LogError("Job not finished");
                    return;
                }
                Client.GetTestResults(jobStatus.jobId);
            }
            EditorGUI.EndDisabledGroup();
        }

        void RefreshJobStatus()
        {
            if (string.IsNullOrEmpty(jobStatus.jobId.Trim()))
            {
                return;
            }
            if ((DateTime.UtcNow - lastRefresh).TotalSeconds < 1)
            {
                return;
            }
            lastRefresh = DateTime.UtcNow;

            jobStatus = Client.GetJobStatus(jobStatus.jobId);
        }

        void DoBuildAndUpload()
        {
            CloudTools.UploadAllRecordings(RecordingUploadWindow.SetAllTestsAndRecordings());
            Debug.Log("Uploaded all recordings.");
            originalBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            if (buildTarget != EditorUserBuildSettings.activeBuildTarget)
            {
                Debug.Log("switching build target");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(buildTarget), buildTarget);
            }
            createBuild = true;
        }

        private void RunOnDeviceFarm(string buildId)
        {
            // TODO use allCloudTests
            var runTests = new List<string>(new string[] { "DummyUTFTest" });

            jobStatus = Client.RunCloudTests(buildId, runTests);
        }

        private void OnBuildFinish()
        {
#if UNITY_IOS
            CloudTestPipeline.ArchiveIpa();
#endif
            CloudTestPipeline.testBuildFinished -= OnBuildFinish;

            Debug.Log($"Build successfully saved at - {_bundleUpload.buildPath}");
            if (originalBuildTarget != EditorUserBuildSettings.activeBuildTarget)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(originalBuildTarget), originalBuildTarget);
            }

            uploadInfo = Client.GetUploadURL();
            Client.UploadBuildToUrl(uploadInfo.upload_uri, _bundleUpload.buildPath);

            // TODO Query upload status
            EditorUtility.DisplayProgressBar("Wait for upload status", "Wait for upload status", 0);
            EditorUtility.ClearProgressBar();
        }

        //[MenuItem("Automated QA/Dev/Run Debug Cloud Test")]
        static void RunDebugCloudTest()
        {
            Client.RunCloudTests("f3678dc4-2218-4320-a4c8-41ff5cdd7b22", new List<string>(new string[] { "DummyUTFTest" }));
        }

        //   [MenuItem("Automated QA/Dev/Log Project ID")]
        static void LogProjectID()
        {
            Debug.Log($"" +
                      $"Application.cloudProjectId: {Application.cloudProjectId}\n" +
                      $"CloudProjectSettings.accessToken: {CloudProjectSettings.accessToken}\n" +
                      $"");
        }

    }
}