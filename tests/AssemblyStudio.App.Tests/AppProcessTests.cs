using Xunit;

namespace AssemblyStudio.App.Tests;

/// <summary>
/// Tests that launch the real application run on their own, after the parallel tests: a startup measurement next to
/// the crash-recovery test (which starts, drives through UI Automation and kills instances) measured 20.8 s instead
/// of about 1.5 s (2026-10-30).
/// </summary>
[CollectionDefinition(nameof(AppProcessTests), DisableParallelization = true)]
public sealed class AppProcessTests;
