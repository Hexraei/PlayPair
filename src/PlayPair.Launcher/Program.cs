using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("       PlayPair Launcher Service        ");
        Console.WriteLine("========================================");

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        // In case it's run from the launcher's bin folder, find the repository root
        string repoRoot = baseDir;
        while (!string.IsNullOrEmpty(repoRoot) && !File.Exists(Path.Combine(repoRoot, "PlayPair.sln")))
        {
            string parent = Path.GetDirectoryName(repoRoot);
            if (parent == repoRoot) break;
            repoRoot = parent;
        }

        if (string.IsNullOrEmpty(repoRoot) || !File.Exists(Path.Combine(repoRoot, "PlayPair.sln")))
        {
            repoRoot = Directory.GetCurrentDirectory();
        }

        Console.WriteLine($"Repository Root: {repoRoot}");

        // Find Server Path
        string serverPath = null;
        string[] serverCandidates = new[]
        {
            Path.Combine(repoRoot, @"src\PlayPair.Server\bin\Debug\net8.0\PlayPair.Server.exe"),
            Path.Combine(repoRoot, @"src\PlayPair.Server\bin\Release\net8.0\PlayPair.Server.exe"),
            Path.Combine(baseDir, "PlayPair.Server.exe")
        };

        foreach (var candidate in serverCandidates)
        {
            if (File.Exists(candidate))
            {
                serverPath = candidate;
                break;
            }
        }

        // Find Client Path
        string clientPath = null;
        string[] clientCandidates = new[]
        {
            Path.Combine(repoRoot, @"src\PlayPair.Client.AppShell\bin\Debug\net8.0-windows10.0.19041.0\PlayPair.Client.AppShell.exe"),
            Path.Combine(repoRoot, @"src\PlayPair.Client.AppShell\bin\Release\net8.0-windows10.0.19041.0\PlayPair.Client.AppShell.exe"),
            Path.Combine(baseDir, "PlayPair.Client.AppShell.exe")
        };

        foreach (var candidate in clientCandidates)
        {
            if (File.Exists(candidate))
            {
                clientPath = candidate;
                break;
            }
        }

        // Start Server
        Process serverProcess = null;
        if (serverPath != null)
        {
            Console.WriteLine($"Starting Server Executable: {serverPath}");
            serverProcess = Process.Start(new ProcessStartInfo
            {
                FileName = serverPath,
                WorkingDirectory = Path.GetDirectoryName(serverPath),
                UseShellExecute = true
            });
        }
        else
        {
            string serverProj = Path.Combine(repoRoot, @"src\PlayPair.Server\PlayPair.Server.csproj");
            if (File.Exists(serverProj))
            {
                Console.WriteLine("Starting Server via 'dotnet run'...");
                serverProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --project \"{serverProj}\"",
                    WorkingDirectory = repoRoot,
                    UseShellExecute = true
                });
            }
            else
            {
                Console.WriteLine("Error: PlayPair.Server not found.");
            }
        }

        // Wait a brief moment for the server to spin up
        Thread.Sleep(2000);

        // Start Client
        Process clientProcess = null;
        if (clientPath != null)
        {
            Console.WriteLine($"Starting Client Executable: {clientPath}");
            var psi = new ProcessStartInfo
            {
                FileName = clientPath,
                WorkingDirectory = Path.GetDirectoryName(clientPath),
                UseShellExecute = true
            };
            psi.EnvironmentVariables["PLAYPAIR_SERVER_URL"] = "http://localhost:5000";
            clientProcess = Process.Start(psi);
        }
        else
        {
            string clientProj = Path.Combine(repoRoot, @"src\PlayPair.Client.AppShell\PlayPair.Client.AppShell.csproj");
            if (File.Exists(clientProj))
            {
                Console.WriteLine("Starting Client via 'dotnet run'...");
                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --project \"{clientProj}\"",
                    WorkingDirectory = repoRoot,
                    UseShellExecute = true
                };
                psi.EnvironmentVariables["PLAYPAIR_SERVER_URL"] = "http://localhost:5000";
                clientProcess = Process.Start(psi);
            }
            else
            {
                Console.WriteLine("Error: PlayPair.Client.AppShell not found.");
            }
        }

        Console.WriteLine("\nProcesses started. You can close this launcher window.");
        Thread.Sleep(3000);
    }
}
