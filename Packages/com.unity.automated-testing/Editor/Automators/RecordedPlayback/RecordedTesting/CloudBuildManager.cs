using Unity.CloudTesting.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.RecordedTesting.Editor
{
    public class CloudBuildManager
    {
        public static void PostExport(string exportPath)
        {
            Debug.Log("CloudBuildPostExport: Build started - " + exportPath);
            CloudTestBuilder.BuildAndroid();
            Debug.Log("CloudBuildPostExport: Build completed - " + CloudProjectSettings.accessToken);
        }
    }
}