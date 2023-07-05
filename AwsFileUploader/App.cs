namespace AwsFileUploader;

using System.Collections.Generic;
using global::Autofac;
using Serilog;

public class App
{
    public App(
        IContainer container,
        LoggerConfiguration loggerConfiguration,
        IDictionary<string, string> environment)
    {
        this.Container = container;
        this.LoggerConfiguration = loggerConfiguration;
        this.Environment = environment;
    }

    public IContainer Container { get; }

    public LoggerConfiguration LoggerConfiguration { get; }

    public IDictionary<string, string> Environment { get; }
}
