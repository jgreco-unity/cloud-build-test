#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using static UnityEngine.EventSystems.RecordingInputModule;

namespace Unity.AutomatedQA
{
    public static class CodeGenerator
    {
        private static string GeneratedTestsFolderName => "GeneratedTests";
        private static string GeneratedTestsAssemblyName => "GeneratedTests.asmdef";
        private static string ScriptTemplatePath = "Packages/com.unity.automated-testing/Editor/ScriptTemplates/";
        private static string GeneratedTestAssemblyTemplatePath => $"{ScriptTemplatePath}Assembly Definition-GeneratedTests.asmdef.txt";
        private static string GeneratedTestScriptTemplatePath => $"{ScriptTemplatePath}C# Script-GeneratedRecordedTests.cs.txt";
        private static (string parent, List<string> segments) NestedSegmentHeirarchy;
        public static readonly string NEW_LINE = "\r\n";

        public static List<(string recordingFileName, string cSharpScriptFileName, bool isStepFile)> GenerateTest(string recordingFileName, bool ignoreDiffs, bool isSimpleTest, string stepFileToOverwrite = "")
        {
            // Create required files for UnityTest script creation.
            CreateTestAssemblyFolder();
            CreateTestAssembly();

            bool isStepFileOverwrite = !string.IsNullOrEmpty(stepFileToOverwrite);
            if (!recordingFileName.EndsWith(".json"))
                recordingFileName += ".json";
            string recordingFilePath = Path.Combine(AutomatedQARuntimeSettings.RecordingDataPath, isStepFileOverwrite ? stepFileToOverwrite : recordingFileName);
            string className = $"{GetClassNameForRecording(recordingFilePath)}_Tests";

            // If this is a "simple" generated test, simply check if edits have been made to the file and return or overwrite accordingly.
            string cSharpFileName = $"{className}.cs";
            // For full tests, continue.
            List<(string recordingFileName, string cSharpScriptFileName, bool isStepFile)> filesWithEdits = new List<(string recordingFileName, string cSharpScriptFileName, bool isStepFile)>();
            string stepFilesDirectory = Path.Combine(Application.dataPath, AutomatedQARuntimeSettings.PackageAssetsFolderName, "GeneratedTests", "Steps");
            if (!Directory.Exists(stepFilesDirectory))
            {
                Directory.CreateDirectory(stepFilesDirectory);
            }

            // Get recording json.
            var json = File.Exists(recordingFilePath) ? File.ReadAllText(recordingFilePath) : File.ReadAllText(recordingFilePath.Replace("_", "-"));
            string relativeRecordingFilePath = recordingFilePath.Split(new string[] { "Assets" }, StringSplitOptions.None).Last();
            List<(string file, TouchData data)> steps = new List<(string file, TouchData data)>();
            InputModuleRecordingData testData = JsonUtility.FromJson<InputModuleRecordingData>(json);
            TouchData playbackComplete = testData.touchData.Last();

            // Add main recording file's TouchData actions (if any).
            foreach (TouchData data in testData.touchData)
            {
                if (!testData.recordings.Any() || data.emitSignal != "playbackComplete")
                    steps.Add((recordingFilePath, data));
            }

            // If this recording has child segments, add their TouchData actions to the list of steps.
            if (!isStepFileOverwrite && testData.recordings.Any())
            {
                NestedSegmentHeirarchy = (recordingFilePath, new List<string>());
                steps.AddRange(AddTouchDataFromSegments(testData.recordings, recordingFilePath));
                steps.Add((recordingFilePath, playbackComplete));
            }

            //Formatting indentations for appended code. Each is a multiple of 4 spaces (1 tab).
            string indentOne = GetIndentationString(1);
            string indentTwo = GetIndentationString(2);
            string indentThree = GetIndentationString(3);
            string indentFour = GetIndentationString(4);

            string stepFileClassName, stepFileCSharpName, lastTouchDataFile;
            stepFileClassName = stepFileCSharpName = lastTouchDataFile = string.Empty;
            List<string> touchIds = new List<string>();
            List<string> handledFiles = new List<string>();
            StringBuilder setUpLogic = new StringBuilder();
            StringBuilder stepFile = new StringBuilder();
            StringBuilder touchDataList = new StringBuilder();
            StringBuilder simpleScript = new StringBuilder(GetRecordedTestScript(recordingFilePath));

            int index = 0;
            foreach ((string file, TouchData data) touchData in steps)
            {
                // If this is not the primary recording, but a step indicates the end of a recording or test, ignore it.
                if (touchData.data.emitSignal == "playbackComplete" || touchData.data.emitSignal == "segmentComplete")
                    continue;

                stepFileClassName = GetClassNameForRecording(Path.Combine(AutomatedQARuntimeSettings.RecordingDataPath, touchData.file)) + "_Steps";

                // Create a step file for each recording file.
                if (!isSimpleTest && !handledFiles.Contains(stepFileClassName))
                {
                    // Finish previous file.
                    if (handledFiles.Any())
                    {
                        stepFile.AppendLine($"{indentTwo}}}");
                        stepFile.AppendLine($"{indentOne}}}");
                        stepFile.AppendLine($"}}");
                        if (!isSimpleTest)
                            CreateStepFile(lastTouchDataFile, stepFileCSharpName, stepFile, ignoreDiffs, stepFileToOverwrite, ref filesWithEdits);
                    }

                    // Format recording name to remove characters that are invalid for use in a C# class name.
                    stepFileCSharpName = $"{stepFileClassName}.cs";
                    lastTouchDataFile = new FileInfo(touchData.file).Name;
                    handledFiles.Add(stepFileClassName);
                    stepFile = new StringBuilder();
                    touchIds = new List<string>();

                    // Start new step file. Steps will be references in the test file.
                    stepFile.AppendLine("using System.Collections.Generic;");
                    stepFile.AppendLine("using UnityEngine;");
                    stepFile.AppendLine($"using static UnityEngine.EventSystems.RecordingInputModule;{NEW_LINE}");
                    stepFile.AppendLine("namespace GeneratedRecordedTests");
                    stepFile.AppendLine("{"); // Start namespace bracket.
                    stepFile.AppendLine($"{indentOne}/// <summary>{NEW_LINE}" +
                                        $"{indentOne}/// This segment touch data were generated by Unity Automated QA for the recording from the Assets folder at \"{Path.Combine(AutomatedQARuntimeSettings.RecordingFolderName, lastTouchDataFile)}\"{NEW_LINE}" +
                                        $"{indentOne}/// You can regenerate this file from the Unity Editor Menu: Automated QA > Generate Recorded Tests{NEW_LINE}" +
                                        $"{indentOne}/// </summary>");
                    stepFile.AppendLine($"{indentOne}public static class {stepFileClassName}");
                    stepFile.AppendLine($"{indentOne}{{");
                    stepFile.AppendLine($"{indentTwo}public static Dictionary<string, TouchData> Actions = new Dictionary<string, TouchData>();");
                    stepFile.AppendLine($"{indentTwo}static {stepFileClassName}()");
                    stepFile.AppendLine($"{indentTwo}{{");
                }

                // Handle generation of a C# TouchData object, which is a 1-to-1 correlation with TouchData stored in the json recording file.
                string waitSignal = string.IsNullOrEmpty(touchData.data.waitSignal) ? string.Empty : touchData.data.waitSignal;
                string emitSignal = string.IsNullOrEmpty(touchData.data.emitSignal) ? string.Empty : touchData.data.emitSignal;
                string objectName = string.IsNullOrEmpty(touchData.data.objectName) ? string.Empty : touchData.data.objectName;
                string objectTag = string.IsNullOrEmpty(touchData.data.objectTag) ? string.Empty : touchData.data.objectTag;
                string objectHierarchy = string.IsNullOrEmpty(touchData.data.objectHierarchy) ? string.Empty : touchData.data.objectHierarchy;
                string idName = (string.IsNullOrEmpty(touchData.data.objectName) ? ( // Use the object name as the first choice in generating an id for the touch data.
                                    string.IsNullOrEmpty(touchData.data.objectTag) ? ( // Use the object tag as the second choice in generating an id for the touch data.
                                        string.IsNullOrEmpty(waitSignal) ? ( // Use the wait signal as the third choice in generating an id for the touch data.
                                            string.IsNullOrEmpty(emitSignal) ? // Use the emit signal as the fourth choice in generating an id for the touch data.
                                                "Signal" : // If all other fields are empty (which they shouldn't be), then use this generic word in generating an id for the touch data.
                                                emitSignal
                                        ) :
                                        waitSignal
                                    ) :
                                    touchData.data.objectTag
                                ) :
                                touchData.data.objectName).Replace(" ", "_");
                string touchIdBase = $"{(touchData.data.eventType == TouchData.type.none ? "EMIT" : touchData.data.eventType.ToString().ToUpper())}_{idName}";
                string touchIdFinal = touchIdBase;
                int idIndex = 2;
                bool uniqueId = false;
                while (!uniqueId)
                {
                    if (touchIds.Contains(touchIdFinal))
                    {
                        touchIdFinal = $"{touchIdBase}_{idIndex}";
                        idIndex++;
                    }
                    else
                    {
                        touchIds.Add(touchIdFinal);
                        uniqueId = true;
                    }
                }

                stepFile.AppendLine($"{indentThree}Actions.Add(\"{touchIdFinal}\", new TouchData{NEW_LINE}" +
                                         $"{indentThree}{{{NEW_LINE}" +
                                         $"{indentFour}pointerId = {touchData.data.pointerId},{NEW_LINE}" +
                                         $"{indentFour}eventType = TouchData.type.{touchData.data.eventType},{NEW_LINE}" +
                                         $"{indentFour}timeDelta = {touchData.data.timeDelta}f,{NEW_LINE}" +
                                         $"{indentFour}position =  new Vector3({touchData.data.position.x}f, {touchData.data.position.y}f),{NEW_LINE}" +
                                         $"{indentFour}positional = {touchData.data.positional.ToString().ToLower()},{NEW_LINE}" +
                                         $"{indentFour}scene = \"{touchData.data.scene}\",{NEW_LINE}" +
                                         $"{indentFour}waitSignal = \"{waitSignal}\",{NEW_LINE}" +
                                         $"{indentFour}emitSignal = \"{emitSignal}\",{NEW_LINE}" +
                                         $"{indentFour}keyCode = \"{touchData.data.keyCode}\",{NEW_LINE}" +
                                         $"{indentFour}inputDuration = {touchData.data.inputDuration}f,{NEW_LINE}" +
                                         $"{indentFour}inputText = \"{touchData.data.inputText}\",{NEW_LINE}" +
                                         $"{indentFour}objectName = \"{objectName}\",{NEW_LINE}" +
                                         $"{indentFour}objectTag = \"{objectTag}\",{NEW_LINE}" +
                                         $"{indentFour}objectHierarchy = \"{objectHierarchy}\",{NEW_LINE}" +
                                         $"{indentFour}objectOffset =  new Vector3({touchData.data.objectOffset.x}f, {touchData.data.objectOffset.y}f){NEW_LINE}" +
                                         $"{indentThree}}});");

                // For each step, add an extra line before the next TouchData object in our generated code. 
                if (index != testData.touchData.Count - 1)
                    stepFile.AppendLine(string.Empty);

                /*
                 * Code will register each step file step used by a test in that test's SetUpClass method. 
                 * This is because recording logic expects all TouchData to be set from the start of playback, as opposed to adding TouchData with each action we invoke.
                */
                setUpLogic.AppendLine($"{indentThree}RegisterStep({stepFileClassName}.Actions[\"{touchIdFinal}\"]);");
                string testStep = $"{indentThree}yield return PerformAction({stepFileClassName}.Actions[\"{touchIdFinal}\"]); " + (touchData.data.eventType == TouchData.type.none ? $"// Emit {touchData.data.emitSignal}" :
                           $"// Do a \"{touchData.data.eventType}\" action " +
                           $"{(string.IsNullOrEmpty(touchData.data.objectName) ? $"at \"{Math.Round(touchData.data.position.x, 2)}x {Math.Round(touchData.data.position.y, 2)}y\" coordinates " : $"on \"{(string.IsNullOrEmpty(touchData.data.objectHierarchy) ? touchData.data.objectName : $"{touchData.data.objectHierarchy}/{touchData.data.objectName}")}\"")} " +
                           $"in scene \"{touchData.data.scene}\".");
                touchDataList.AppendLine(testStep);
                index++;
            }

            // Add final test step for emitting "playbackComplete".
            touchDataList.AppendLine($"{indentThree}yield return PerformAction(EMIT_COMPLETE); // Test complete.");

            // Finish final step file.
            stepFile.AppendLine($"{indentTwo}}}");
            stepFile.AppendLine($"{indentOne}}}");
            stepFile.AppendLine($"}}");
            if (!isSimpleTest)
                CreateStepFile(lastTouchDataFile, stepFileCSharpName, stepFile, ignoreDiffs, stepFileToOverwrite, ref filesWithEdits);

            // Generate test file with step-by-step action invocations generated previously.
            StringBuilder fullScript = new StringBuilder();
            fullScript.AppendLine("using System.Collections;");
            fullScript.AppendLine("using UnityEngine;");
            fullScript.AppendLine("using UnityEngine.TestTools;");
            fullScript.AppendLine($"{NEW_LINE}namespace GeneratedRecordedTests");
            fullScript.AppendLine("{"); // Start namespace bracket.
            fullScript.AppendLine($"{indentOne}/// <summary>{NEW_LINE}" +
                                  $"{indentOne}/// These tests were generated by Unity Automated QA for the recording from the Assets folder at \"{relativeRecordingFilePath}\".{NEW_LINE}" +
                                  $"{indentOne}/// You can regenerate this file from the Unity Editor Menu: Automated QA > Generate Recorded Tests{NEW_LINE}" +
                                  $"{indentOne}/// </summary>");
            fullScript.AppendLine($"{indentOne}public class {className} : AutomatedQATestsBase");
            fullScript.AppendLine($"{indentOne}{{"); // Start class bracket.
            fullScript.AppendLine($"{indentTwo}/// Generated from recording file: \"{relativeRecordingFilePath}\".{NEW_LINE}" +
                                  $"{indentTwo}[UnityTest]{NEW_LINE}" +
                                  $"{indentTwo}public IEnumerator CanPlayToEnd(){NEW_LINE}" +
                                  $"{indentTwo}{{{NEW_LINE}" +
                                  $"{touchDataList}" +
                                  $"{indentTwo}}}{NEW_LINE}");
            fullScript.AppendLine($"{indentTwo}// Steps defined by recording.{NEW_LINE}" +
                                  $"{indentTwo}protected override void SetUpTestClass(){NEW_LINE}" +
                                  $"{indentTwo}{{{NEW_LINE}" +
                                  $"{setUpLogic}" +
                                  $"{indentTwo}}}{NEW_LINE}");
            fullScript.AppendLine($"{indentTwo}// Initialize test run data.{NEW_LINE}" +
                                  $"{indentTwo}protected override void SetUpTestRun(){NEW_LINE}" +
                                  $"{indentTwo}{{{NEW_LINE}" +
                                  $"{indentThree}Test.entryScene = \"{testData.entryScene}\";{NEW_LINE}" +
                                  $"{indentThree}Test.recordedAspectRatio = new Vector2({testData.recordedAspectRatio.x}f,{testData.recordedAspectRatio.y}f);{NEW_LINE}" +
                                  $"{indentThree}Test.recordedResolution = new Vector2({testData.recordedResolution.x}f,{testData.recordedResolution.y}f);{NEW_LINE}" +
                                  $"{indentTwo}}}");
            fullScript.AppendLine($"{indentOne}}}"); // End class bracket.
            fullScript.AppendLine($"}}"); // End namespace bracket.

            string fileName = Path.Combine(Application.dataPath, AutomatedQARuntimeSettings.PackageAssetsFolderName, "GeneratedTests", cSharpFileName);
            // Compare new generated content to old generated content. If there is a difference, the user has made edits and we want to confirm that the user wishes to overwrite them.
            if (!isStepFileOverwrite && !ignoreDiffs && File.Exists(fileName))
            {
                if (HasGeneratedTestContentBeenEdited(fileName, fullScript.ToString(), simpleScript.ToString()))
                {
                    filesWithEdits.Add((recordingFileName, cSharpFileName, false));
                }
            }

            bool generateFullTestScript =
                !isStepFileOverwrite // Ignore the test file if we are only overwriting step files.
                && !isSimpleTest // Don't generate a full test script if it wasn't requested.
                && (ignoreDiffs || !File.Exists(fileName)) // Don't generate a file if we are ignoring file creation while checking for edited content, unless the file doesn't exist at all.
                || (!isSimpleTest && isSimpleFileMatch(fileName, simpleScript.ToString())); // If it is an unedited match to the simple script, but a full script was requested, overwrite it now.

            bool generateSimpleTestScript =
                !isStepFileOverwrite // Ignore the test file if we are only overwriting step files.
                && isSimpleTest // Don't generate a simple test script if it wasn't requested.
                && (ignoreDiffs || !File.Exists(fileName)) // Don't generate a file if we are ignoring file creation while checking for edited content, unless the file doesn't exist at all.
                || (isSimpleTest && IsFullFileMatch(fileName, fullScript.ToString())); // If it is an unedited match to the full script, but a simple script was requested, overwrite it now.

            if (generateFullTestScript)
            {
                File.WriteAllText(fileName, fullScript.ToString());
            }
            else if (generateSimpleTestScript)
            {
                GenerateSimpleTest(recordingFilePath);
            }
            return filesWithEdits;
        }

        /// <summary>
        /// Recursive tool for adding steps from segements and nested sub-segments referenced by all associated json files in a recording.
        /// Record files used previously in the hierarchy of segment relationships, and throw an error if there is a circular reference.
        /// </summary>
        /// <param name="recordings"></param>
        /// <returns></returns>
        private static List<(string file, TouchData data)> AddTouchDataFromSegments(List<Recording> recordings, string parentFile)
        {
            // This is how we will determine that a hierarchical reference of segments from the top level down to the last child do not invoke a circular reference to a segment higher up in the tree of segments.
            if (NestedSegmentHeirarchy.segments.Any() && parentFile != NestedSegmentHeirarchy.segments.Last())
            {
                NestedSegmentHeirarchy = (parentFile, new List<string>()); // This is a top level recording, with no parent segments. Start mapping out new hierarchy of segments that it references.
            }

            List<(string file, TouchData data)> steps = new List<(string file, TouchData data)>();
            foreach (Recording recording in recordings)
            {
                // This segment was already reference higher up in the stack, thus creating a circular relationship. Any resulting composite recoridng would be invalid and effectively infinite.
                if (NestedSegmentHeirarchy.segments.Contains(recording.filename))
                {
                    throw new UnityException("Circular reference encountered. The chosen recording has references to segments that reference a parent segment. " +
                        "This creates a circular reference, which is an invalid use of modular segments and recordings. The path from top recording to circular reference is: " +
                        $"{parentFile} > {string.Join(" > ", NestedSegmentHeirarchy.segments)}");
                }
                NestedSegmentHeirarchy.segments.Add(recording.filename);

                // Get this segment's data, and see if it too references child recordings.
                var jsonSegment = File.ReadAllText(Path.Combine(Application.dataPath, AutomatedQARuntimeSettings.RecordingFolderName, recording.filename));
                InputModuleRecordingData segmentData = JsonUtility.FromJson<InputModuleRecordingData>(jsonSegment);
                if (segmentData.recordings.Any())
                {
                    steps.AddRange(AddTouchDataFromSegments(segmentData.recordings, recording.filename));
                }

                // Ignore segment complete emits. Generated code does not behave like a composite recording.
                foreach (TouchData data in segmentData.touchData)
                {
                    if (data.emitSignal != "segmentComplete")
                        steps.Add((recording.filename, data));
                }
            }
            return steps;
        }

        /// <summary>
        /// Add spaces to a string based on requested indentiation tab count.
        /// </summary>
        /// <param name="tabCount"></param>
        /// <returns></returns>
        public static string GetIndentationString(int tabCount)
        {
            StringBuilder tabs = new StringBuilder();
            for (int x = 0; x < tabCount; x++)
            {
                tabs.Append("    ");
            }
            return tabs.ToString();
        }

        /// <summary>
        /// Strip all spacing, new line, and carriage return characters which may change after file generation, and should not be considered a customization of our generated content.
        /// </summary>
        private static bool HasGeneratedTestContentBeenEdited(string filePath, string fullScript, string simpleScript)
        {
            // If neither a full generated test, nor a simple generated test are identical to the newly-generated file, then the file content has been edited.
            return !IsFullFileMatch(filePath, fullScript) && !isSimpleFileMatch(filePath, simpleScript);
        }

        /// <summary>
        /// Does a new file built as a full test script match the old file?
        /// </summary>
        private static bool IsFullFileMatch(string filePath, string fullScript)
        {
            if (!File.Exists(filePath)) return false;
            string existingFileContent = File.ReadAllText(filePath).Replace(" ", string.Empty).Replace(NEW_LINE, string.Empty);
            string newFullFileContent = fullScript.ToString().Replace(" ", string.Empty).Replace(NEW_LINE, string.Empty);
            return existingFileContent == newFullFileContent;
        }

        /// <summary>
        /// Does a new file built as a simple test script match the old file?
        /// </summary>
        private static bool isSimpleFileMatch(string filePath, string simpleScript)
        {
            if (!File.Exists(filePath)) return false;
            string existingFileContent = File.ReadAllText(filePath).Replace(" ", string.Empty).Replace(NEW_LINE, string.Empty);
            string newSimpleFileContent = simpleScript.ToString().Replace(" ", string.Empty).Replace(NEW_LINE, string.Empty);
            return existingFileContent == newSimpleFileContent;
        }

        /// <summary>
        /// Generate new file containing all of the TouchData from a recording json file.
        /// </summary>
        /// <param name="stepFileName">Name of file without path or extension.</param>
        /// <param name="stepFile">File content to save to file.</param>
        /// <param name="ignoreDiffs">Do not compare new file content to old content.</param>
        /// <param name="stepFilesToOverwrite">List of step files that the user has chosen to overwrite.</param>
        /// <param name="filesWithEdits">Reference to list of C# files where customized edits where detected.</param>
        private static void CreateStepFile(string stepFileRecording, string stepFileCSharpName, StringBuilder stepFile, bool ignoreDiffs, string stepFileToOverwrite, ref List<(string recordingFileName, string cSharpScriptFileName, bool isStepFile)> filesWithEdits)
        {
            string stepFilePath = Path.Combine(AutomatedQARuntimeSettings.PackageAssetsFolderPath, AutomatedQARuntimeSettings.GeneratedTestsFolderName, "Steps", stepFileCSharpName);
            // Check if this file has edits and report if so.
            if (!ignoreDiffs && File.Exists(stepFilePath) && HasGeneratedTestContentBeenEdited(stepFilePath, stepFile.ToString(), string.Empty))
            {
                filesWithEdits.Add((stepFileRecording, stepFileCSharpName, true));
            }
            // If this is a call for file creation & overwrites, check that this file was requested to be overwritten and then generate it.
            else if (!File.Exists(stepFilePath) || (ignoreDiffs && stepFileToOverwrite == stepFileRecording))
            {
                File.WriteAllText(stepFilePath, stepFile.ToString());
            }
        }

        /// <summary>
        /// Create the folder where tests will be stored.
        /// </summary>
        public static void CreateTestAssemblyFolder()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, AutomatedQARuntimeSettings.PackageAssetsFolderName, GeneratedTestsFolderName));
        }

        /// <summary>
        /// Create the assembly that allows UnityTests to be compiled.
        /// </summary>
        public static void CreateTestAssembly()
        {
            var template = File.ReadAllText(GeneratedTestAssemblyTemplatePath);
            var content = template.Replace("#SCRIPTNAME#", Path.GetFileNameWithoutExtension(GeneratedTestsAssemblyName));
            File.WriteAllText(
                Path.Combine(Application.dataPath, AutomatedQARuntimeSettings.PackageAssetsFolderName, GeneratedTestsFolderName, GeneratedTestsAssemblyName),
                content
            );
        }

        /// <summary>
        /// Generate a simple test from a recording. This test has one line to invoke the playback of a recording, and an assertion to mark the test as passed on completion. 
        /// This test has minimal capability to be modified and customized by the user.
        /// </summary>
        /// <param name="recording"></param>
        public static void GenerateSimpleTest(string recording)
        {
            var recordingFilePath = recording.Split(new string[] { "Assets" }, StringSplitOptions.None).Last().Trim('/').Trim('\\');
            File.WriteAllText(
                    Path.Combine(Application.dataPath, AutomatedQARuntimeSettings.PackageAssetsFolderName, GeneratedTestsFolderName, $"{GetClassNameForRecording(recordingFilePath)}_Tests.cs"),
                    GetRecordedTestScript(recordingFilePath)
            );
        }

        /// <summary>
        /// Gets the template of a simple test.
        /// </summary>
        /// <param name="recording"></param>
        /// <returns></returns>
        private static string GetRecordedTestScript(string recording)
        {
            string templateContent = File.ReadAllText(GeneratedTestScriptTemplatePath);
            var testClassName = GetClassNameForRecording(recording);
            return templateContent
                .Replace("#RECORDING_NAME#", testClassName)
                .Replace("#RECORDING_FILE#", AutomatedQARuntimeSettings.RecordingFolderName + "/" + Path.GetFileName(recording));
        }

        /// <summary>
        /// Generate a class name for a recording.
        /// </summary>
        /// <param name="recordingFilePath"></param>
        /// <returns></returns>
        private static string GetClassNameForRecording(string recordingFilePath)
        {
            var testClassName = Path.GetFileNameWithoutExtension(recordingFilePath);
            testClassName = Regex.Replace(testClassName, @"[\W_]+", "_");
            return testClassName;
        }
    }
}
#endif