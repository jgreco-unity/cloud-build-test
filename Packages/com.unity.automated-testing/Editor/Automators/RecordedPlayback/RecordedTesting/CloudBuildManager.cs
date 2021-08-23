using System;
using System.IO;
using TestPlatforms.Cloud;
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
            var projectId = Environment.GetEnvironmentVariable("PROJECT_ID");
            Debug.Log("CloudBuildPostExport: Build started");
            try
            {
                if (string.IsNullOrEmpty(projectId))
                {
                    projectId = Application.cloudProjectId;
                }
                CloudTestBuilder.BuildAndRunTests(BuildTarget.Android, authToken, projectId);
            }
            finally
            {
                Debug.Log($"CloudBuildPostExport: Build completed - {CloudTestConfig.BuildPath} {File.Exists(CloudTestConfig.BuildPath)}");
            }
        }
    }
}