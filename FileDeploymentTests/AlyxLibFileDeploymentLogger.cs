using FileDeployment.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FileDeployment.Rules;
using FileDeployment.Operations;
using FileDeployment.Exceptions;
using FileDeployment;

namespace FileDeploymentTests;

public class AlyxLibFileDeploymentLoggerMsg(string message, bool verbose = false, bool ignore = false)
{
    public string Message { get; set; } = message;
    public bool Verbose { get; set; } = verbose;
    /// <summary>
    /// If true, the message will not be logged at all, even if Verbose is true
    /// </summary>
    public bool Ignore { get; set; } = ignore;
}

public class AlyxLibFileDeploymentLogger : IFileDeploymentLogger
{
    private Dictionary<Type, AlyxLibFileDeploymentLoggerMsg> operationMessages = new()
    {
        { typeof(CopyFileOperation), new("Created {0}") },
        { typeof(TemplateFileOperation), new("Created {0}") },
        { typeof(DeleteFileOperation), new("Deleted {0}") },
        { typeof(SymlinkFileOperation), new("Created {0}") }
    };

    private Dictionary<Type, AlyxLibFileDeploymentLoggerMsg> ruleMessages = new()
    {
        { typeof(FileDoesNotExistRule), new("File '{0}' already exists and will not be replaced", true) },
        { typeof(HashRule), new("File hash does not match", true) },
        { typeof(FileNameDoesNotExistRule), new("") }
    };

    private Dictionary<Type, AlyxLibFileDeploymentLoggerMsg> exceptionMessages = new()
    {
        //{ typeof(FileAlreadyExistsException), new("File '{0}' already exists and will not be replaced", true) },
        //{ typeof(FileOperationException), new("File operation failed") },
        //{ typeof(FileNotFoundException), new("", false, true) } // This is a common exception, so we ignore it by default
    };

    public bool Verbose { get; set; } = true;

    public static string TrimPrefixDirectory(string source, params string[] dirs)
    {
        foreach (var dir in dirs)
        {
            var index = source.IndexOf(dir);
            if (index >= 0)
            {
                var start = index + dir.Length;
                if (start < source.Length && (source[start] == '/' || source[start] == '\\'))
                    start++; // Skip separator
                return source[start..];
            }
        }

        return source;
    }

    private static string FormatMessage(string format, string addition)
    {
        // if addition is not at the start of format (i.e. {0} is not the start, lowercase the first letter of addition.
        // if addition does not end with a period, add one
        if (!format.StartsWith("{0}"))
        {
            addition = char.ToLowerInvariant(addition[0]) + addition[1..];
        }
        if (!addition.EndsWith('.'))
        {
            addition += '.';
        }
        return string.Format(format, addition);
    }
    private static string FormatMessage(string message)
    {
        // If the message is not capitalized, capitalize the first letter
        if (char.IsLower(message[0]))
        {
            message = char.ToUpperInvariant(message[0]) + message[1..];
        }
        // If the message does not end with a period, add one
        if (!message.EndsWith('.'))
        {
            message += '.';
        }
        return message;
    }

    public void Log(LogEntry entry)
    {
        string source = Verbose ? entry.Source : entry.Source.Split('/').LastOrDefault() ?? entry.Source;
        string destination = entry.HasDestination ? (Verbose ? entry.Destination : entry.Destination.Split('/').LastOrDefault() ?? entry.Destination) : "N/A";
        if (Verbose)
        {
            //source = FileUtils.TrimPathUpToDirectory(source, "alyxlib");
            //destination = FileUtils.TrimPathUpToDirectory(destination, "content");
            //if (source.Contains("content"))
            //    source = source[(source.IndexOf("content") + "content".Length + 1)..]; // Remove "content/" prefix
            //if (source.Contains("game"))
            //    source = source[(source.IndexOf("game") + "game".Length + 1)..]; // Remove "addons/" prefix
            //if (source.Contains("alyxlib"))
            //    source = source[(source.IndexOf("alyxlib") + "alyxlib".Length + 1)..]; // Remove "alyxlib/" prefix
            source = TrimPrefixDirectory(source, "content", "game", "alyxlib");
            destination = TrimPrefixDirectory(destination, "content", "game", "alyxlib");
        }


        string message = entry.Operation.Description ?? entry.Message;

        if (entry.HasException)
        {
            message = entry.Exception.Message ?? message;
        }
        else if (entry.HasRule)
        {
            if (entry.Rule is FileWillNotBeReplaced rule)
            {
                message = $"File '{destination}' already exists and will not be replaced.";
            }
            else
                message = entry.Rule.Description ?? message;
        }

        switch (entry.Type)
        {
            case LogEntryType.Error:
                Console.WriteLine($"ERROR: {FormatMessage(message)}");
                break;

            case LogEntryType.Warning:
                Console.WriteLine($"WARNING: {FormatMessage(message)}");
                break;

            case LogEntryType.Info:
                Console.WriteLine($"INFO: {FormatMessage(message)}");
                break;

            case LogEntryType.Success:
                Console.WriteLine($"SUCCESS: {FormatMessage(message)}");
                break;
        }

        //if (entry.HasException)
        //{
        //    // These trygets should be replaced with a debug console method selection
        //    //if (exceptionMessages.TryGetValue(entry.Exception.GetType(), out var msg))
        //    //{
        //    //    if (msg.Verbose && Verbose)
        //    //    {
        //    //        Console.WriteLine($"(verbose) ERROR: {FormatMessage(msg.Message, destination)}");
        //    //    }
        //    //    else
        //    //    {
        //    //        Console.WriteLine($"ERROR: {FormatMessage(msg.Message, destination)}");
        //    //    }
        //    //}
        //    //else
        //    //{
        //    //    Console.WriteLine($"!!BAD HasException ({entry.Exception.GetType().Name}) {entry.Exception.Message}");
        //    //}
        //    Console.WriteLine($"ERROR: ({entry.Exception.GetType().Name}) {entry.Exception.Message}");
        //}
        //else if (entry.FailedBecauseOfRule)
        //{
        //    //var rule = entry.Rule;
        //    //if (ruleMessages.TryGetValue(rule.GetType(), out var msg))
        //    //{
        //    //    string message = rule.Description ?? msg.Message;
        //    //    if (msg.Verbose && Verbose)
        //    //    {
        //    //        Console.WriteLine($"(verbose) WARNING: {FormatMessage(message, destination)}");
        //    //    }
        //    //    else
        //    //    {
        //    //        Console.WriteLine($"WARNING: {FormatMessage(message, destination)}");
        //    //    }

        //    //}
        //    //else
        //    //{
        //    //    Console.WriteLine($"!!BAD FailedBecauseOfRule {entry.Operation.GetType().Name} - {source} -> {destination ?? "N/A"}: {entry.Message} (Rule: {entry.Rule.GetType().Name})");
        //    //}
        //    if (entry.Rule.Description != null)
        //        Console.WriteLine($"?? {FormatMessage(entry.Rule.Description)}");
        //    else
        //        Console.WriteLine($"RULE: {entry.Rule.GetType().Name} {entry.RelevantPath}");
        //}
        //else
        //{
        //    //Console.WriteLine($"SUCCESS: {entry.Operation.GetType().Name} - {source} -> {destination ?? "N/A"}: {entry.Message}");
        //    if (operationMessages.TryGetValue(entry.Operation.GetType(), out var msg))
        //    {
        //        if (msg.Verbose && Verbose)
        //        {
        //            Console.WriteLine($"(verbose) {FormatMessage(msg.Message, destination)}");
        //        }
        //        else
        //        {
        //            Console.WriteLine(FormatMessage(msg.Message, destination));
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine($"!!BAD LAST {entry.Operation.GetType().Name} - {source} -> {destination ?? "N/A"}: {entry.Message}");
        //    }
        //}

        //if (entry.HasException)
        //{
        //    if (entry.FailedBecauseOfRule)
        //    {
        //        Console.WriteLine($"ERROR: {entry.OperationType.Name} - {source} -> {destination ?? "N/A"}: {entry.Message} (Rule: {entry.RuleType?.Name}, Exception: ({entry.Exception.GetType().Name}) {entry.Exception.Message})");
        //    }
        //    else
        //    {
        //        Console.WriteLine($"ERROR: {entry.OperationType.Name} - {source} -> {destination ?? "N/A"}: {entry.Message} (Exception: ({entry.Exception.GetType().Name}) {entry.Exception.Message})");
        //    }
        //}
        //else if (entry.FailedBecauseOfRule)
        //{
        //    Console.WriteLine($"WARNING: {entry.OperationType.Name} - {source} -> {destination ?? "N/A"}: {entry.Message} (Rule: {entry.RuleType?.Name})");
        //}
        //else
        //{
        //    Console.WriteLine($"INFO: {entry.OperationType.Name} - {source} -> {destination ?? "N/A"}: {entry.Message}");
        //}
    }
}