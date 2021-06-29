using System;
using System.Collections.Generic;
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
        public static readonly BuildTarget[] SupportedBuildTargets = { BuildTarget.Android, BuildTarget.iOS };

        private List<string> allCloudTests = new List<string>();
        private CloudTestPipeline.JobStatusResponse jobStatus = new CloudTestPipeline.JobStatusResponse();
        private CloudTestPipeline.UploadUrlResponse uploadInfo = new CloudTestPipeline.UploadUrlResponse();
        private CloudTestPipeline.BundleUpload _bundleUpload = new CloudTestPipeline.BundleUpload();
        private DateTime lastRefresh = DateTime.UtcNow;

        [MenuItem("Automated QA/Cloud Test Runner...", priority=AutomatedQAEditorSettings.MenuItems.CloudTestRunner)]
        public static void ShowWindow()
        {
            CloudTestingWindow wnd = GetWindow<CloudTestingWindow>();
            wnd.titleContent = new GUIContent("Cloud Test Runner");
            wnd.Show();
        }

        private void OnEnable()
        {
            allCloudTests.Clear();
            var cloudDict = CloudTestPipeline.GetCloudTests();
            foreach (var testlist in cloudDict)
            {
                allCloudTests.AddRange(testlist.Value);
            }
        }

        private void OnGUI()
        {
            if (!SupportedBuildTargets.Contains(EditorUserBuildSettings.activeBuildTarget))
            {
                GUILayout.Label($"Supported Build Targets: {String.Join(",", SupportedBuildTargets)}");
                return;
            }

            GUITestList();
            GUILayout.FlexibleSpace();
            GUIEmailUs();
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
            if (GUILayout.Button("Please email us at AutomatedQA@unity3d.com for access (if you have not already).", EditorStyles.linkLabel))
            {
                Application.OpenURL("mailto:AutomatedQA@unity3d.com");
            }
        }

        void GUIBuild()
        {
            _bundleUpload.buildName = $"CloudBundle.{CloudTestPipeline.BuildFileExtension}";
            if (GUILayout.Button("Build & Upload") && EditorUtility.DisplayDialog("Confirm Build", "Usage of the Unity editor will be blocked until the build process is complete.",
                "Continue", "Cancel"))
            {
                _bundleUpload.buildPath = CloudTestPipeline.BuildPath;
                DoBuildAndUpload();
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
                CloudTestPipeline.GetTestResults(jobStatus.jobId);
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

            jobStatus = CloudTestPipeline.GetJobStatus(jobStatus.jobId);
        }

        void DoBuildAndUpload()
        {
            CloudTools.UploadAllRecordings(RecordingUploadWindow.SetAllTestsAndRecordings());
            Debug.Log("Uploaded all recordings.");
            CloudTestBuilder.CreateLocalBuild();
            CloudTestPipeline.testBuildFinished += OnBuildFinish;
        }

        private void RunOnDeviceFarm(string buildId)
        {
            // TODO use allCloudTests
            var runTests = new List<string>(new string[] { "DummyUTFTest" });

            jobStatus = CloudTestPipeline.RunCloudTests(buildId, runTests);
        }

        private void OnBuildFinish()
        {
#if UNITY_IOS
            CloudTestPipeline.ArchiveIpa();
#endif
            CloudTestPipeline.testBuildFinished -= OnBuildFinish;

            uploadInfo = CloudTestPipeline.GetUploadURL();
            CloudTestPipeline.UploadBuildToUrl(uploadInfo.upload_uri, _bundleUpload.buildPath);

            // TODO Query upload status
            EditorUtility.DisplayProgressBar("Wait for upload status", "Wait for upload status", 0);
            EditorUtility.ClearProgressBar();
        }

        //[MenuItem("Automated QA/Dev/Run Debug Cloud Test")]
        static void RunDebugCloudTest()
        {
            CloudTestPipeline.RunCloudTests("f3678dc4-2218-4320-a4c8-41ff5cdd7b22", new List<string>(new string[] { "DummyUTFTest" }));
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