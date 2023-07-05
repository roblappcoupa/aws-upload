namespace AwsFileUploader;

using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

public static class AppFactory
{
    private const string ConfigAppName = "AwsFileUploader";

    public static App CreateApp(string[] args)
    {
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        var configurationRoot = configurationBuilder.Build();

        var appConfiguration = new AppConfiguration();
        configurationRoot
            .GetSection(ConfigAppName)
            .Bind(appConfiguration);

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .ReadFrom.Configuration(configurationRoot);

        var container = ConfigureAutofacContainer(configurationRoot, appConfiguration);

        var systemInfo = GetSystemInfo();

        //FlurlHttp.Configure(
        //    settings =>
        //    {
        //        var jsonSettings = JsonUtilities.SerializerSettings;
        //        settings.JsonSerializer = new NewtonsoftJsonSerializer(jsonSettings);

        //        settings.Timeout = TimeSpan.FromDays(2);

        //        //settings.HttpClientFactory = new Http2ClientFactory();
        //    });

        return new App(container, loggerConfiguration, systemInfo);
    }

    private static IContainer ConfigureAutofacContainer(IConfiguration configuration, AppConfiguration appConfig)
    {
        var serviceCollection = new ServiceCollection();

        ConfigureServiceCollection(serviceCollection, configuration, appConfig, ConfigAppName);

        var containerBuilder = new ContainerBuilder();

        // Once you've registered everything in the ServiceCollection, call
        // Populate to bring those registrations into Autofac
        containerBuilder.Populate(serviceCollection);

        // Make your Autofac registrations. Order is important!
        // If you make them BEFORE you call Populate, then the
        // registrations in the ServiceCollection will override Autofac
        // registrations; if you make them AFTER Populate, the Autofac
        // registrations will override. You can make registrations
        // before or after Populate, however you choose.
        AddAutofacRegistrations(containerBuilder);

        // Creating a new AutofacServiceProvider makes the container
        // available to your app using the Microsoft IServiceProvider
        // interface so you can use those abstractions rather than
        // binding directly to Autofac.
        var container = containerBuilder.Build();

        return container;
    }

    private static void ConfigureServiceCollection(
        IServiceCollection serviceCollection,
        IConfiguration configuration,
        AppConfiguration appConfig,
        string applicationName)
    {
        serviceCollection.AddOptions();
        serviceCollection.Configure<AppConfiguration>(
            configureOptions =>
                configuration
                    .GetSection(applicationName)
                    .Bind(configureOptions));

        serviceCollection.AddLogging(
            loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog();
            });

        //serviceCollection.AddHttpClient();

        //var httpClientBuilder = serviceCollection.AddHttpClient(
        //    "AWS",
        //    client =>
        //    {
        //        client.DefaultRequestVersion = new Version(2, 0);
        //        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        //    });

        //if (appConfig.MaxHttpConnections.HasValue)
        //{
        //    ServicePointManager.DefaultConnectionLimit = appConfig.MaxHttpConnections.Value;

        //    httpClientBuilder
        //        .ConfigurePrimaryHttpMessageHandler(
        //            _ =>
        //            {
        //                var handler = new SocketsHttpHandler
        //                {
        //                    MaxConnectionsPerServer = appConfig.MaxHttpConnections.Value
        //                };

        //                return handler;
        //            });
        //}
    }

    private static void AddAutofacRegistrations(ContainerBuilder containerBuilder)
    {
        containerBuilder.RegisterType<ProcessorQueue>().As<IProcessorQueue>().SingleInstance();
        containerBuilder.RegisterType<UrlClient>().As<IUrlClient>().SingleInstance();
        containerBuilder.RegisterType<UrlProvider>().As<IUrlProvider>().SingleInstance();

        containerBuilder.RegisterType<AppRunner>().AsSelf();
    }

    private static IDictionary<string, string> GetSystemInfo()
    {
        var currentProcess = System.Diagnostics.Process.GetCurrentProcess();

        return new Dictionary<string, string>
               {
                   { nameof(Environment.OSVersion), Environment.OSVersion.ToString() },
                   { nameof(Environment.Is64BitOperatingSystem), Environment.Is64BitOperatingSystem.ToString() },
                   { nameof(currentProcess.ProcessName), currentProcess.ProcessName },
                   { "ProcessId", currentProcess.Id.ToString() },
                   { nameof(Environment.Is64BitProcess), Environment.Is64BitProcess.ToString() },
                   { "Cores", Environment.ProcessorCount.ToString() },
                   { nameof(Environment.MachineName), Environment.MachineName },
                   { nameof(Environment.CurrentDirectory), Environment.CurrentDirectory },
                   { nameof(Environment.UserName), Environment.UserName },
                   { "CLR Version", Environment.Version.ToString() },
                   { nameof(Environment.CurrentManagedThreadId), Environment.CurrentManagedThreadId.ToString() },
                   { "Current UTC Date", DateTime.UtcNow.ToString("F") }
               };
    }
}