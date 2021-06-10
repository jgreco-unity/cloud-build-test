using System;
using System.Collections;
using System.Text;
using Unity.AutomatedQA;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.RecordedTesting.Runtime
{

    public static class RuntimeClient
    {
        
        public static void LogTestCompletion(string testName)
        {
            Debug.Log("Unity Test Completed: " + testName);
        }


        public static void DownloadRecording(string recordingFileName, string resultFileOutputPath)
        {
            var projectId = Application.cloudProjectId;
            var downloadUri =
                $"{AutomatedQARuntimeSettings.GAMESIM_API_ENDPOINT}/v1/recordings/{recordingFileName}/download?projectId={projectId}";

            var dh = new DownloadHandlerFile(resultFileOutputPath);

            dh.removeFileOnAbort = true;
            Debug.Log("Starting download" + downloadUri);
            using (var webrx = UnityWebRequest.Get(downloadUri))
            {

                webrx.downloadHandler = dh;
                AsyncOperation request = webrx.SendWebRequest();

                while (!request.isDone)
                {
                }

                if (webrx.isNetworkError || webrx.isHttpError)
                {
                    Debug.LogError($"Couldn't download file. Error - {webrx.error}");
                }
                else
                {
                    Debug.Log($"Downloaded file saved to {resultFileOutputPath}.");
                }

            }
        }
    }
}