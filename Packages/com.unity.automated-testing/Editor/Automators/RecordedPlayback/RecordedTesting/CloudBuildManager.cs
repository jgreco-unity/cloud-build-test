using System;
using System.IO;
using Unity.CloudTesting.Editor;
using UnityEngine;

namespace Unity.RecordedTesting.Editor
{
    public class CloudBuildManager
    {
        public static void PostExport(string exportPath)
        {
            var authToken = Environment.GetEnvironmentVariable("AUTH_TOKEN");
            Debug.Log("CloudBuildPostExport: Build started - " + authToken);
            CloudTestBuilder.UploadAndRunTests();
            Debug.Log($"CloudBuildPostExport: Build completed - {CloudTestPipeline.BuildPath} {File.Exists(CloudTestPipeline.BuildPath)}");
        }
    }
}