using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Model;

namespace NzbDrone.Common.Processes
{
    public interface IProcessProvider
    {
        int GetCurrentProcessId();
        ProcessInfo GetCurrentProcess();
        ProcessInfo GetProcessById(int id);
        List<ProcessInfo> FindProcessByName(string name);
        void OpenDefaultBrowser(string url);
        void WaitForExit(Process process);
        void SetPriority(int processId, ProcessPriorityClass priority);
        void KillAll(string processName);
        void Kill(int processId);
        bool Exists(int processId);
        bool Exists(string processName);
        ProcessPriorityClass GetCurrentProcessPriority();
        Process Start(string path, string args = null, StringDictionary environmentVariables = null, Action<string> onOutputDataReceived = null, Action<string> onErrorDataReceived = null);
        Process SpawnNewProcess(string path, string args = null, StringDictionary environmentVariables = null, bool noWindow = false);
        ProcessOutput StartAndCapture(string path, string args = null, StringDictionary environmentVariables = null);
    }

    public class ProcessProvider : IProcessProvider
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ProcessProvider> _logger;

        public const string SONARR_PROCESS_NAME = "Sonarr";
        public const string SONARR_CONSOLE_PROCESS_NAME = "Sonarr.Console";

        public ProcessProvider(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ProcessProvider>();
        }

        public int GetCurrentProcessId()
        {
            return Process.GetCurrentProcess().Id;
        }

        public ProcessInfo GetCurrentProcess()
        {
            return ConvertToProcessInfo(Process.GetCurrentProcess());
        }

        public bool Exists(int processId)
        {
            return GetProcessById(processId) != null;
        }

        public bool Exists(string processName)
        {
            return GetProcessesByName(processName).Any();
        }

        public ProcessPriorityClass GetCurrentProcessPriority()
        {
            return Process.GetCurrentProcess().PriorityClass;
        }

        public ProcessInfo GetProcessById(int id)
        {
            _logger.LogDebug("Finding process with Id:{Id}", id);

            var processInfo = ConvertToProcessInfo(Process.GetProcesses().FirstOrDefault(p => p.Id == id));

            if (processInfo == null)
            {
                _logger.LogWarning("Unable to find process with ID {Id}", id);
            }
            else
            {
                _logger.LogDebug("Found process {ProcessInfo}", processInfo.ToString());
            }

            return processInfo;
        }

        public List<ProcessInfo> FindProcessByName(string name)
        {
            return GetProcessesByName(name).Select(ConvertToProcessInfo).Where(c => c != null).ToList();
        }

        public void OpenDefaultBrowser(string url)
        {
            _logger.LogInformation("Opening URL [{Url}]", url);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo(url)
                {
                    UseShellExecute = true
                }
            };

            process.Start();
        }

        public Process Start(string path, string args = null, StringDictionary environmentVariables = null, Action<string> onOutputDataReceived = null, Action<string> onErrorDataReceived = null)
        {
            (path, args) = GetPathAndArgs(path, args);

            var logger = _loggerFactory.CreateLogger(new FileInfo(path).Name);

            var startInfo = new ProcessStartInfo(path, args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            };

            if (environmentVariables != null)
            {
                foreach (DictionaryEntry environmentVariable in environmentVariables)
                {
                    try
                    {
                        _logger.LogTrace("Setting environment variable '{Key}' to '{Value}'", environmentVariable.Key, environmentVariable.Value);
                        startInfo.EnvironmentVariables.Add(environmentVariable.Key.ToString(), environmentVariable.Value.ToString());
                    }
                    catch (Exception e)
                    {
                        if (environmentVariable.Value == null)
                        {
                            _logger.LogError(e, "Unable to set environment variable '{Key}', value is null", environmentVariable.Key);
                        }

                        else
                        {
                            _logger.LogError(e, "Unable to set environment variable '{Key}'", environmentVariable.Key);
                        }

                        throw;
                    }
                }
            }

            logger.LogDebug("Starting {Path} {Args}", path, args);

            var process = new Process
            {
                StartInfo = startInfo
            };

            process.OutputDataReceived += (sender, eventArgs) =>
            {
                if (string.IsNullOrWhiteSpace(eventArgs.Data)) return;

                logger.LogDebug("{Data}", eventArgs.Data);

                if (onOutputDataReceived != null)
                {
                    onOutputDataReceived(eventArgs.Data);
                }
            };

            process.ErrorDataReceived += (sender, eventArgs) =>
            {
                if (string.IsNullOrWhiteSpace(eventArgs.Data)) return;

                logger.LogError("{Data}", eventArgs.Data);

                if (onErrorDataReceived != null)
                {
                    onErrorDataReceived(eventArgs.Data);
                }
            };

            process.Start();

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            return process;
        }

        public Process SpawnNewProcess(string path, string args = null, StringDictionary environmentVariables = null, bool noWindow = false)
        {
            (path, args) = GetPathAndArgs(path, args);

            _logger.LogDebug("Starting {Path} {Args}", path, args);

            var startInfo = new ProcessStartInfo(path, args);
            startInfo.CreateNoWindow = noWindow;
            startInfo.UseShellExecute = !noWindow;

            var process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();

            return process;
        }

        public ProcessOutput StartAndCapture(string path, string args = null, StringDictionary environmentVariables = null)
        {
            var output = new ProcessOutput();
            var process = Start(path, args, environmentVariables, s => output.Lines.Add(new ProcessOutputLine(ProcessOutputLevel.Standard, s)),
                                                                  error => output.Lines.Add(new ProcessOutputLine(ProcessOutputLevel.Error, error)));

            process.WaitForExit();
            output.ExitCode = process.ExitCode;

            return output;
        }

        public void WaitForExit(Process process)
        {
            _logger.LogDebug("Waiting for process {ProcessName} to exit.", process.ProcessName);

            process.WaitForExit();
        }

        public void SetPriority(int processId, ProcessPriorityClass priority)
        {
            var process = Process.GetProcessById(processId);

            _logger.LogInformation("Updating [{ProcessName}] process priority from {PriorityClass} to {Priority}",
                        process.ProcessName,
                        process.PriorityClass,
                        priority);

            process.PriorityClass = priority;
        }

        public void Kill(int processId)
        {
            var process = Process.GetProcesses().FirstOrDefault(p => p.Id == processId);

            if (process == null)
            {
                _logger.LogWarning("Cannot find process with id: {ProcessId}", processId);
                return;
            }

            process.Refresh();

            if (process.Id != Process.GetCurrentProcess().Id && process.HasExited)
            {
                _logger.LogDebug("Process has already exited");
                return;
            }

            _logger.LogInformation("[{ProcessId}]: Killing process", process.Id);
            process.Kill();
            _logger.LogInformation("[{ProcessId}]: Waiting for exit", process.Id);
            process.WaitForExit();
            _logger.LogInformation("[{ProcessId}]: Process terminated successfully", process.Id);
        }

        public void KillAll(string processName)
        {
            var processes = GetProcessesByName(processName);

            _logger.LogDebug("Found {Count} processes to kill", processes.Count);

            foreach (var processInfo in processes)
            {
                if (processInfo.Id == Process.GetCurrentProcess().Id)
                {
                    _logger.LogDebug("Tried killing own process, skipping: {ProcessId} [{ProcessName}]", processInfo.Id, processInfo.ProcessName);
                    continue;
                }

                _logger.LogDebug("Killing process: {ProcessId} [{ProcessName}]", processInfo.Id, processInfo.ProcessName);
                Kill(processInfo.Id);
            }
        }

        private ProcessInfo ConvertToProcessInfo(Process process)
        {
            if (process == null) return null;

            process.Refresh();

            ProcessInfo processInfo = null;

            try
            {
                if (process.Id <= 0) return null;

                processInfo = new ProcessInfo();
                processInfo.Id = process.Id;
                processInfo.Name = process.ProcessName;
                processInfo.StartPath = GetExeFileName(process);

                if (process.Id != Process.GetCurrentProcess().Id && process.HasExited)
                {
                    processInfo = null;
                }
            }
            catch (Win32Exception e)
            {
                _logger.LogWarning(e, "Couldn't get process info for {ProcessName}", process.ProcessName);
            }

            return processInfo;

        }

        private static string GetExeFileName(Process process)
        {
            if (process.MainModule.FileName != "mono.exe")
            {
                return process.MainModule.FileName;
            }

            return process.Modules.Cast<ProcessModule>().FirstOrDefault(module => module.ModuleName.ToLower().EndsWith(".exe")).FileName;
        }

        private List<Process> GetProcessesByName(string name)
        {
            //TODO: move this to an OS specific class

            var monoProcesses = Process.GetProcessesByName("mono")
                                       .Union(Process.GetProcessesByName("mono-sgen"))
                                       .Where(process =>
                                              process.Modules.Cast<ProcessModule>()
                                                     .Any(module =>
                                                          module.ModuleName.ToLower() == name.ToLower() + ".exe"));

            var processes = Process.GetProcessesByName(name)
                                   .Union(monoProcesses).ToList();

            _logger.LogDebug("Found {ProcessCount} processes with the name: {Name}", processes.Count, name);

            try
            {
                foreach (var process in processes)
                {
                    _logger.LogDebug(" - [{ProcessId}] {ProcessName}", process.Id, process.ProcessName);
                }
            }
            catch
            {
                // Don't crash on gettings some log data.
            }

            return processes;
        }

        private (string Path, string Args) GetPathAndArgs(string path, string args)
        {
            //TODO: IsLinux?
            /*if (PlatformInfo.IsMono && path.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
            {
                return ("mono", $"--debug {path} {args}");
            }*/
            if (OsInfo.IsOsx && path.EndsWith(".app", StringComparison.InvariantCultureIgnoreCase))
            {
                return ("/usr/bin/open", $"--new {path} --args {args}");
            }

            if (OsInfo.IsWindows && path.EndsWith(".bat", StringComparison.InvariantCultureIgnoreCase))
            {
                return ("cmd.exe", $"/c {path} {args}");
            }

            if (OsInfo.IsWindows && path.EndsWith(".ps1", StringComparison.InvariantCultureIgnoreCase))
            {
                return ("powershell.exe", $"-ExecutionPolicy Bypass -NoProfile -File {path} {args}");
            }

            if (OsInfo.IsWindows && path.EndsWith(".py", StringComparison.InvariantCultureIgnoreCase))
            {
                return ("python.exe", $"{path} {args}");
            }

            return (path, args);
        }
    }
}
