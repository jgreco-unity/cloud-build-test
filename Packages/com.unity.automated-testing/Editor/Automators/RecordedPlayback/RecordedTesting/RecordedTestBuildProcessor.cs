using System;
using System.Collections.Generic;
using System.IO;
using Unity.CloudTesting.Editor;
using Unity.RecordedPlayback;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.RecordedTesting.Editor
{
    public class IOSRecordedTestBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder { get; }

        private static string resourcesPath = "Assets/AutomatedQA/Temp/Resources";
        
        private HashSet<string> createdFiles = new HashSet<string>();
        private HashSet<string> createdDirs = new HashSet<string>();

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!CloudTestPipeline.IsRunningOnCloud() && (report.summary.options & BuildOptions.IncludeTestAssemblies) != 0)
            {
                createdFiles = new HashSet<string>();
                createdDirs = new HashSet<string>();
                CreateDirectoryfNotExists(resourcesPath);

                foreach (var testdata in RecordedTesting.GetAllRecordedTests())
                {
                    string sourceFromEditor = Path.Combine(Application.dataPath, testdata.recording);
                    string destInResources = Path.Combine(resourcesPath, testdata.recording);

                    if (File.Exists(sourceFromEditor))
                    {
                        CreateDirectoryfNotExists(Path.GetDirectoryName(destInResources));
                        createdFiles.UnionWith(RecordedPlaybackPersistentData.CopyRecordingFile(sourceFromEditor, destInResources));
                    }
                    else
                    {
                        Debug.LogError($"file {sourceFromEditor} doesn't exist");
                    }
                }
            }
        }
        
        public void OnPostprocessBuild(BuildReport report)
        {
            if (!CloudTestPipeline.IsRunningOnCloud() && (report.summary.options & BuildOptions.IncludeTestAssemblies) != 0)
            {
                foreach (var file in createdFiles)
                {
                    DeleteFileIfExists(file);
                }
                Debug.Log(string.Join(", ", createdDirs));
                foreach (var dir in createdDirs)
                {
                    DeleteDirectoryIfExists(dir);
                }
            }
        }
                
        private void CreateDirectoryfNotExists(string path)
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
        
        private void DeleteFileIfExists(string file)
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
        
        private void DeleteDirectoryIfExists(string dir)
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
