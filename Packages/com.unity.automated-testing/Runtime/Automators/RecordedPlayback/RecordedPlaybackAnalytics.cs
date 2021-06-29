using Unity.AutomatedQA;
using UnityEditor;
using UnityEngine;

namespace Unity.RecordedPlayback
{
// Test Analytics
public class RecordedPlaybackAnalytics
{
    // Required constants
    const int k_MaxEventsPerHour = 1000;
    const int k_MaxNumberOfElements = 1000;
    const string k_VendorKey = "unity.testing";
    const string creationEvent = "RecordingCreation";
    const string executionEvent = "RecordingExecution";
    const string environmentEvent = "RecordedPlaybackEnv";

    //// Analytics data
    public struct RecordingCreation
    {
        public string recordingFileName;
        public long recordingSize;
        public int numRecordingActions;
        public int swipeActions;
        public int waitOrEmitSignals;

        public RecordingCreation(string recordingFileName, long recordingSize, int numRecordingActions, int swipeActions,
            int waitOrEmitSignals)
        {
            this.recordingFileName = recordingFileName;
            this.recordingSize = recordingSize;
            this.numRecordingActions = numRecordingActions;
            this.swipeActions = swipeActions;
            this.waitOrEmitSignals = waitOrEmitSignals;
        }
        
    }

    public struct RecordingExecution
    {
        public string recordingName;
        public string sceneName;
        public bool testPassed;
        public int testDuration;
        public string executionType;
        public string testType;
        public string tags;

        public RecordingExecution(string recordingName, string sceneName, bool testPassed, int testDuration)
        {
            this.recordingName = recordingName;
            this.sceneName = sceneName;
            this.testPassed = testPassed;
            this.testDuration = testDuration;
            executionType = ReportingManager.IsAutomatorTest ? "automated_run" : "recording";
            testType = ReportingManager.IsTestWithoutRecordingFile ? "script" :
                ReportingManager.IsCompositeRecording ? "composite" : "simple";
            tags = Application.isBatchMode ? "batch_mode" : "";
        }
    }
    
    public struct RecordedPlaybackEnv
    {
        public string platform;
        public string buildType;
        public string hostPlatform;
        public string recordingFileStorage;
        public bool newInputEnabled;
        public bool oldInputEnabled;

        public RecordedPlaybackEnv(string platform, string buildType, string hostPlatform, string recordingFileStorage, bool newInputEnabled, bool oldInputEnabled)
        {
            this.platform = platform;
            this.buildType = buildType;
            this.hostPlatform = hostPlatform;
            this.recordingFileStorage = recordingFileStorage;
            this.newInputEnabled = newInputEnabled;
            this.oldInputEnabled = oldInputEnabled;
        }
    }

    public static void SendRecordingCreation(string recordingName, long recordingSize, int numRecordingActions)
    {
        SendEvent(new RecordingCreation(recordingName, recordingSize, numRecordingActions, 0, 0), creationEvent);
    }

    public static void SendRecordingExecution(string recordingName, string sceneName, bool testPassed, int testDuration)
    {
        var recordingExecution = new RecordingExecution(recordingName, sceneName, testPassed, testDuration);
        SendEvent(recordingExecution, executionEvent);
    }
    
    public static void SendRecordedPlaybackEnv()
    {
        var oldInputEnabled = false;
        var newInputEnabled = false;
        
#if ENABLE_INPUT_SYSTEM
        newInputEnabled = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        oldInputEnabled = true;
#endif
        
        var env = new RecordedPlaybackEnv(
            Application.platform.ToString(),
            AutomatedQARuntimeSettings.buildType.ToString(),
            AutomatedQARuntimeSettings.hostPlatform.ToString(),
            AutomatedQARuntimeSettings.recordingFileStorage.ToString(),
            newInputEnabled,
            oldInputEnabled
        );
        
        SendEvent(env, environmentEvent);
    }
    
    private static void SendEvent<T>(T eventObject, string eventName)
    {
        #if UNITY_EDITOR
        if (!EditorAnalytics.enabled)
            return;
        
        EditorAnalytics.RegisterEventWithLimit(eventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);

        EditorAnalytics.SendEventWithLimit(eventName, eventObject);
        #endif
    }
}
}