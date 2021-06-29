﻿using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Unity.AutomatedQA.EditorTests
{
    public class BuildTests
    {
        // A Test behaves as an ordinary method
        public void HelperPlatformTest(string platformType)
        {

			Debug.Log("Awake:" + SceneManager.GetActiveScene().name);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

			EditorSceneManager.SaveScene(scene, "testScene.unity");

            BuildTarget buildTarget = BuildTarget.NoTarget;
            
            switch (platformType)
            {
                case "win":
                    buildTarget = BuildTarget.StandaloneWindows;
                    break;
                case "mac":
                    buildTarget = BuildTarget.StandaloneOSX;
                    break;
            }
            
            var buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.target = buildTarget;
            buildPlayerOptions.locationPathName = 
                "WillisTestBuild123";
            buildPlayerOptions.options = BuildOptions.AutoRunPlayer | BuildOptions.ShowBuiltPlayer;
            
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            BuildSummary summary = report.summary;

            Debug.Log("Result: " + summary.result + " output path " + summary.outputPath + " total size " + summary.totalSize.ToString());

            Debug.Log(BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, buildTarget));
            // Use the Assert class to test conditions
            Assert.NotZero(summary.totalSize);
        }

        
        /// winPlatformTest
        /// Builds the player for Windows
        /// This uses UTR test filtering, which will filter tests by function name. e.g.:
        /// --extra-utr-arg="--testfilter=win"
        [Test]
        public void winPlatformTest()
        {
            HelperPlatformTest("win");
        }
        
        /// macPlatformTest
        /// Builds the player for Mac
        /// This uses UTR test filtering, which will filter tests by function name. e.g.:
        /// --extra-utr-arg="--testfilter=mac"
        [Test]
        public void macPlatformTest()
        {
            HelperPlatformTest("mac");
        }

    }
}
