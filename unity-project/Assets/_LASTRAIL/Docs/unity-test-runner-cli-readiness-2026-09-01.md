# Unity Test Runner CLI Readiness - 2026-09-01

Purpose: keep launch verification evidence reproducible for Unity `6000.3.22f1` without changing gameplay, UI design, server contracts, rewards, economy, story truth, gacha probability, OAuth, signing, Rail Choice Events, or World Raid scope.

## Root Cause

The failing command combined `-runTests` with `-quit`. In this project on Unity `6000.3.22f1`, that combination can make Unity perform a normal batchmode quit after script compilation before `UnityEditor.TestTools.TestRunner.CommandLineTest.Executer.InitializeAndExecuteRun` starts. The process exits with code `0`, but the requested `-testResults` XML is not created.

The successful command omits `-quit`. Unity Test Framework starts the command-line test run, writes XML to the requested path, and then exits the Editor itself.

## Correct Command

Use:

```powershell
& "D:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe" `
  -batchmode `
  -nographics `
  -projectPath "E:\Projects\LASTRAIL\unity-project" `
  -runTests `
  -testPlatform EditMode `
  -testResults "E:\Projects\LASTRAIL\unity-project\TestResults-EditMode.xml" `
  -logFile "E:\Projects\LASTRAIL\unity-project\Unity-EditMode.log"
```

Do not add `-quit` to `-runTests` commands. Test Framework owns process exit after the run completes.

Focused example:

```powershell
.\Tools\run-editmode-tests.ps1 `
  -TestFilter "LaunchScreenScreenshotQaReportTests" `
  -TestResults "E:\Projects\LASTRAIL\unity-project\TestResults-ScreenshotFidelity.xml" `
  -LogFile "E:\Projects\LASTRAIL\unity-project\Unity-ScreenshotFidelity.log"
```

Full example:

```powershell
.\Tools\run-editmode-tests.ps1 `
  -TestResults "E:\Projects\LASTRAIL\unity-project\TestResults-EditMode.xml" `
  -LogFile "E:\Projects\LASTRAIL\unity-project\Unity-EditMode.log"
```

## Verification Evidence

- Unity version: `6000.3.22f1`.
- Test Framework package in manifest: `com.unity.test-framework` requested as `1.5.1`, resolved by Unity package cache as `1.6.0`.
- Focused no-quit run: `LaunchScreenScreenshotQaReportTests`, total `7`, passed `7`, failed `0`, XML generated at `TestResults-ScreenshotFidelity-NoQuit.xml`.
- Full no-quit run: total `588`, passed `588`, failed `0`, XML generated at `TestResults-ScreenshotFidelity-Full-NoQuit.xml`.
- Logs show `CommandLineTest.Executer.InitializeAndExecuteRun`, `Saving results to`, and `Test run completed. Exiting with code 0`.

## Stable Rule

- `-runTests` command: omit `-quit`.
- ExecuteMethod validation/screenshot commands: `-quit` is still acceptable because those methods are not Test Framework command-line runs.
- A launch verification run is valid only when the requested XML exists and contains a `/test-run` node with nonzero `total`.
- The wrapper waits briefly for the XML file and parseable `/test-run` node because Unity can log `Saving results to` before the file is visible to the caller.
