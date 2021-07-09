using System;
using System.IO;
using Unity.CloudTesting.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.RecordedTesting.Editor
{
    public class CloudBuildManager
    {
        public static void PostExport(string exportPath)
        {
            var authToken = Environment.GetEnvironmentVariable("AUTH_TOKEN");
            Debug.Log("CloudBuildPostExport: Build started");
            try
            {
                CloudTestPipeline.AccessToken = authToken;
                CloudTestBuilder.TargetPlatform = BuildTarget.Android;
                CloudTestBuilder.BuildAndRunTests();
            }
            finally
            {
                Debug.Log($"CloudBuildPostExport: Build completed - {CloudTestPipeline.BuildPath} {File.Exists(CloudTestPipeline.BuildPath)}");
            }
        }
    }
}