using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Unity.AutomatedQA.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.CloudTesting.Editor
{
    public class CloudTestBuilder
    {
        
        public static void CreateLocalBuild()
        {
            Debug.Log("Executing BuildAndRunTests");
            
            // Editor code flag for utf cloud workflows
            CloudTestPipeline.SetTestRunOnCloud(true);
            
            // Setting scripting defines
            AutomatedQAEditorSettings.ApplyBuildFlags(EditorUserBuildSettings.selectedBuildTargetGroup);
            
            var filter = GetTestFilter();
            
            Debug.Log($"Build target = {EditorUserBuildSettings.activeBuildTarget}");
            CloudTestPipeline.MakeBuild(filter.ToArray());
        }
        
        public static void BuildAndroid()
        {
            CloudTestPipeline.BuildPath = "TestBuild.apk";
            File.Delete(CloudTestPipeline.BuildPath);
            CloudTestPipeline.SetTestRunOnCloud(true);
            AutomatedQAEditorSettings.ApplyBuildFlags(BuildTargetGroup.Android);
            var filter = GetTestFilter();
            CloudTestPipeline.testBuildFinished += () => Debug.Log($"Build {CloudTestPipeline.BuildPath} complete");
            CloudTestPipeline.MakeBuild(filter.ToArray(), BuildTarget.Android);
            if (Application.isBatchMode)
            {
                EditorApplication.update.Invoke();
            }
        }

        public static CloudTestPipeline.UploadUrlResponse BuildAndUploadAndroid()
        {
            BuildAndroid();
            var uploadUrlResponse = CloudTestPipeline.UploadBuild(GetAuthToken());
            Debug.Log($"Uploaded build with id {uploadUrlResponse.id}");
            return uploadUrlResponse;
        }

        public static void UploadAndRunTests()
        {
            var cloudTests = new List<string>(new[] { "DummyUTFTest" });

            var accessToken = GetAuthToken();

            // TODO: Upload recordings?

            var uploadUrlResponse = BuildAndUploadAndroid();
            Thread.Sleep(TimeSpan.FromSeconds(30f)); // wait before triggering tests to avoid failure

            Debug.Log($"Running Cloud Tests: {string.Join(",", cloudTests)}");
            var jobStatusResponse = CloudTestPipeline.RunCloudTests(uploadUrlResponse.id, cloudTests, accessToken);

            while (jobStatusResponse.status != "COMPLETED" && jobStatusResponse.status != "ERROR" && jobStatusResponse.status != "UNKNOWN")
            {
                Thread.Sleep(TimeSpan.FromSeconds(60f));
                jobStatusResponse = CloudTestPipeline.GetJobStatus(jobStatusResponse.jobId, accessToken);
            }

            var testResults = CloudTestPipeline.GetTestResults(jobStatusResponse.jobId, accessToken);
            if (Application.isBatchMode && !testResults.allPass)
            {
                throw new Exception("Job has completed but there are failing tests");
            }

            Debug.Log("All tests passed");
        }

        private static string GetAuthToken()
        {
            var envToken = Environment.GetEnvironmentVariable("AUTH_TOKEN");
            if (!string.IsNullOrEmpty(envToken))
                return envToken;

            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-authToken" && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
                if (args[i].StartsWith("-authToken="))
                {
                    return args[i].Split('=')[1];
                }
            }

            return CloudProjectSettings.accessToken;
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