using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CLI_Wrapper {

    public class CLI 
    {

        private ProcessStartInfo startInfo;
        private readonly Queue<string> StdOutput = new();
        private readonly Queue<string> StdError = new();

        // Logging
        private StreamWriter? LogFile = null;
        private string? LogPath = null;

        public event EventHandler<string>? OutputDataReceived;
        public event EventHandler<string>? ErrorDataReceived;

        internal CLI(string executableNameOrPath, bool findInPath) {
            if (findInPath)
            {
                try
                {
                    string? path = Environment.GetEnvironmentVariable("PATH")
                        ?.Split(';')
                        ?.Select(folder =>
                        {
                            string cmdPath = Path.Combine(folder, executableNameOrPath);
                            if (File.Exists(cmdPath))
                            {
                                return cmdPath;
                            }

                            string exePath = Path.Combine(folder, $"{executableNameOrPath}.exe");
                            if (File.Exists(exePath))
                            {
                                return exePath;
                            }

                            return null;
                        })
                        ?.FirstOrDefault(path => path != null);

                    if (path == null) throw new FileNotFoundException("Failed to locate executable in PATH.");
                    else executableNameOrPath = path;
                }catch(Exception e)
                {
                    throw new FileNotFoundException($"Failed to find {executableNameOrPath} in PATH.", e);
                }
            }

            this.startInfo = new();
            startInfo.FileName = $"\"{executableNameOrPath}\"";
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardInput = false;
        }

        /// <summary>
        /// Use this if the program is an EXE file that will be ran directly
        /// </summary>
        /// <param name="exePath"></param>
        /// <returns></returns>
        public static CLI RunExeFile(string exePath) {
            return new CLI(exePath, false);
        }

        /// <summary>
        /// Use this if the program is stored in the PATH environment variable
        /// A <see cref="FileNotFoundException"/> is thrown if the program could not be found in PATH.
        /// </summary>
        /// <param name="programName"></param>
        /// <exception cref="FileNotFoundException" />
        /// <returns></returns>
        public static CLI RunPathVariable(string programName) {
            return new CLI(programName, true);
        }

        #region Settings
        public CLI AddArgument(string arg) {
            if (!string.IsNullOrWhiteSpace(this.startInfo.Arguments))
            {
                arg = " " + arg;
            }
            this.startInfo.Arguments += arg;
            return this;
        }

        public CLI AddArguments(params string[] args) {
            return this.AddArguments(args.AsEnumerable());
        }

        public CLI AddArguments(IEnumerable<string> args) {
            if (!string.IsNullOrWhiteSpace(this.startInfo.Arguments))
            {
                this.startInfo.Arguments +=" ";
            }
            this.startInfo.Arguments += string.Join(' ', args);
            return this;
        }

        /// <summary>
        /// Enables logging and sets the path of where to save the log file.
        /// NOTE: If both StdOutput and StdError are used, there is no guarantee
        /// they will be written in the correct order. I.e. data from StdError might
        /// be written before StdOutput even though StdOutput 'happened first'.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public CLI SetLogPath(string path) {
            this.LogPath = path;
            return this;
        }

        /// <summary>
        /// By default the command window is hidden. This will show the command
        /// window while the process is running.
        /// </summary>
        /// <returns></returns>
        public CLI ShowWindow() {
            this.startInfo.CreateNoWindow = false;
            return this;
        }

        /// <summary>
        /// By defaults the working directory is the same as the assmebly.
        /// This selects a different working directoy to use.
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public CLI SetWorkingDirectory(string directory) {
            this.startInfo.WorkingDirectory = directory;
            return this;
        }
        #endregion

        /// <inheritdoc cref="Run"/>
        public async Task<CliResult> RunAsync() {
            return await Task.Run(Run);
        }

        public CliResult Run() {
            StdOutput.Clear();
            StdError.Clear();

            if (this.LogPath != null)
            {
                try
                {
                    this.LogFile = new StreamWriter(File.Open(this.LogPath, FileMode.Create, FileAccess.Write, FileShare.Read));
                }catch(Exception e)
                {
                    return new(new FileNotFoundException("Failed to open log file.", e), int.MinValue, new List<string>(), new List<string>());
                }

                this.LogFile.WriteLine($"{this.startInfo.FileName} {this.startInfo.Arguments}");
            }

            Process process = new Process();
            process.StartInfo = this.startInfo;
            process.OutputDataReceived += ReceiveOutput;
            process.ErrorDataReceived += ReceiveError;
            //process.Exited += ReceiveExit;

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            } catch (Exception e) when (e is Win32Exception || e is InvalidOperationException)
            {
                CloseLog(e);
                return new(new FileNotFoundException("The system cannont file the specified file/command.", e), int.MinValue, StdOutput.ToList(), StdError.ToList());
            } catch (Exception e) {
                // Process has already been disposed (unlikely) or running on Linux/Mac where Process isn't a supported feature (Core only???)
                CloseLog(e);
                return new(e, int.MinValue, StdOutput.ToList(), StdError.ToList());
            }

            try
            {
                process.WaitForExit();
            } catch(SystemException e) {
                CloseLog(e);
                return new(new SystemException("Process has already exited", e), int.MinValue, StdOutput.ToList(), StdError.ToList());
            } catch(Exception e)
            {
                CloseLog(e);
                return new(e, int.MinValue, StdOutput.ToList(), StdError.ToList());
            }

            CloseLog(process.ExitCode);
            return new CliResult(null, process.ExitCode, StdOutput.ToList(), StdError.ToList());
        }

        private void CloseLog(string exitMessage) {
            if (this.LogFile != null)
            {
                // Put these in seperate try/catch statements to try and get as much info into the log file
                // as possible while closing, without throwing any errors into the main program.
                try
                {
                    this.LogFile.WriteLine($"Process exited with {exitMessage}");
                } catch (Exception) { }
                try
                {
                    this.LogFile.Flush();
                } catch (Exception) { }
                try
                {
                    this.LogFile.Close();
                } catch (Exception) { }

                this.LogFile = null;
            }
        }

        private void CloseLog (Exception e) {
            CloseLog($"exception: {e.Message}");
        }

        private void CloseLog(int exitCode) {
            CloseLog($"exit code = {exitCode}");
        }

        private void ReceiveOutput(object sender, DataReceivedEventArgs e) {
            if (e.Data != null)
            {
                if (this.LogFile != null)
                {
                    lock(this.LogFile)
                    {
                        this.LogFile.WriteLine(e.Data);
                    }
                }

                OutputDataReceived?.Invoke(this, e.Data);
                StdOutput.Enqueue(e.Data);
            }
        }

        private void ReceiveError(object sender, DataReceivedEventArgs e) {
            if (e.Data != null)
            {
                if (this.LogFile != null)
                {
                    lock (this.LogFile)
                    {
                        this.LogFile.WriteLine(e.Data);
                    }
                }

                ErrorDataReceived?.Invoke(this, e.Data);
                StdError.Enqueue(e.Data);
            }
        }
    }
}
