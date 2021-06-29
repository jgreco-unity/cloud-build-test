# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.5.0] - 2021-06-24
### Major Features and Improvements
- Support for running tests on real iOS devices in the cloud
- New [CloudTest] attribute can be used to specify any Unity Test Framework test to run on real devices in the cloud
- `Automated QA > Test Generation...` allows for step-by-step Unity Test Framework test generation (C# code).
	- Generates code for every step/action taken in a recording.
	- Allows for assertions or additional custom logic between each step.
	- Can select which recordings to generate tests for.
	- Editor Window warns user if about to overwrite custom edits in a recording's test when re-generating test.	

### Breaking changes
- Recording file in RecordedPlaybackAutomator is now an asset reference instead of path string
  - Migration: Update the file path in AutomatedRuns using RecordedPlaybackAutomator

### Bug Fixes and Minor Changes
- Added dynamic wait logic so that differences in load times or animations does not result in automation failing to interact with the target object.
- Delayed loading of the recorded playback controller to avoid issues with initializing too early.

## [0.4.0] - 2021-06-02

### Major Features and Improvements
- HTML & XML reports for AutomatedQA runs.
  - XML report is designed for loading tests results into a CI process run.
  - Latest HTML reportcan be opened in editor from the "Recorded Playback" and "Composite Recordings" editor windows after playback.
  - Both currently stored in Application.persistentDataPath. CI/CD must copy report from path (Will be integrated with Cloud services & modified to take raw test data from devices so that explicit file extraction is not necessary).
- Added settings file for editable values used in Automated Qa package, thus making various AutomatedQA behaviors customizable (more to be added as time goes on).
  - Added new editor window to edit the settings file (Automated QA > Settings).
  - Multiple config files with varying values can be stored in `Assets/AutomatedQA/Resources`, and desired settings can be requested from cloud config via file name.
  
### Bug Fixes and Minor Changes
- Added placeholder segment file names in Composite Recording window when selecting "Save Segment".
- Updated VisualFx to prevent visual clutter from a rapid series of events.
- Updated validation of GameObject that is about to be clicked. Increased performance. Now checks that an object is off screen or under another object that would intercept the click.
- Updated recorded tests to load an empty scene after execution.
- Fixed ignoring depth surface warnings when taking screenshots on Mac.
- Added new Automators: Scene Automator and Text Input Automator.
- Fixed event timings and scene loading with composite recordings

## [0.3.1] - 2021-05-04

### Major Features and Improvements
- New `AutomatedRun` object to link together recordings and custom C# scripts to automate gameplay. 
  - Create an AutomatedRun with `Create -> Automated QA -> Automated Run`
- New `Automator` class. Extend this class to create custom automators
  - Extend the `AutomatorConfig` class for your Automator to expose it in the `AutomatedRun` inspector.
 
### Bug Fixes and Minor Changes
- Fixed an issue where drop events had extra delay
- Fixed an issue with the Upload Recording window requiring entitlements
- Removed dependency on com.unity.nuget.newtonsoft-json package
- Fixed entry scene to work with initialization scenes
- Moved menu items to top level Automated QA menu
- Added fix to CloudTestResults.cs that prevents ITestRunCallback from being eagerly stripped by the "ahead of time" compilation in IL2CPP builds.
- Added logic to cleanup temporary files accumulating from previous recordings and stored in the persistent data path.
- Removed Linq usage from package as Linq can be a heavy library for mobile game development.

## [0.3.0] - 2021-04-21

### Breaking changes
- Simplified assembly definitions 
  - Migration: Please update asmdefs to reference Unity.AutomatedQA
- Renamed asset directory to AutomatedQA 
  - Migration: Please delete the old AutomatedTesting folder
  
### Bug Fixes and Minor Changes
- Package directory restructure
- Added SettingsManager package and AutomatedQAEditorSettings/AutomatedQARuntimeSettings to wrap package settings
- Disabled Cloud Testing window for unsupported platforms
- Fixed an issue with recorded tests using composite recordings
- Fixed an issue causing two simultaneously active EventSystem components, resulting in a flood of console warnings
- Fixed an issue where multi-clicking "Record" or "Play" buttons creates multiple RecordedPlaybackController GameObjects

## [0.2.0] - 2021-04-05

### Major Features and Improvements
- Added new window under Window > Automated Testing > Composite Recordings that allows:
  1) Multiple recordings to be combined together via the UI
  2) Multiple recordings to be captured continuously during record mode
- Added UIElements to composite recordings window
- Update the package display name to Automated QA
- Object interactions now use the original location inside the object during playback

### Bug Fixes and Minor Changes
- Fixed bug where Record mode is automatically enabled on window open
- 2 new window paths: 
  1) Window > Automated Testing > Recorded Playback and
  2) Window > Automated Testing > Advanced > Composite Recorded Playback

## [0.1.0] - 2021-03-01

### Major Features and Improvements
- Can now start recordings by clicking Record or Play in the Recorded Playback window without the need to manually add a RecordedInputModule to the scene.
- Add edit button to rename recordings 

### Bug Fixes and Minor Changes
- Fixed import errors in Unity Editor 2018.4.18f1+. Note: At this time we do not officially support Unity versions less than 2019.4.
- Fix bug where Generated Recorded Tests are created in the wrong directory 
- Fix bug where object presses are sometimes not properly played back 

## [0.0.7] - 2021-02-18

### Major Features and Improvements
- Generate Recorded Tests
  - Menu: Tools > Automated Testing > Generate Recorded Tests
  - Does not work with recording data created before version 0.0.7
- Recorded playback analytics events have been added

### Known Issues
- Very quick presses of buttons in recordings do not play back correctly. Workaround: hold the click for about a second.

### Bug Fixes and Minor Changes
- Pretty print recording JSON data

## [0.0.6-preview.1] - 2021-02-12
- Fixed documentation structure

## [0.0.5-preview.1] - 2021-02-11
- Updated package dependencies

## [0.0.4-preview.1] - 2021-02-08
- Split experimental features out to com.unity.automated-testing.experiments

## [0.0.3-preview.1] - 2021-02-08
- Restructured package directories and updated documentation

## [0.0.2-preview.1] - 2021-01-29
- Merged recorded playback and cloud testing packages

## [0.0.1-preview.1] - 2021-01-14
- Support for running UTF tests using recordings

## [0.0.0-preview.2] - 2020-11-16
- Use object references for recording playback actions

## [0.0.0-preview.1] - 2020-10-06
- Initial version
