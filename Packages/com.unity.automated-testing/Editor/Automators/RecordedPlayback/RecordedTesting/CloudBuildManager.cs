using UnityEngine;

namespace Unity.RecordedTesting.Editor
{
    public class CloudBuildManager
    {
        public static void PostExport(string exportPath)
        {
            Debug.Log("CloudBuildPostExport: " + exportPath);
        }
    }
}