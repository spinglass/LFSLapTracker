using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LFSLapTracker
{
    class Program
    {
        static void Main(string[] args)
        {
            CommandLine commandLine = new CommandLine(args);
            Settings settings = new Settings(commandLine);

            bool run = true;

            if (!string.IsNullOrEmpty(settings.LFS))
            {
                // Create process to launch LFS
                Process lfsProcess = new Process();
                lfsProcess.EnableRaisingEvents = true;
                lfsProcess.Exited += OnLFSExited;
                lfsProcess.StartInfo.CreateNoWindow = true;
                lfsProcess.StartInfo.UseShellExecute = false;
                lfsProcess.StartInfo.FileName = settings.LFS;
                lfsProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(lfsProcess.StartInfo.FileName);
                lfsProcess.StartInfo.Arguments = "/insim=" + settings.Port; // Command line to allow us to connect

                Console.WriteLine("Launching LFS ({0})", settings.LFS);
                try
                {
                    run = lfsProcess.Start();
                }
                catch
                {
                    run = false;
                }
                if (!run)
                {
                    Console.WriteLine("Failed to launch LFS");
                }
            }

            if (run)
            {
                m_InSimConnection = new InSimConnection(settings);
                m_InSimConnection.Run();
            }
        }

        static void OnLFSExited(object sender, EventArgs e)
        {
            Console.WriteLine("LFS exited");
            if (m_InSimConnection != null)
            {
                m_InSimConnection.Quit();
            }
        }

        private static InSimConnection m_InSimConnection;
    }
}
