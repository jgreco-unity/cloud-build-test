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
        private string       _token;
        
        public BuildPlayerOptions ModifyOptions(BuildPlayerOptions playerOptions)
        {
            if (CloudTestPipeline.IsRunningOnCloud())
            {
                playerOptions.options &= ~(BuildOptions.AutoRunPlayer);
                playerOptions.locationPathName = CloudTestPipeline.BuildPath;

                return playerOptions;    
            }
            else
            {
                return playerOptions;
            }
            
        }
    }
}
