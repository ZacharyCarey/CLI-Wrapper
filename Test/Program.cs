// See https://aka.ms/new-console-template for more information

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
