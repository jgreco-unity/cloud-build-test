using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Unity.AutomatedQA.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace Unity.CloudTesting.Editor
{
    public class CloudTestBuilder
    {
        private static BuildTarget? targetPlatform;
        private static BuildTarget TargetPlatform
        {
          get => targetPlatform?? EditorUserBuildSettings.activeBuildTarget;
          set => targetPlatform = value;
        }

#if UNITY_IOS
        [PostProcessBuild]
        public static void ChangeXcodePlist(BuildTarget buildTarget, string pathToBuiltProject)
        {
            // Adds UIFileSharingEnabled to Info.plist
            if (buildTarget == BuildTarget.iOS && CloudTestPipeline.IsRunningOnCloud())
            {
                // Get plist
                string plistPath = pathToBuiltProject + "/Info.plist";
                PlistDocument plist = new PlistDocument();
                plist.ReadFromString(File.ReadAllText(plistPath));

                // Get root
                PlistElementDict rootDict = plist.root;
                rootDict.SetBoolean("UIFileSharingEnabled", true);
                // rootDict.SetString("CFBundleDisplayName", "CloudBundle");
                // Write to Info.plist
                Debug.Log("Updating Info.plist");
                File.WriteAllText(plistPath, plist.WriteToString());
            }
        }
#endif

        public static void CreateLocalBuild()
        {
            // Editor code flag for utf cloud workflows
            CloudTestPipeline.SetTestRunOnCloud(true);

            // Setting scripting defines
            AutomatedQAEditorSettings.ApplyBuildFlags(EditorUserBuildSettings.selectedBuildTargetGroup);

            var filter = GetTestFilter();

            Debug.Log($"Build target = {EditorUserBuildSettings.activeBuildTarget}");
            CloudTestPipeline.MakeBuild(filter.ToArray());
        }

        public static void CreateBuild()
        {
            ParseCommandLineArgs();
            CloudTestPipeline.BuildFolder = "";
            File.Delete(CloudTestPipeline.BuildPath);
            CloudTestPipeline.SetTestRunOnCloud(true);
            Debug.Log("Creating Build for platform " + TargetPlatform);
            AutomatedQAEditorSettings.ApplyBuildFlags(BuildPipeline.GetBuildTargetGroup(TargetPlatform));
            var filter = GetTestFilter();
            CloudTestPipeline.testBuildFinished += () => Debug.Log($"Build {CloudTestPipeline.BuildPath} complete");
            CloudTestPipeline.MakeBuild(filter.ToArray(), TargetPlatform);
            if (Application.isBatchMode)
            {
                EditorApplication.update.Invoke();
            }
        }

        public static CloudTestPipeline.UploadUrlResponse BuildAndUpload()
        {
            CreateBuild();
            var uploadUrlResponse = CloudTestPipeline.UploadBuild();
            Debug.Log($"Uploaded build with id {uploadUrlResponse.id}");
            return uploadUrlResponse;
        }

        public static void BuildAndRunTests()
        {
            var cloudTests = new List<string>(new[] { "DummyUTFTest" });

            var uploadUrlResponse = BuildAndUpload();
            Thread.Sleep(TimeSpan.FromSeconds(30f)); // wait before triggering tests to avoid failure

            Debug.Log($"Running Cloud Tests: {string.Join(",", cloudTests)}");
            var jobStatusResponse = CloudTestPipeline.RunCloudTests(uploadUrlResponse.id, cloudTests);

            AwaitTestResults(jobStatusResponse.jobId);
        }

        private static void AwaitTestResults(string jobId)
        {
            ParseCommandLineArgs();
            var jobStatusResponse = CloudTestPipeline.GetJobStatus(jobId);
            while (jobStatusResponse.status != "COMPLETED" && jobStatusResponse.status != "ERROR" && jobStatusResponse.status != "UNKNOWN")
            {
                Thread.Sleep(TimeSpan.FromSeconds(60f));
                jobStatusResponse = CloudTestPipeline.GetJobStatus(jobStatusResponse.jobId);
            }

            var testResults = CloudTestPipeline.GetTestResults(jobStatusResponse.jobId);
            if (Application.isBatchMode && !testResults.allPass)
            {
                throw new Exception("Job has completed but there are failing tests");
            }

            Debug.Log("All tests passed");
        }

        private static void ParseCommandLineArgs()
        {
            var accessToken = GetArgValue("token");
            if (!string.IsNullOrEmpty(accessToken))
            {
                CloudTestPipeline.AccessToken = accessToken;
            }

            string testPlatformStr = GetArgValue("testPlatform");
            if (!string.IsNullOrEmpty(testPlatformStr))
            {
                if (!Enum.TryParse(testPlatformStr, true, out BuildTarget testPlatform))
                {
                    throw new Exception($"Invalid testPlatform {testPlatformStr}, please use a valid BuildTarget");
                }
                TargetPlatform = testPlatform;
            }
        }

        private static string GetArgValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == $"-{name}" && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
                if (args[i].StartsWith($"-{name}="))
                {
                    return args[i].Split('=')[1];
                }
            }

            return null;
        }

        private static List<string> GetTestFilter()
        {
            var cloudTests = CloudTestPipeline.GetCloudTests();
            var filter = new List<string>();
            foreach (var tests in cloudTests.Values)
            {
                foreach (var test in tests)
                {
                    Debug.Log("Adding Test: " + test);
                    filter.Add(test);
                }
            }

            return filter;
        }
    }
}