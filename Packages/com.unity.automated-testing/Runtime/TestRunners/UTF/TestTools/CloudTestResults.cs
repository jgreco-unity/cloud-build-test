#if UNITY_INCLUDE_TESTS && AQA_PLATFORM_CLOUD && AQA_BUILD_TYPE_UTR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

using UnityEngine;
using Unity.RecordedTesting;
using Debug = UnityEngine.Debug;
using NUnit.Framework.Interfaces;
using Unity.RecordedTesting.Runtime;
using UnityEditor;
using UnityEngine.TestRunner;
using UnityEngine.Scripting;
using Random = System.Random;

[assembly:Preserve]
[assembly:TestRunCallback(typeof(CloudTestResults))]
namespace Unity.RecordedTesting
{
    
    public class CloudTestResults: ITestRunCallback
    {

        private DeviceFarmConfig _dfConf;

        public void RunStarted(ITest testsToRun)
        {
            _dfConf = CloudTestManager.Instance.GetDeviceFarmConfig();
        }

        public void RunFinished(ITestResult testResults)
        {
            //TODO: Filter UTF at runtime with testNames
            Debug.Log("Unity Test Completed: DummyUTFTest");
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void TestStarted(ITest test) { }

        public void TestFinished(ITestResult result)
        {
            if (!result.Test.IsSuite)
            {

                //TODO: Add xml reports to artifacts
                // var xmlResult = result.ToXml(true).OuterXml;
                CloudTestManager.Instance.ResetTestResults();
                CloudTestManager.Instance.SetCounter(result.Name, result.ResultState.Status == TestStatus.Passed ? 1 : 0);
                var dfConfOverrides = new DeviceFarmOverrides(result.FullName);
                if (_dfConf.awsDeviceUDID == null)
                {
                    dfConfOverrides.awsDeviceUDID = "Local-Device-UDID";
                }
                CloudTestManager.UploadCounters(dfConfOverrides);
            }
        }
    }
}
#endif