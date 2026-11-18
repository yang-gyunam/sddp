using Sddp.Diagnostics;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace Sddp.Api.Extensions;

internal sealed class DiagnosticLogSink(Logger target) : ILogEventSink, IDisposable
{
    internal static void Configure(HostBuilderContext context, LoggerConfiguration configuration)
    {
        var settings = context.Configuration.GetSection("Serilog");
        // Apply levels and enrichers before masking; only destinations execute behind the mask.
        IConfiguration Section(bool destinations)
        {
            var values = settings.AsEnumerable().Where(pair =>
            {
                var key = pair.Key.Split(':').ElementAtOrDefault(1);
                return key is "Using" or "LevelSwitches" || destinations == (key is "WriteTo" or "AuditTo");
            });
            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }
        var sinks = new LoggerConfiguration().MinimumLevel.Verbose().ReadFrom.Configuration(Section(true));
        if (context.Configuration["OpenTelemetry:Endpoint"] is { Length: > 0 } endpoint)
            sinks.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = endpoint;
                options.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                options.ResourceAttributes = new Dictionary<string, object> { ["service.name"] = "sddp-api" };
            });
        configuration.ReadFrom.Configuration(Section(false)).Enrich.FromLogContext()
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
            .WriteTo.Sink(new DiagnosticLogSink(sinks.CreateLogger()));
    }

    public void Emit(LogEvent original)
    {
        var props = original.Properties.Select(pair => new LogEventProperty(pair.Key, Value(pair.Key, pair.Value))).ToList();
        Exception? maskedException = null;
        if (original.Exception is { } error)
        {
            props.RemoveAll(p => p.Name is "ExceptionType" or "ExceptionDetail");
            props.Add(new("ExceptionType", new ScalarValue(error.GetType().FullName)));
            var detail = DiagnosticMask.Text(error.ToString(), 16000)!;
            props.Add(new("ExceptionDetail", new ScalarValue(detail)));
            maskedException = new MaskedException(DiagnosticMask.Text(error.Message)!, DiagnosticMask.Text(error.StackTrace, 16000), detail);
        }
        var template = original.MessageTemplate;
        var text = DiagnosticMask.Text(template.Text, 16000)!;
        if (text != template.Text) template = new MessageTemplateParser().Parse(text);
        target.Write(new LogEvent(original.Timestamp, original.Level, maskedException, template, props,
            original.TraceId ?? default, original.SpanId ?? default));
    }

    private static LogEventPropertyValue Value(string name, LogEventPropertyValue value)
    {
        if (value is ScalarValue { Value: int or long or double } && name.EndsWith("tokens", StringComparison.OrdinalIgnoreCase))
            return value;
        if (DiagnosticMask.IsSensitiveKey(name) || name.Equals("parameters", StringComparison.OrdinalIgnoreCase))
            return new ScalarValue(DiagnosticMask.Hidden);
        return value switch
        {
            ScalarValue { Value: string s } => new ScalarValue(name.ToLowerInvariant() switch
            {
                "commandtext" or "db.statement" or "db.query.text" or "sql" => DiagnosticMask.Sql(s),
                "requestpath" or "requesturi" or "url.full" or "url.path" => DiagnosticMask.Url(s),
                _ => DiagnosticMask.Text(s, 16000)
            }),
            ScalarValue { Value: Uri uri } => new ScalarValue(DiagnosticMask.Url(uri.ToString())),
            StructureValue structure => new StructureValue(structure.Properties.Select(p =>
                new LogEventProperty(p.Name, Value(p.Name, p.Value))), structure.TypeTag),
            SequenceValue array => new SequenceValue(array.Elements.Select(v => Value(name, v))),
            DictionaryValue dict => new DictionaryValue(dict.Elements.Select(p =>
                KeyValuePair.Create(p.Key, Value(p.Key.Value?.ToString() ?? "", p.Value)))),
            _ => value
        };
    }

    private sealed class MaskedException(string message, string? stack, string detail) : Exception(message)
    {
        public override string? StackTrace => stack;
        public override string ToString() => detail;
    }

    public void Dispose() => target.Dispose();
}
