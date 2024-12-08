// See https://aka.ms/new-console-template for more information

using CLI_Wrapper;

CLI cli = CLI.RunPathVariable("ffmpeg.exe")
    .SetWorkingDirectory(@"C:\Users\Zach\Downloads")
    .AddArgument("-i VTS-05_VID-0001_CID-02.VOB")
    .AddArgument("-map 0:v? -map 0:2 -map 0:3 -map 0:4 -map 0:5 -map 0:s?")
    .AddArguments("-c:v libx264 -crf 16 -c:a aac -c:s copy -c:d copy -c:t copy")
    .AddArguments("-f mp4")
    .AddArgument("Test.mp4")
    .AddArgument("-y")
    .SetLogPath("ffmpeg_log.txt");

CliResult result = cli.Run();

if (result.Exception != null)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Exception occurred: {result.Exception.Message}");
    Console.ResetColor();
    return;
}

if (result.ExitCode != 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Non-zero exit code: {result.ExitCode}");
    Console.ResetColor();
}

Console.WriteLine("Std Output: ");
foreach(var msg in result.OutputData)
{
    Console.WriteLine(msg);
}

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("Std Error: ");
foreach(var msg in result.ErrorData)
{
    Console.WriteLine(msg);
}
Console.ResetColor();

