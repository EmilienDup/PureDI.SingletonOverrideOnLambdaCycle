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
