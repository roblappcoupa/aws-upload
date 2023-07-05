namespace AwsFileUploader;

using System;
using System.Threading.Tasks;
using global::Autofac;
using Serilog;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        int exitCode;

        try
        {
            Console.WriteLine("Application starting");
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            Console.WriteLine("Building application");
            var application = AppFactory.CreateApp(args);

            Console.WriteLine("Initializing logger");
            Log.Logger = application.LoggerConfiguration.CreateLogger();

            Console.WriteLine("Creating LifetimeScope on the DI container");
            await using (var scope = application.Container.BeginLifetimeScope())
            {
                Console.WriteLine("Resolving AppRunner from LifetimeScope");
                var appRunner = scope.Resolve<AppRunner>();

                await appRunner.Run();

                Console.WriteLine("AppRunner completed");
            }

            exitCode = 0;
        }
        catch (Exception exception)
        {
            Console.WriteLine("An Exception was thrown:\n{0}", exception);
            exitCode = 1;
        }

        Console.WriteLine("Application Completed. Exit code: {0}", exitCode);

        return exitCode;
    }

    private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
    {
        var exception = (Exception)args.ExceptionObject;
        Console.WriteLine("An unhandled Exception was thrown:\n{0}", exception);
        Console.WriteLine("Is runtime terminating? {0}", args.IsTerminating);
    }
}