# [CLI-Wrapper](https://www.nuget.org/packages/CLI-Wrapper/) 
A .NET 8 wrapper for System.Diagnostics.Process for running CLI programs easier

[![NuGet Version](https://img.shields.io/nuget/v/CLI-Wrapper)](https://www.nuget.org/packages/CLI-Wrapper/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CLI-Wrapper)](https://www.nuget.org/packages/CLI-Wrapper/)
[![GitHub issues](https://img.shields.io/github/issues/ZacharyCarey/CLI-Wrapper)](https://github.com/ZacharyCarey/CLI-Wrapper/issues)
[![GitHub stars](https://img.shields.io/github/stars/ZacharyCarey/CLI-Wrapper)](https://github.com/ZacharyCarey/CLI-Wrapper/stargazers)
[![GitHub](https://img.shields.io/github/license/ZacharyCarey/CLI-Wrapper)](https://github.com/ZacharyCarey/CLI-Wrapper/blob/master/LICENSE)
[![GitHub code contributors](https://img.shields.io/github/contributors/ZacharyCarey/CLI-Wrapper)](https://github.com/ZacharyCarey/CLI-Wrapper/graphs/contributors)

# Features
- Set logging file output
- Add arguments one at a time or using an IEnumerable
- Set working directory
- Find the .exe path using the PATH environment
- Run the .exe directly with the file path
- Automatically extracts Std output and Std error
- Use events to parse output data while the program is running, or parse all lines once the program finishes
- Show output window if desired
- Functions to run as async

# Example
This is en exmaple that uses most of the features. It runs a FFMpeg command using the PATH to find
the .exe file.
```c#
using CLI_Wrapper;

// Arguments that will be added later to demonstrate adding arguments of an IEnumerable
string[] mapArgs = { "-map 0:v?", "-map 0:2", "-map 0:3", "-map 0:4", "-map 0:5", "-map 0:s?" };
string[] codecArgs = { "-c:v libx264", "-crf 16", "-c:a aac"};

// This will find the .exe using the PATH environemtn variable.
// You can also call CLI.RunExeFile(exePath) if you know the path to the exe
CLI cli = CLI.RunPathVariable("ffmpeg.exe") 
    .SetWorkingDirectory(@"C:\Users\Zach\Downloads") // Sets the working directory to run the process from
    .AddArgument("-i VTS-05_VID-0001_CID-02.VOB")    // Add a single string literal argument
    .AddArguments(mapArgs)                           // Add arguments using an IEnumerable 
    .AddArguments(codecArgs)
    .AddArguments("-f mp4", "Test.mp4", "-y")        // Add multiple string literal arguments
    .SetLogPath("ffmpeg_log.txt");                   // Save the output of the process to a file (relative to program working directory, not CLI working directory)

// Parse the output data while the program is running.
// If you want to use IProgress<>, this is where it should be done.
// NOTE: Most programs print over StdOutput, but FFMpeg is strange and uses the StdError stream instead
cli.ErrorDataReceived += (object? sender, string line) =>
{
    if (line.StartsWith("Output #0, mp4, to"))
    {
        // Shows that this is ran while the process is running, as it is printed before
        // the full output gets printed in the rest of the code
        int start = line.IndexOf('\'');
        int stop = line.LastIndexOf('\'');
        string fileName = line[(start+1)..stop];
        Console.WriteLine($"Found output file name: {fileName}");
    }
};

// Starts the process and retrieves the resulting output.
// This function will block until the process is finished. Use RunAsync() for async capabilities.
// The CLI object can be reused if you want to build the arguments once and reuse them multiple times. Just call Run() as needed.
CliResult result = cli.Run();

Console.WriteLine();

// Verify that the process ran without any errors.
if (result.Exception != null)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Exception occurred: {result.Exception.Message}");
    Console.ResetColor();
    return;
}

// Check the output of the process. FFMpeg will report "0" on success.
// The value of this is entirely dependent on the process you are running.
if (result.ExitCode != 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Non-zero exit code: {result.ExitCode}");
    Console.ResetColor();
}

// Print all StdOutput that was received from the process.
Console.WriteLine("Std Output: ");
foreach(var msg in result.OutputData)
{
    Console.WriteLine(msg);
}

Console.WriteLine();

// Print all StdError that was received from the process.
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("Std Error: ");
foreach(var msg in result.ErrorData)
{
    Console.WriteLine(msg);
}
Console.ResetColor();
```


# Compatibility
Built and tested for x64 Windows.

## Code contributors
<a href="https://github.com/ZacharyCarey/CLI-Wrapper/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=ZacharyCarey/CLI-Wrapper" />
</a>

### License

Copyright © 2023

Released under [MIT license](https://en.wikipedia.org/wiki/MIT_License)