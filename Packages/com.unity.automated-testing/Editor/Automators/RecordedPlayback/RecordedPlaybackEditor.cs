using System;
using System.IO;
using Unity.AutomatedQA;
using UnityEditor;
using UnityEngine;

namespace Unity.RecordedPlayback.Editor
{
    public static class RecordedPlaybackEditorUtils
    {
        public static void SaveCurrentRecordingDataAsProjectAsset()
        {
            // TODO use project setting to determine output path
            string recordingIdentifier = DateTime.Now.ToString("yyyy-MM-dd-THH-mm-ss");
            var outputPath = Path.Combine(AutomatedQARuntimeSettings.RecordingDataPath,
                $"recording-{recordingIdentifier}.json");
            var mainFile = RecordedPlaybackPersistentData.GetRecordingDataFilePath();

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.Copy(mainFile, outputPath, false);
            var recordingFiles = RecordedPlaybackPersistentData.GetSegmentFiles(mainFile);
            foreach (var recordingFile in recordingFiles)
            {
                var segmentPath = Path.Combine(Path.GetDirectoryName(mainFile), recordingFile);
                var destPath = Path.Combine(AutomatedQARuntimeSettings.RecordingDataPath, recordingFile);
                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                File.Copy(segmentPath, destPath, true);
            }
            AssetDatabase.Refresh();

            RecordedPlaybackAnalytics.SendRecordingCreation(outputPath, new System.IO.FileInfo(outputPath).Length, -1);
        }
    }
}