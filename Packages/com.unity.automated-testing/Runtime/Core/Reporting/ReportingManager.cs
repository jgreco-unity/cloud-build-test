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
    public static GameObject ReportingMonitor { get; set; }
    public static string CurrentTestName { get; set; }
    public static TestRunData ReportData { get; set; }
    private static bool Initialized { get; set; }
    private static bool Finalized { get; set; }
    private static List<Log> AllLogs { get; set; }
    private static string _reportSaveDirectory { get; set; }

    public class TestRunData
    {
        public bool IsLocalRun { get; set; }
        public DateTime RunStartTime { get; set; }
        public DateTime RunFinishtTime { get; set; }
        public string Resolution { get; set; }
        public string AspectRatio { get; set; }
        public string Udid { get; set; }
        public string DeviceType { get; set; }
        public string DeviceModel { get; set; }
        public List<TestData> Tests = new List<TestData>();
    }

    public class TestData
    {
        public bool InProgress { get; set; }
        public string TestName { get; set; }
        public string RecordingName { get; set; }
        public DateTime TimestampUtc { get; set; }
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public TestStatus Status = TestStatus.NotRun;
        public List<StepData> Steps = new List<StepData>();
    }

    public class StepData
    {
        public enum ConsoleLogType { Error, Warning, Log }
        public string Name { get; set; }
        public string ActionType { get; set; }
        public string Hierarchy { get; set; }
        public string Scene { get; set; }
        public string Coordinates { get; set; }
        public string ScreenshotBefore { get; set; }
        public string ScreenshotAfter { get; set; }
        public TestStatus Status = TestStatus.NotRun;
        public List<Log> Logs = new List<Log>();
    }

    public struct Log
    {
        public string Message;
        public string StackTrace;
        public LogType Type;
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
    /// Creates listener that checks for OnApplicationQuit and other hooks which represent the appropriate time to finalize and generate a report.
    /// </summary>
    public static void CreateMonitoringService()
    {
        if (ReportingMonitor == null)
        {
            ReportingMonitor = new GameObject("AutomatedQAReportingMonitor");
            ReportingMonitor rm = ReportingMonitor.AddComponent<ReportingMonitor>();
            rm.RecordingMode = RecordingInputModule.Instance.RecordingMode;
            UnityEngine.Object.DontDestroyOnLoad(ReportingMonitor);
        }
    }

    /// <summary>
    /// Get started capturing test data for test report.
    /// </summary>
    public static void InitializeReport()
    {
        if (Initialized) return;
        Initialized = true;
        AllLogs = new List<Log>();

        ReportData = new TestRunData();
#if UNITY_EDITOR
        ReportData.IsLocalRun = true;
#else
        ReportData.IsLocalRun = false;
#endif
        ReportData.RunStartTime = DateTime.UtcNow;
        ReportData.DeviceType = SystemInfo.deviceType.ToString();
        ReportData.DeviceModel = SystemInfo.deviceModel;
        ReportData.Udid = SystemInfo.deviceUniqueIdentifier;

        Application.logMessageReceived -= RecordLog; // Detach if already attached.
        Application.logMessageReceived += RecordLog; // Attach handler to recieve incoming logs.
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
        ReportData.RunFinishtTime = DateTime.UtcNow;
        ReportData.AspectRatio = $"Width [{RecordedPlaybackPersistentData.RecordedAspectRatio.x}] by Height [{RecordedPlaybackPersistentData.RecordedAspectRatio.y}]";
        ReportData.Resolution = $"{RecordedPlaybackPersistentData.RecordedResolution.x} x {RecordedPlaybackPersistentData.RecordedResolution.y}";

        // If any tests ran without executing steps, something went wrong. Empty tests should not be considered a successful test.
        List<TestData> testsWithoutSteps = ReportData.Tests.FindAll(x => x.Status == TestStatus.Pass && !x.Steps.Any());
        if (testsWithoutSteps.Any())
        {
            foreach (TestData test in testsWithoutSteps)
            {
                test.Status = TestStatus.Fail;
            }
        }

        if (Directory.Exists(ReportSaveDirectory))
        {
            Directory.Delete(ReportSaveDirectory, true);
        }
        Directory.CreateDirectory(ReportSaveDirectory);
        Directory.CreateDirectory(Path.Combine(ReportSaveDirectory, "screenshots"));

        GenerateXmlReport();
        GenerateHtmlReport();
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
        if (ReportData.Tests.Any() && ReportData.Tests.Last().InProgress)
        {
            FinalizeTestData();
        }
        TestData recording = new TestData();
        recording.TestName = CurrentTestName;
        if (AutomatedQARuntimeSettings.hostPlatform != HostPlatform.Cloud)
        {
            recording.RecordingName = string.IsNullOrEmpty(RecordedPlaybackPersistentData.RecordingFileName) ?
                RecordedTesting.GetLocalRecordingFile(recording.TestName) :
                RecordedPlaybackPersistentData.RecordingFileName;
        }
        recording.RecordingName = string.IsNullOrEmpty(recording.RecordingName) ? "" : recording.RecordingName.Split('/').Last().Replace(".json", string.Empty);
        recording.StartTime = Time.time;
        recording.TimestampUtc = DateTime.UtcNow;
        recording.InProgress = true;
        ReportData.Tests.Add(recording);
    }

    /// <summary>
    /// Current recording or Unity test is complete. Finalize data in preperation for next test or end of run.
    /// </summary>
    public static void FinalizeTestData()
    {
        if (ReportData == null || !ReportData.Tests.Any()) return;
        TestData currentRecording = ReportData.Tests.Last();
        if (currentRecording.Status == TestStatus.NotRun)
            currentRecording.Status = TestStatus.Pass; // A pass is determined by whether an exception prevented completion of a step.
        if (currentRecording.Steps.Any() && currentRecording.Steps.Last().Status == TestStatus.NotRun)
            currentRecording.Steps.Last().Status = TestStatus.Pass;
        if (!currentRecording.InProgress) return;
        currentRecording.EndTime = Time.time;
        currentRecording.InProgress = false;
    }

    /// <summary>
    /// Creates a new step or action data point for the current test being executed.
    /// </summary>
    /// <param name="step"></param>
    public static void AddStep(StepData step)
    {
        // If this is the continuation of a drag, only record the drag start and next drag release.
        if (ReportData.Tests.Last().Steps.Any()
            && ReportData.Tests.Last().Steps.Last().ActionType.ToLowerInvariant() == "drag"
            && step.ActionType.ToLowerInvariant() == "drag")
            return;

        if (ReportData.Tests.Last().Steps.Any() &&
            ReportData.Tests.Last().Steps.Last().Status == TestStatus.NotRun)
            ReportData.Tests.Last().Steps.Last().Status = TestStatus.Pass; // A pass is determined by whether a logged error or exception prevented completion of a step.

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
        if (string.IsNullOrEmpty(step.ScreenshotBefore))
        {
            step.ScreenshotBefore = screenshotPath;
        }
        else
        {
            step.ScreenshotAfter = screenshotPath;
        }
    }

    /// <summary>
    /// Record logs from Unity console and add them to most recently-generated step.
    /// </summary>
    public static void RecordLog(string message, string stackTrace, LogType type)
    {
        if (!ReportData.Tests.Any() || !ReportData.Tests.Last().Steps.Any()) return;

        // If the newest log is identical to the last log, increment the last log. Otherwise record as a new log.
        Log lastLogStep = ReportData.Tests.Last().Steps.Last().Logs.Any() ? ReportData.Tests.Last().Steps.Last().Logs.Last() : new Log();
        Log lastLogAll = AllLogs.Any() ? AllLogs.Last() : new Log();
        if (lastLogAll.Message == message && lastLogAll.Type == type && lastLogStep.Message == message && lastLogStep.Type == type)
        {
            lastLogAll.CountInARow++;
            lastLogStep.CountInARow++;
            return;
        }
        else
        {
            Log newLog = new Log()
            {
                Message = message,
                StackTrace = type == LogType.Exception ? stackTrace : string.Empty,
                Type = type,
            };
            ReportData.Tests.Last().Steps.Last().Logs.Add(newLog);
            AllLogs.Add(newLog);
        }
        // Report test failure if an exception occurred, or an error was logged.
        if (type == LogType.Exception || type == LogType.Error)
        {
            TestData currentTest = ReportData.Tests.Last();
            StepData currentStep = currentTest.Steps.Last();
            currentTest.Status = currentStep.Status = TestStatus.Fail;
            RecordingInputModule.Instance.CaptureScreenshots(); // Errors may prevent normal screen capture logic from being hit.
        }
        // Report warning status on associated step.
        else if (type == LogType.Warning)
        {
            TestData currentTest = ReportData.Tests.Last();
            StepData currentStep = currentTest.Steps.Last();
            currentStep.Status = TestStatus.Warning;
            if (currentTest.Status != TestStatus.Fail)
                currentTest.Status = TestStatus.Warning;
        }
    }

    public static void GenerateXmlReport()
    {
        StringBuilder xmlReport = new StringBuilder();
        xmlReport.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xmlReport.AppendLine("<testsuites>");
        int failCount = ReportData.Tests.FindAll(x => x.Status == TestStatus.Fail).Count;
        xmlReport.AppendLine($"<testsuite failures=\"{failCount}\" tests=\"{ReportData.Tests.Count}\" errors=\"{failCount}\" name=\"Automation-Tests\" skipped=\"{ReportData.Tests.FindAll(x => x.Status == TestStatus.NotRun).Count}\" time=\"{Math.Abs(ReportData.RunStartTime.Subtract(ReportData.RunFinishtTime).TotalSeconds)}\">");
        foreach (TestData test in ReportData.Tests)
        {
            // Extrapolate test results into xml nodes to append to the test run's xml report.
            string[] namePieces = string.IsNullOrEmpty(test.TestName) ? new string[] { } : test.TestName.Split('.');
            string className = string.IsNullOrEmpty(test.TestName) ? "NotACompiledTest" : (namePieces.Length > 1 ? namePieces[namePieces.Length - 2] : "CouldNotFindClassName");
            string testName = string.IsNullOrEmpty(test.TestName) ? test.RecordingName : namePieces[namePieces.Length - 1];
            if (test.Status == TestStatus.Fail || test.Status == TestStatus.Warning)
            {
                xmlReport.AppendLine($"<testcase classname=\"{className}\" name=\"{testName}\" time=\"{test.EndTime - test.StartTime}\">");
                xmlReport.AppendLine("<failure message=\"Failed. View HTML report for details.\" type=\"Test Failure\"></failure></testcase>");
            }
            else if (test.Status == TestStatus.Pass)
            {
                xmlReport.AppendLine($"<testcase classname=\"{className}\" name=\"{testName}\" time=\"{test.EndTime - test.StartTime}\"></testcase>");
            }
            else if (test.Status == TestStatus.NotRun)
            {
                xmlReport.AppendLine($"<testcase classname=\"{className}\" name=\"{testName}\" time=\"{test.EndTime - test.StartTime}\">");
                xmlReport.AppendLine("<skipped message=\"Skipped. View HTML report for details.\" type=\"Inconclusive\"></skipped></testcase>");
            }
        }
        xmlReport.AppendLine("</testsuite>");
        xmlReport.AppendLine("</testsuites>");
        File.WriteAllText(Path.Combine(ReportSaveDirectory, $"{ReportFileNameWithoutExtension}.xml"), xmlReport.ToString());
    }

    public static void GenerateHtmlReport()
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("<head>");
        report.AppendLine(TestRunReportHtmlManiest.REQUIRED_EXTERNAL_SCRIPTS);
        report.AppendLine(TestRunReportHtmlManiest.CHART_SCRIPT);
        report.AppendLine(TestRunReportHtmlManiest.REPORT_SCRIPTS);
        report.AppendLine(TestRunReportHtmlManiest.REPORT_STYLES);
        report.AppendLine("</head>");
        report.AppendLine("<body>");

        report.AppendLine(TestRunReportHtmlManiest.MODAL_POPUP);
        report.AppendLine(TestRunReportHtmlManiest.UNIT_LOGO_HEADER);
        report.AppendLine($@"
	        <div class='header-region'>
		        <h1  class='header-title'>{(ReportData.IsLocalRun ? "Local" : "Cloud/CI")} Test Run Report</h1>
	        </div>
        ");

        report.AppendLine("<div class='status-summary-region'>");
        report.AppendLine($@"
	        <div class='test-run-data-region'>
			    <div class='test-run-data'><div class='data-label'>Time Started UTC:</div><div class='data-value'>{ReportData.RunStartTime}</div></div>
			    <div class='test-run-data'><div class='data-label'>Total Run Time:</div><div class='data-value'>{Math.Round(Math.Abs(ReportData.RunStartTime.Subtract(ReportData.RunFinishtTime).TotalSeconds), 0)} (s)</div></div>
			    <div class='test-run-data'><div class='data-label'>Device Type:</div><div class='data-value'>{ReportData.DeviceType}</div></div>
			    <div class='test-run-data'><div class='data-label'>Device Model:</div><div class='data-value'>{ReportData.DeviceModel}</div></div>
			    <div class='test-run-data'><div class='data-label'>Device UDID:</div><div class='data-value'>{ReportData.Udid}</div></div>
			    <div class='test-run-data'><div class='data-label'>Aspect Ratio:</div><div class='data-value'>{ReportData.AspectRatio}</div></div>
			    <div class='test-run-data'><div class='data-label'>Resolution:</div><div class='data-value'>{ReportData.Resolution}</div></div>
		    </div>
        ");
        report.AppendLine(TestRunReportHtmlManiest.PIECHART_AND_TOOLTIP);
        report.AppendLine("<div class='piechart-messages'>");
        foreach (Log log in AllLogs)
        {
            string logClass = string.Empty;
            switch (log.Type)
            {
                case LogType.Error:
                case LogType.Exception:
                    logClass = "error";
                    break;
                case LogType.Warning:
                    logClass = "warning";
                    break;
                case LogType.Log:
                    logClass = "log";
                    break;
            }
            report.AppendLine($"<div class='console-log piechart-{logClass}{(log.StackTrace.Length > 0 ? " has-stacktrace" : string.Empty)}' onclick='ShowStackTrace(this);'><strong>&nbsp;<span class='char {GetLogTypeClass(log.Type)}'>{GetLogTypeIndicator(log.Type)}</span>&nbsp;{(log.CountInARow > 0 ? $"[{log.CountInARow + 1}]" : string.Empty)}</strong>&nbsp;{log.Message}<input type='hidden' class='console-log-stacktrace' value='{log.StackTrace}'/></div>");
        }
        report.AppendLine($"<input class='pie-chart-data' type='hidden' value ='Status|Count," +
            $"Pass|{ReportData.Tests.FindAll(x => x.Status == TestStatus.Pass).Count}," +
            $"Warning|{ReportData.Tests.FindAll(x => x.Status == TestStatus.Warning).Count}," +
            $"Fail|{ReportData.Tests.FindAll(x => x.Status == TestStatus.Fail).Count}," +
            $"Not Run|{ReportData.Tests.FindAll(x => x.Status == TestStatus.NotRun).Count}'/>");
        report.AppendLine("</div>"); // End .piechart-messages
        report.AppendLine("</div>"); // End .status-summary-region

        report.AppendLine("<div class='recordings-container'>");
        foreach (TestData test in ReportData.Tests)
        {
            char statusIndicator = GetStatusIndicator(test.Status);
            // The toggle element that represents a single test/recording. Clicking it expands to show individual steps.
            report.AppendLine($@"
                <div class='recording-toggle' onclick='ToggleDetails(this);'>
                    <div class='status-indicator {GetStatusClass(test.Status)}'>
                        <div>{statusIndicator}</div>
                    </div>
                    <div class='recording-name'>
                        {(string.IsNullOrEmpty(test.TestName) ? string.Empty : $"{test.TestName} ")
                        } {
                        (string.IsNullOrEmpty(test.RecordingName) ? string.Empty : $"({test.RecordingName})")}
                    </div>
                </div>
            ");

            report.AppendLine("<div class='recording-details-region'>");
            if (!test.Steps.Any())
            {
                report.AppendLine("<h3 style='color: red;'><em>This test did not have any steps! Automatically failing as there should not be an \"empty\" test.</em></h3>");
            }
            int index = 0;
            foreach (StepData step in test.Steps)
            {
                string stepStatus = GetStatusClass(step.Status);
                // The toggle element under a test/recording expander that shows each step taken in a test/recording.
                report.AppendLine($@"
                    <div class='step-toggle {stepStatus}' onclick='ToggleDetails(this);'>
                        <div class='status-indicator step-square {stepStatus}'>
                            <div>{GetStatusIndicator(step.Status)}</div>
                        </div>
                        <div class='recording-name'>
                            {step.ActionType} <span class='game-object-heirarchy'>[{step.Scene}: {step.Name}]</span>
                        </div>
                    </div>
                ");
                report.AppendLine("<div class='recording-details'>");
                report.AppendLine("<h4>Screenshot Before</h4>");
                if (!string.IsNullOrEmpty(step.ScreenshotBefore) && File.Exists(step.ScreenshotBefore))
                {
                    string screenshotName = Path.Combine(ReportSaveDirectory, "screenshots", $"screenshot_before_{(!string.IsNullOrEmpty(test.TestName) ? test.TestName : test.RecordingName)}_{index}");
                    File.Copy(step.ScreenshotBefore, screenshotName, true);
                    report.AppendLine($"<img class='screenshot' src='{screenshotName}'/>");
                }
                else
                {
                    report.AppendLine($"<div><em>N/A</em></div>");
                }
                report.AppendLine("<h4>Screenshot After</h4>");
                if (!string.IsNullOrEmpty(step.ScreenshotAfter) && File.Exists(step.ScreenshotAfter))
                {
                    string screenshotName = Path.Combine(ReportSaveDirectory, "screenshots", $"screenshot_after_{(!string.IsNullOrEmpty(test.TestName) ? test.TestName : test.RecordingName)}_{index}");
                    File.Copy(step.ScreenshotAfter, screenshotName, true);
                    report.AppendLine($"<img class='screenshot' src='{screenshotName}'/>");
                }
                else
                {
                    report.AppendLine($"<div><em>N/A</em></div>");
                }
                // The steps' details
                report.AppendLine($@"
                    <div class='recording-details-data'>
                        <div><strong>Scene:</strong>&nbsp;{step.Scene}</div> 
					    <div><strong>Hierarchy:</strong>&nbsp;{step.Hierarchy}</div> 
					    <div><strong>Coordinates:</strong>&nbsp;{step.Coordinates}</div>
                    </div>
                 ");

                // Each log that was posted to the console during this test.
                report.AppendLine("<div class='recording-details-logs'>");
                if (!step.Logs.Any())
                {
                    report.AppendLine("<div><strong><em>No logs recorded during this step.</em></strong></div>");
                }
                foreach (Log log in step.Logs)
                {
                    report.AppendLine($@"
                        <div class='console-log {(log.StackTrace.Length > 0 ? " has-stacktrace" : string.Empty)}' onclick='ShowStackTrace(this);'>
                            <strong>
                                <span class='char {GetLogTypeClass(log.Type)}'>
                                    {GetLogTypeIndicator(log.Type)}
                                </span>
                                {(log.CountInARow > 0 ? $"&nbsp;[{log.CountInARow + 1}]" : string.Empty)}
                            </strong>
                            &nbsp;{log.Message}
                            <input type='hidden' class='console-log-stacktrace' value='{log.StackTrace}'/>
                        </div>");
                }
                report.AppendLine("</div>"); // End .recording-details-errors
                report.AppendLine("</div>"); // End .recording-details
                index++;
            }
            report.AppendLine("</div>"); // End .recording-details
        }

        report.AppendLine("</div>"); // End .recordings-container
        report.AppendLine("</body>");
        File.WriteAllText(Path.Combine(ReportSaveDirectory, $"{ReportFileNameWithoutExtension}.html"), report.ToString());
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

    private static char GetStatusIndicator(TestStatus status)
    {
        switch (status)
        {
            case TestStatus.Fail:
                return TestRunReportHtmlManiest.ERROR_CHAR;
            case TestStatus.NotRun:
                return TestRunReportHtmlManiest.NOT_RUN_CHAR;
            case TestStatus.Pass:
                return TestRunReportHtmlManiest.CHECK_MARK_CHAR;
            case TestStatus.Warning:
                return TestRunReportHtmlManiest.WARNING_CHAR;
            default:
                return TestRunReportHtmlManiest.INFO_CHAR.ToCharArray().First();
        }
    }

    private static string GetStatusClass(TestStatus status)
    {
        switch (status)
        {
            case TestStatus.Fail:
                return "fail";
            case TestStatus.Pass:
                return "pass";
            case TestStatus.Warning:
                return "warning";
            case TestStatus.NotRun:
            default:
                return "notrun";
        }
    }

    private static string GetLogTypeIndicator(LogType logType)
    {
        switch (logType)
        {
            case LogType.Log:
                return TestRunReportHtmlManiest.INFO_CHAR;
            case LogType.Error:
            case LogType.Exception:
                return TestRunReportHtmlManiest.ERROR_CHAR.ToString();
            case LogType.Warning:
                return TestRunReportHtmlManiest.WARNING_CHAR.ToString();
            default:
                return TestRunReportHtmlManiest.NOT_RUN_CHAR.ToString();
        }
    }

    private static string GetLogTypeClass(LogType logType)
    {
        switch (logType)
        {

            case LogType.Error:
            case LogType.Exception:
                return "error";
            case LogType.Warning:
                return "warn";
            case LogType.Log:
            default:
                return "log";
        }
    }
}
