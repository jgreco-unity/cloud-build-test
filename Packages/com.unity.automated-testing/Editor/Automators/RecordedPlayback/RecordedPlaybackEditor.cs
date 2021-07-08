using System;
using System.Collections.Generic;
using System.IO;
using Unity.AutomatedQA;
using UnityEditor;

namespace Unity.RecordedPlayback.Editor
{
    public static class RecordedPlaybackEditorUtils
    {
        private static HashSet<string> createdFiles = new HashSet<string>();
        private static HashSet<string> createdDirs = new HashSet<string>();

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

        public static void CreateDirectoryIfNotExists(string path)
        {
            var folders = path.Split(Path.DirectorySeparatorChar);
            var dir = "";
            foreach (var folder in folders)
            {
                dir = Path.Combine(dir, folder);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    createdDirs.Add(dir);
                }
            }
        }

        public static void MarkFileAsCreated(string path)
        {
            createdFiles.Add(path);
        }

        public static void MarkFilesAsCreated(List<string> paths)
        {
            createdFiles.UnionWith(paths);
        }

        public static void ClearCreatedPaths()
        {
            foreach (var file in createdFiles)
            {
                DeleteFileIfExists(file);
            }
            createdFiles.Clear();
            foreach (var dir in createdDirs)
            {
                DeleteDirectoryIfExists(dir);
            }
            createdDirs.Clear();
        }

        private static void DeleteFileIfExists(string file)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
            var metaFile = file + ".meta";
            if (File.Exists(metaFile))
            {
                File.Delete(metaFile);
            }
        }

        private static void DeleteDirectoryIfExists(string dir)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
            var metaFile = dir + ".meta";
            if (File.Exists(metaFile))
            {
                File.Delete(metaFile);
            }
        }
    }
}