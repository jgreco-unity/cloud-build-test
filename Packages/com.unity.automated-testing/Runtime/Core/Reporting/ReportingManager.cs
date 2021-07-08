using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using Unity.AutomatedQA;
using Unity.RecordedPlayback;
using UnityEngine.SceneManagement;
using Unity.RecordedTesting;
using System.IO;
using UnityEngine.EventSystems;

public static class ReportingManager
{
    public static string ReportSaveDirectory
    {
        get
        {
            if (string.IsNullOrEmpty(_reportSaveDirectory))
                _reportSaveDirectory = Path.Combine(Application.persistentDataPath, AutomatedQARuntimeSettings.PackageAssetsFolderName, "Report");
            return _reportSaveDirectory;
        }
    }

    public static string ReportFileNameWithoutExtension
    {
        get
        {
            return "report";
        }
    }

    // Serialize CurrentTestName to store name of automated run, which is set before playmode changes.
    [SerializeField]
    public static string CurrentTestName;

    public static GameObject ReportingMonitorInstance { get; set; }
    public static TestRunData ReportData { get; set; }
    public static bool IsAutomatorTest { get; set; }
    public static bool IsTestWithoutRecordingFile { get; set; }
    public static bool IsCompositeRecording { get; set; }
    public static string EntryScene { get; set; }
    private static bool Initialized { get; set; }
    private static bool Finalized { get; set; }
    private static string _reportSaveDirectory { get; set; }

    [Serializable]
    public class TestRunData
    {
        public TestRunData()
        {
            Tests = new List<TestData>();
            FpsData = new List<FpsData>();
            AllLogs = new List<Log>();
        }
        public List<Log> AllLogs;
        public List<FpsData> FpsData;
        public bool IsLocalRun;
        public string RunStartTime;
        public string RunFinishtTime;
        public string RunTime;
        public string Resolution;
        public string AspectRatio;
        public string Udid;
        public string DeviceType;
        public string DeviceModel;
        public List<TestData> Tests;
    }

    [Serializable]
    public class TestData
    {
        public TestData()
        {
            Status = TestStatus.NotRun.ToString();
            Steps = new List<StepData>();
        }
        public bool InProgress;
        public string TestName;
        public string RecordingName;
        public string TimestampUtc;
        public float StartTime;
        public float EndTime;
        public string Status;
        public List<StepData> Steps;
    }

    [Serializable]
    public class StepData
    {
        public StepData()
        {
            Status = TestStatus.NotRun.ToString();
            Logs = new List<Log>();
        }
        public enum ConsoleLogType { Error, Warning, Log }
        public string Name;
        public string ActionType;
        public string Hierarchy;
        public string Scene;
        public string Coordinates;
        public string ScreenshotBefore;
        public string ScreenshotAfter;
        public string Status;
        public List<Log> Logs;
    }

    [Serializable]
    public class FpsData
    {
        public string CurrentStepDataName;
        public string Fps;
        public string TimeStamp;
    }

    [Serializable]
    public struct Log
    {
        public string Message;
        public string StackTrace;
        public string Type;
        public int CountInARow;
    }

    public enum TestStatus
    {
        Pass,
        Fail,
        Warning,
        NotRun
    }

    public enum ReportType
    {
        Html,
        Xml
    }

    /// <summary>
    /// Record the current framerate.
    /// </summary>
    public static void SampleFramerate()
    {
        if (ReportData == null)
            return;
        FpsData fpsData = new FpsData();
        fpsData.CurrentStepDataName = ReportData.Tests.Any() && ReportData.Tests.Last().Steps.Any() ? $"{ReportData.Tests.Last().TestName} > [{ReportData.Tests.Last().Steps.Last().ActionType.ToUpper()} {ReportData.Tests.Last().Steps.Last().Hierarchy} > {ReportData.Tests.Last().Steps.Last().Name}]" : string.Empty;
        fpsData.Fps = Math.Round(1.0f / Time.deltaTime, 0).ToString();
        fpsData.TimeStamp = Math.Round(Time.time, 1).ToString();
        ReportData.FpsData.Add(fpsData);
    }

    /// <summary>
    /// Creates listener that checks for OnApplicationQuit and other hooks which represent the appropriate time to finalize and generate a report.
    /// </summary>
    public static void CreateMonitoringService()
    {
        if (ReportingMonitorInstance == null)
        {
            ReportingMonitorInstance = new GameObject("AutomatedQAReportingMonitor");
            ReportingMonitor rm = ReportingMonitorInstance.AddComponent<ReportingMonitor>();
            rm.RecordingMode = RecordingInputModule.Instance.RecordingMode;
            UnityEngine.Object.DontDestroyOnLoad(ReportingMonitorInstance);
        }
    }

    /// <summary>
    /// Get started capturing test data for test report.
    /// </summary>
    public static void InitializeReport()
    {
        if (Initialized) return;
        Initialized = true;
        ReportData = new TestRunData();
#if UNITY_EDITOR
        ReportData.IsLocalRun = true;
#else
        ReportData.IsLocalRun = false;
#endif
        ReportData.RunStartTime = DateTime.UtcNow.ToString();
        ReportData.DeviceType = SystemInfo.deviceType.ToString();
        ReportData.DeviceModel = SystemInfo.deviceModel;
        ReportData.Udid = SystemInfo.deviceUniqueIdentifier;

        Application.logMessageReceived -= RecordLog; // Detach if already attached.
        Application.logMessageReceived += RecordLog; // Attach handler to recieve incoming logs.

        if (Directory.Exists(ReportSaveDirectory))
            Directory.Delete(ReportSaveDirectory, true);
        Directory.CreateDirectory(ReportSaveDirectory);
        Directory.CreateDirectory(Path.Combine(ReportSaveDirectory, "screenshots"));
    }

    /// <summary>
    /// Set final run data and generate all reports.
    /// </summary>
    public static void FinalizeReport()
    {
        if (!Initialized || Finalized) return;
        if (ReportData.Tests.Last().InProgress)
        {
            FinalizeTestData();
        }
        Finalized = true;
        ReportData.RunFinishtTime = DateTime.UtcNow.ToString();
        ReportData.RunTime = Time.time.ToString();
        ReportData.AspectRatio = $"Width [{RecordedPlaybackPersistentData.RecordedAspectRatio.x}] by Height [{RecordedPlaybackPersistentData.RecordedAspectRatio.y}]";
        ReportData.Resolution = $"{RecordedPlaybackPersistentData.RecordedResolution.x} x {RecordedPlaybackPersistentData.RecordedResolution.y}";

        // If any tests ran without executing steps, something went wrong. Empty tests should not be considered a successful test.
        List<TestData> testsWithoutSteps = ReportData.Tests.FindAll(x => x.Status == TestStatus.Pass.ToString() && !x.Steps.Any());
        if (testsWithoutSteps.Any())
        {
            foreach (TestData test in testsWithoutSteps)
            {
                test.Status = TestStatus.Fail.ToString();
            }
        }

        if (!string.IsNullOrEmpty(RecordingInputModule.ScreenshotFolderPath) && Directory.Exists(RecordingInputModule.ScreenshotFolderPath))
        {
            // Copy screenshots over to report.
            foreach (var file in Directory.GetFiles(RecordingInputModule.ScreenshotFolderPath))
                File.Copy(file, Path.Combine(ReportSaveDirectory, "screenshots", new FileInfo(file).Name));
        }

        // Clean up temp screenshot directory.
        string[] folders = Directory.GetDirectories(AutomatedQARuntimeSettings.PersistentDataPath);
        foreach (string folder in folders)
        {
            string foldername = folder.Split(Path.DirectorySeparatorChar).Last();
            if (foldername.Contains("screenshots"))
            {
                Directory.Delete(folder, true);
            }
        }

        GenerateXmlReport();
        GenerateHtmlReport();
        UnityEngine.Object.DestroyImmediate(ReportingMonitorInstance);
    }

    /// <summary>
    /// A new recording or Unity test is being launched. Prepare to capture related data.
    /// </summary>
    public static void InitializeDataForNewTest()
    {
        if (!Initialized)
        {
            InitializeReport();
        }

        // Test was already initialized. Most likely due to errors or logs requiring test data initialization before it would normally be invoked.
        bool isCurrentTestAlreadyInitialized = ReportData.Tests.Any() && ReportData.Tests.FindAll(x => x.TestName == CurrentTestName).Any();
        if (isCurrentTestAlreadyInitialized || (IsTestWithoutRecordingFile && string.IsNullOrEmpty(CurrentTestName)))
            return;

        if (ReportData.Tests.Any() && ReportData.Tests.Last().InProgress)
        {
            FinalizeTestData();
        }
        TestData recording = new TestData();
        recording.TestName = CurrentTestName;

        // Check if the current test is expected to have a json recording file.
        if (!IsTestWithoutRecordingFile && AutomatedQARuntimeSettings.hostPlatform != HostPlatform.Cloud &&
            // CurrentTestName is null/empty if a recording was launched from AutomatedQa editor windows.
            (string.IsNullOrEmpty(CurrentTestName) ? true : RecordedTesting.IsRecordedTest(recording.TestName)))
        {
            recording.RecordingName = string.IsNullOrEmpty(RecordedPlaybackPersistentData.RecordingFileName) ?
                RecordedTesting.GetLocalRecordingFile(recording.TestName) :
                RecordedPlaybackPersistentData.RecordingFileName;
        }
        recording.RecordingName = string.IsNullOrEmpty(recording.RecordingName) ? "" : recording.RecordingName.Split('/').Last().Replace(".json", string.Empty);
        recording.StartTime = Time.time;
        recording.TimestampUtc = DateTime.UtcNow.ToString();
        recording.InProgress = true;
        StepData step = new StepData();
        step.Name = "Test Initialization";
        step.Scene = SceneManager.GetActiveScene().name;
        step.ActionType = "setup";
        recording.Steps.Add(step);
        ReportData.Tests.Add(recording);
    }

    /// <summary>
    /// Current recording or Unity test is complete. Finalize data in preperation for next test or end of run.
    /// </summary>
    public static void FinalizeTestData()
    {
        if (ReportData == null || !ReportData.Tests.Any()) return;
        TestData currentRecording = ReportData.Tests.Last();
        if (currentRecording.Status == TestStatus.NotRun.ToString())
            currentRecording.Status = TestStatus.Pass.ToString(); // A pass is determined by whether an exception prevented completion of a step.
        if (currentRecording.Steps.Any() && currentRecording.Steps.Last().Status == TestStatus.NotRun.ToString())
            currentRecording.Steps.Last().Status = TestStatus.Pass.ToString();
        if (!currentRecording.InProgress) return;
        currentRecording.EndTime = Time.time;
        currentRecording.InProgress = false;

        List<StepData> stepsToUpdate = currentRecording.Steps.FindAll(x => x.Scene.ToLower().Contains("emptyscene"));
        foreach (StepData step in stepsToUpdate)
        {
            step.Scene = SceneManager.GetActiveScene().name;
        }

        RecordedPlaybackAnalytics.SendRecordingExecution(
            RecordedPlaybackPersistentData.kRecordedPlaybackFilename,
            EntryScene,
            currentRecording.Status == TestStatus.Pass.ToString(),
            (int)(currentRecording.EndTime - currentRecording.StartTime)
        );
        IsAutomatorTest = false;
    }

    /// <summary>
    /// Creates a new step or action data point for the current test being executed.
    /// </summary>
    /// <param name="step"></param>
    public static void AddStep(StepData step)
    {
        if (!ReportData.Tests.Any())
            InitializeDataForNewTest();

        // If this is the continuation of a drag, only record the drag start and next drag release.
        if (ReportData.Tests.Last().Steps.Any()
            && ReportData.Tests.Last().Steps.Last().ActionType.ToLowerInvariant() == "drag"
            && step.ActionType.ToLowerInvariant() == "drag")
            return;

        if (ReportData.Tests.Last().Steps.Any() &&
            ReportData.Tests.Last().Steps.Last().Status == TestStatus.NotRun.ToString())
            ReportData.Tests.Last().Steps.Last().Status = TestStatus.Pass.ToString(); // A pass is determined by whether a logged error or exception prevented completion of a step.

        step.Scene = SceneManager.GetActiveScene().name;
        ReportData.Tests.Last().Steps.Add(step);
    }

    /// <summary>
    /// A step is generated before the coordinates of a click or drag are performed. This allows for the coordinates to be recorded later in the execution of an action, and after a step is generated.
    /// </summary>
    /// <param name="CoordinatesOfPress"></param>
    public static void UpdateCurrentStep(Vector2 CoordinatesOfPress = new Vector2())
    {
        StepData currentStep = ReportData.Tests.Last().Steps.Last();
        if (CoordinatesOfPress != default(Vector2))
            currentStep.Coordinates = $"x [{Math.Round(CoordinatesOfPress.x, 0)}] y [{Math.Round(CoordinatesOfPress.y, 0)}]";
    }

    /// <summary>
    /// A screenshot was recorded while executing a test, so associate its storage path the most recently-generated step.
    /// </summary>
    /// <param name="screenshotPath"></param>
    public static void AddScreenshot(string screenshotPath)
    {
        StepData step = ReportData.Tests.Last().Steps.Last();
        string relativePath = $"screenshots/{screenshotPath.Split(new string[] { "/", "\\" }, StringSplitOptions.None).Last()}";
        if (string.IsNullOrEmpty(step.ScreenshotBefore))
        {
            step.ScreenshotBefore = relativePath;
        }
        else
        {
            step.ScreenshotAfter = relativePath;
        }
    }

    /// <summary>
    /// Record logs from Unity console and add them to most recently-generated step.
    /// </summary>
    public static void RecordLog(string message, string stackTrace, LogType type)
    {
        message = EncodeCharactersForJson(message);
        bool anyTestsSet = ReportData.Tests.Any();
        bool anyStepsSet = anyTestsSet && ReportData.Tests.Last().Steps.Any();
        bool isCurrentTestNameSet = !string.IsNullOrEmpty(CurrentTestName);

        // An log is outside the context of the test run itself, and won't be tracked unless it is an exception that can affect the test run/
        if ((!anyTestsSet || !anyStepsSet) && (!isCurrentTestNameSet || type != LogType.Exception)) return;

        if (!anyTestsSet && !anyStepsSet && isCurrentTestNameSet)
        {
            InitializeDataForNewTest();
            if (!ReportData.Tests.Any())
                return;
        }

        // If the newest log is identical to the last log, increment the last log. Otherwise record as a new log.
        Log lastLogStep = ReportData.Tests.Last().Steps.Last().Logs.Any() ? ReportData.Tests.Last().Steps.Last().Logs.Last() : new Log();
        Log lastLogAll = ReportData.AllLogs.Any() ? ReportData.AllLogs.Last() : new Log();
        bool incrementingLastAllLog = false;
        bool incrementingLastStepLog = false;
        if (lastLogAll.Message == message && lastLogAll.Type == type.ToString())
        {
            incrementingLastAllLog = true;
            lastLogAll.CountInARow++;
            ReportData.AllLogs[ReportData.AllLogs.Count - 1] = lastLogAll;
        }
        if (lastLogStep.Message == message && lastLogStep.Type == type.ToString())
        {
            incrementingLastStepLog = true;
            lastLogStep.CountInARow++;
            ReportData.Tests.Last().Steps.Last().Logs[ReportData.Tests.Last().Steps.Last().Logs.Count - 1] = lastLogStep;
        }

        Log newLog = new Log()
        {
            Message = message,
            StackTrace = type == LogType.Exception ? stackTrace : string.Empty,
            Type = type.ToString(),
        };
        if (!incrementingLastAllLog)
            ReportData.AllLogs.Add(newLog);
        if (!incrementingLastStepLog)
            ReportData.Tests.Last().Steps.Last().Logs.Add(newLog);

        // Report test failure if an exception occurred, or an error was logged.
        if (type == LogType.Exception || type == LogType.Error)
        {
            TestData currentTest = ReportData.Tests.Last();
            StepData currentStep = currentTest.Steps.Last();
            currentTest.Status = currentStep.Status = TestStatus.Fail.ToString();
            if (RecordingInputModule.Instance != null)
                RecordingInputModule.Instance.CaptureScreenshots(); // Errors may prevent normal screen capture logic from being hit.
        }
        // Report warning status on associated step.
        else if (type == LogType.Warning)
        {
            TestData currentTest = ReportData.Tests.Last();
            StepData currentStep = currentTest.Steps.Last();
            currentStep.Status = TestStatus.Warning.ToString();
            if (currentTest.Status != TestStatus.Fail.ToString())
                currentTest.Status = TestStatus.Warning.ToString();
        }
    }

    public static void GenerateXmlReport()
    {
        GenerateXmlReport(ReportData.Tests, Path.Combine(ReportSaveDirectory, $"{ReportFileNameWithoutExtension}.xml"));
    }

    public static void GenerateXmlReport(List<TestData> tests, string outputPath)
    {
        StringBuilder xmlReport = new StringBuilder();
        xmlReport.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xmlReport.AppendLine("<testsuites>");
        int failCount = tests.FindAll(x => x.Status == TestStatus.Fail.ToString()).Count;
        xmlReport.AppendLine($"<testsuite failures=\"{failCount}\" tests=\"{tests.Count}\" errors=\"{failCount}\" name=\"Automation-Tests\" skipped=\"{tests.FindAll(x => x.Status == TestStatus.NotRun.ToString()).Count}\" time=\"{Time.time}\">");
        foreach (TestData test in tests)
        {
            // Extrapolate test results into xml nodes to append to the test run's xml report.
            string[] namePieces = string.IsNullOrEmpty(test.TestName) ? new string[] { } : test.TestName.Split('.');
            string className = string.IsNullOrEmpty(test.TestName) ? "NotACompiledTest" : (namePieces.Length > 1 ? namePieces[namePieces.Length - 2] : "CouldNotFindClassName");
            string testName = string.IsNullOrEmpty(test.TestName) ? test.RecordingName : namePieces[namePieces.Length - 1];
            if (test.TestName.Contains(":"))
            {
                string[] deviceInfo = test.TestName.Split(':');
                testName = deviceInfo.Length > 1? $"{deviceInfo[0]}:{deviceInfo[1]}:{testName}" : testName;
            }

            if (test.Status == TestStatus.Fail.ToString() || test.Status == TestStatus.Warning.ToString())
            {
                xmlReport.AppendLine($"<testcase classname=\"{className}\" name=\"{testName}\" time=\"{test.EndTime - test.StartTime}\">");
                xmlReport.AppendLine("<failure message=\"Failed. View HTML report for details.\" type=\"Test Failure\"></failure></testcase>");
            }
            else if (test.Status == TestStatus.Pass.ToString())
            {
                xmlReport.AppendLine($"<testcase classname=\"{className}\" name=\"{testName}\" time=\"{test.EndTime - test.StartTime}\"></testcase>");
            }
            else if (test.Status == TestStatus.NotRun.ToString())
            {
                xmlReport.AppendLine($"<testcase classname=\"{className}\" name=\"{testName}\" time=\"{test.EndTime - test.StartTime}\">");
                xmlReport.AppendLine("<skipped message=\"Skipped. View HTML report for details.\" type=\"Inconclusive\"></skipped></testcase>");
            }
        }
        xmlReport.AppendLine("</testsuite>");
        xmlReport.AppendLine("</testsuites>");
        File.WriteAllText(outputPath, xmlReport.ToString());
    }

    public static void GenerateHtmlReport()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine(TestRunReportHtmlManifest.TEST_RUN_REPORT_HTML_TEMPLATE);
        report.AppendLine($"<input id='test_results' type='hidden' value='{JsonUtility.ToJson(ReportData)}'/>");
        File.WriteAllText(Path.Combine(ReportSaveDirectory, $"report.html"), report.ToString());
    }

    public static bool DoesReportExist(ReportType reportType)
    {
        return File.Exists(Path.Combine(ReportSaveDirectory, $"{ReportFileNameWithoutExtension}{GetReportExtension(reportType)}"));
    }

    public static void OpenReportFile(ReportType reportType)
    {
        System.Diagnostics.Process.Start(Path.Combine(ReportSaveDirectory, $"{ReportFileNameWithoutExtension}{GetReportExtension(reportType)}"));
    }

    private static string GetReportExtension(ReportType reportType)
    {
        switch (reportType)
        {
            case ReportType.Html:
                return ".html";
            case ReportType.Xml:
                return ".xml";
        }
        return string.Empty;
    }

    private static List<(string character, string encoding)> EncodingKeys = new List<(string, string)>();
    /// <summary>
    /// Prepares string for json insertion.
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    public static string EncodeCharactersForJson(string val)
    {
        if (string.IsNullOrEmpty(val))
        {
            return string.Empty;
        }

        string result = System.Text.RegularExpressions.Regex.Replace(val, @"[^\u0000-\u007F]+", string.Empty); //Remove non ASCII characters from strings.
        result = new string(result.ToCharArray().ToList().FindAll(c => !char.IsControl(c)).ToArray()); //Remove control characters from strings.
        if (EncodingKeys.Count == 0)
        {
            EncodingKeys.Add(("<", "&#60;"));
            EncodingKeys.Add((">", "&#62;"));
            EncodingKeys.Add(("-", "&#45;"));
            EncodingKeys.Add((".", "&#46;"));
            EncodingKeys.Add(("~", "&#126;"));
            EncodingKeys.Add(("[", "&#91;"));
            EncodingKeys.Add(("]", "&#93;"));
            EncodingKeys.Add(("{", "&#123;"));
            EncodingKeys.Add(("}", "&#125;"));
            EncodingKeys.Add(("\\", "&#47;"));
            EncodingKeys.Add(("\"", "&#39;"));
            EncodingKeys.Add(("'", "&#39;"));
            EncodingKeys.Add((":", "&#58;"));
            EncodingKeys.Add(("(", "&#40;"));
            EncodingKeys.Add((")", "&#41;"));
            EncodingKeys.Add(("*", "&#42;"));
            EncodingKeys.Add(("/", "&#47;"));
            EncodingKeys.Add(("\n", " "));
            EncodingKeys.Add(("\t", " "));
            EncodingKeys.Add(("\r", " "));
            EncodingKeys.Add(("\b", " "));
        }

        for (int x = 0; x < EncodingKeys.Count; x++)
        {
            result = result.Replace(EncodingKeys[x].character, EncodingKeys[x].encoding);
        }
        return result;
    }
}