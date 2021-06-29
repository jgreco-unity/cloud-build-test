using System;
using System.Collections.Generic;
using System.IO;
using Unity.CloudTesting.Editor;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEngine;

[assembly:TestPlayerBuildModifier(typeof(CloudTestRunner))]


namespace Unity.CloudTesting.Editor
{
    public class CloudTestRunner: ITestPlayerBuildModifier
    {
        public BuildPlayerOptions ModifyOptions(BuildPlayerOptions playerOptions)
        {
            if (CloudTestPipeline.IsRunningOnCloud())
            {
                playerOptions.options &= ~(BuildOptions.AutoRunPlayer);
#if UNITY_IOS
                playerOptions.locationPathName = Path.Combine(CloudTestPipeline.BuildFolder, Application.identifier);
#else
                playerOptions.locationPathName = CloudTestPipeline.BuildPath;
#endif

                return playerOptions;    
            }

            return playerOptions;
        }
    }
}
