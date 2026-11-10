# Pure.DI: a singleton that reads a delegate argument on a cycle through lambdas does not compile (CS0103)

Minimal repro for Pure.DI master (bfa2627), a regression from b6feb8b. Two screens open each other through `Func<string, TScreen>`, and both use a singleton that reads a bound `string`. Since the delegate overrides `string`, the singleton reads the delegate argument (DIW015). Master bfa2627 declares the singleton's `Ensure…Exists()` local function inside a delegate lambda, but calls it from the screens' `Create…()` local functions at the root level, and refers to `hasOverriddenString` where it is not declared.

[`ISSUE.md`](ISSUE.md) is the text to paste into a Pure.DI GitHub issue.

```
dotnet build                                                                                   # 2.5.7 from nuget.org: compiles, prints "title", DIW015
dotnet build -p:PureDIVersion=<master build version> -p:RestoreAdditionalProjectSources=<folder with the package>
```

| version | result |
|---|---|
| 2.5.7 | compiles, prints `title`, DIW015 |
| master b6feb8b | compiles, prints `title`, DIW015 |
| master bfa2627 | CS0103 `EnsureSettingsExists314d1`, `EnsureSettingsExists314d`, `hasOverriddenString` |
