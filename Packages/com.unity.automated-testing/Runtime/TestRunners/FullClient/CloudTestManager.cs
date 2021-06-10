using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.AutomatedQA;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.RecordedTesting
{
    public class CloudTestManager
    {
        private Dictionary<string, Counter> _testResults = new Dictionary<string, Counter>();
        static object _mutex = new object();

        private static CloudTestManager _instance;
        public static CloudTestManager Instance => _instance ?? (_instance = new CloudTestManager());

        private string deviceFarmConfigFile = "config.json";

        public void ResetTestResults()
        {
            lock (_mutex)
            {
                _testResults = new Dictionary<string, Counter>();
            }
        }

        private static byte[] GetBytes(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return bytes;
        }


        public static void UploadCounters(DeviceFarmOverrides dfConfOverrides = null)
        {
            var testResults = CloudTestManager.Instance.GetTestResults(dfConfOverrides);
            Debug.Log($"Uploading counters {testResults.ToString()}");
            var postUrl = $"{AutomatedQARuntimeSettings.DEVICE_TESTING_API_ENDPOINT}/v1/counters";
            Debug.Log($"Counters = {testResults}");
            byte[] counterPayload = GetBytes(testResults);
            UploadHandlerRaw uH = new UploadHandlerRaw(counterPayload);
            uH.contentType = "application/json";


            using (var uwr = new UnityWebRequest(postUrl, UnityWebRequest.kHttpVerbPOST))
            {
                uwr.uploadHandler = uH;
                AsyncOperation request = uwr.SendWebRequest();

                while (!request.isDone)
                {
                }

                if (uwr.isNetworkError || uwr.isHttpError)
                {
                    Debug.LogError($"Couldn't upload counters. Error - {uwr.error}");
                }
                else
                {
                    Debug.Log($"Uploaded counters.");
                }

            }
        }

        public DeviceFarmConfig GetDeviceFarmConfig(DeviceFarmOverrides dfOverrides = null)
        {
            string configPath = Path.Combine(Application.persistentDataPath, deviceFarmConfigFile);
            // Read env and testName
            string configStr = File.ReadAllText(configPath);
            DeviceFarmConfig dfConf = JsonUtility.FromJson<DeviceFarmConfig>(configStr.ToString());
            if (dfOverrides != null)
            {
                dfConf.testName = dfOverrides.testName;
                dfConf.awsDeviceUDID = dfOverrides.awsDeviceUDID;
            }
            Debug.Log($"Test name - {dfConf.testName}, Config - {dfConf.ToString()}");
            // string dfConfStr = JsonUtility.ToJson(dfConf);
            return dfConf;
        }
        internal Counter GetCounter(string name)
        {
            if (!_testResults.ContainsKey(name))
            {
                lock (_mutex)
                {
                    // Verify the counter still doesn't exist after acquiring the lock
                    if (!_testResults.ContainsKey(name))
                    {
                        var counter = new Counter(name);
                        _testResults[name] = counter;
                    }
                }
            }
            return _testResults[name];
        }

        public void SetCounter(string name, Int64 value)
        {
            GetCounter(name).Reset(value);
            Debug.Log($"Set counter {name} and counters dict is now - {JsonUtility.ToJson(_testResults).ToString()}");
        }



        public string GetTestResults(DeviceFarmOverrides dfConfOverrides = null)
        {
            DeviceFarmConfig dfConf = GetDeviceFarmConfig(dfConfOverrides);
            string dfConfStr = JsonUtility.ToJson(dfConf);
            Metadata metadata = new Metadata(dfConf.awsDeviceUDID, attemptId: "", dfConfStr);
            lock (_mutex)
            {
                var countersData = new CountersData(_testResults, metadata);
                return JsonUtility.ToJson(countersData);
            }
        }

    }
}