namespace WNAB.Maui;

// LLM-Dev:v1 Helper to resolve services in parameterless page constructors.
internal static class ServiceHelper
{
    public static IServiceProvider? Services { get; set; }
    public static T GetService<T>() where T : notnull => GetRequiredService<T>();

    public static T GetRequiredService<T>() where T : notnull
    {
        var provider = Services ?? Application.Current?.Handler?.MauiContext?.Services
            ?? throw new InvalidOperationException("MAUI service provider is not available.");
        return provider.GetService(typeof(T)) is T service
            ? service
            : throw new InvalidOperationException($"Service of type {typeof(T)} not found.");
    }
}
