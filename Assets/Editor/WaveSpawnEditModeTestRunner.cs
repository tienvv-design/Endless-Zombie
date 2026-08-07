using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
public sealed class WaveSpawnEditModeTestRunner : ICallbacks
{
    private static readonly string ProjectRoot = Directory.GetParent(Application.dataPath).FullName;
    private static readonly string RequestPath = Path.Combine(ProjectRoot, "outputs", "wave-spawn-roadmap", "run-editmode-tests.request");
    private static readonly string ResultPath = Path.Combine(ProjectRoot, "outputs", "wave-spawn-roadmap", "editmode-results.xml");

    static WaveSpawnEditModeTestRunner()
    {
        if (!File.Exists(RequestPath))
            return;

        File.Delete(RequestPath);
        EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new WaveSpawnEditModeTestRunner());
        api.Execute(new ExecutionSettings(new Filter
        {
            testMode = TestMode.EditMode,
            groupNames = new[] { "^WaveSpawnConfigTests" },
        })
        {
            runSynchronously = true,
        });
    }

    public void RunStarted(ITestAdaptor testsToRun) { }

    public void RunFinished(ITestResultAdaptor result)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
        string summary =
            $"state={result.ResultState}\n" +
            $"passed={result.PassCount}\n" +
            $"failed={result.FailCount}\n" +
            $"skipped={result.SkipCount}\n" +
            $"inconclusive={result.InconclusiveCount}\n" +
            $"durationSeconds={result.Duration:0.000}\n" +
            $"message={result.Message}\n";
        File.WriteAllText(ResultPath, summary);
        Debug.Log($"[WaveSpawnTests] Finished: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped.");
    }

    public void TestStarted(ITestAdaptor test) { }
    public void TestFinished(ITestResultAdaptor result) { }
}
