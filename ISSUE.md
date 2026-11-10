# A singleton that reads a delegate argument on a cycle through lambdas: CS0103 (regression in #192)

**Pure.DI master (bfa2627)**, a regression from master b6feb8b and 2.5.7. .NET SDK 10.0.403, macOS. Repro project attached (`dotnet build -p:PureDIVersion=…`).

## Minimal code

```csharp
using Pure.DI;
using static Pure.DI.Lifetime;

var composition = new Composition();
Console.WriteLine(composition.Root);

// A singleton that reads a bound string
public sealed class Settings(string path)
{
    public override string ToString() => path;
}

// Two screens that open each other through delegates with a string argument
public sealed class Screen0(Settings settings, Func<string, Screen1> openScreen1, Func<string, Screen0> openScreen0)
{
    public override string ToString() => settings.ToString();
}

public sealed class Screen1(Settings settings, Func<string, Screen0> openScreen0, Func<string, Screen1> openScreen1)
{
    public override string ToString() => settings.ToString();
}

public sealed class App(Func<string, Screen0> open)
{
    public override string ToString() => open("title").ToString();
}

partial class Composition
{
    private static void Setup() => DI.Setup(nameof(Composition))
        .Bind<string>().To(_ => "path")
        .Bind().As(Singleton).To<Settings>()
        .Root<App>("Root");
}
```

## Expected

The code compiles, as with b6feb8b and 2.5.7. Those print `title` and report DIW015, because the built-in `Func<T1, T>` overrides `string` for the singleton too.

## Actual (master bfa2627)

```
Composition.g.cs(115,5): error CS0103: The name 'EnsureSettingsExists314d1' does not exist in the current context
Composition.g.cs(140,5): error CS0103: The name 'EnsureSettingsExists314d' does not exist in the current context
Composition.g.cs(94,13): error CS0103: The name 'hasOverriddenString' does not exist in the current context
```

The screens on the cycle are created by local functions of the root (`CreateScreen0314d(string overriddenString, bool hasOverriddenString)`, see #192). The singleton reads the overridden `string`, so its `Ensure…Exists()` local function is declared in the scope of the overriding lambda, but it is called from the `Create…()` functions, which cannot see it. The body of the singleton's function also tests `hasOverriddenString`, which is a parameter of the `Create…()` functions, not a variable of the lambda:

```csharp
System.Func<string, global::Screen0> localFactory= new System.Func<string, global::Screen0>((string localArg1) =>
{
    string overriddenString = localArg1;

    void EnsureSettingsExists314d()
    {
        ...
                    if (hasOverriddenString)                 // CS0103
                    {
                        overriddenStringOrDefault = overriddenString;
                    }
                    ...
                    _singletonSettings314d = new global::Settings(overriddenStringOrDefault);
    }
    return CreateScreen0314d(overriddenString, true);
});
...
global::Screen0 CreateScreen0314d(string overriddenString, bool hasOverriddenString)
{
    EnsureSettingsExists314d();                              // CS0103: declared inside the lambda above
    ...
}
```

## In a real application

In an app with a few dozen screens on navigation cycles, singletons that read a bound `string` are reached from several delegates with a `string` argument. Code generation for the composition root then never finishes: in 60 s it built the creation code of a few singletons (a storage service, a configuration manager) about 7,000 times each, each time in another local function scope, with a 10 GB heap after 30 minutes. Each of those singletons had `string` among its free overrides.
