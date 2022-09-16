using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace ClientProc {
    public class ClientPipe {
        private static int numClients = Utilities.Constants.NumberOfClients;

        public static void StartClient (string[] args) {
            if (args.Length > 0) {
                if (args[0] == Utilities.Constants.ClientName) {
                    var pipeClient =
                        new NamedPipeClientStream (".", Utilities.Constants.PipeName,
                            PipeDirection.InOut, PipeOptions.None,
                            TokenImpersonationLevel.Impersonation);

                    Console.WriteLine ("Connecting to server...\n");
                    pipeClient.Connect ();

                    var ss = new StreamString (pipeClient);
                    // Validate the server's signature string.
                    if (ss.ReadString () == "I am the one true server!") {
                        // The client security token is sent with the first write.
                        // Send the name of the file whose contents are returned
                        // by the server.
                        ss.WriteString (Utilities.Constants.FilePathAndNameToRead);

                        // Print the file to the screen.
                        Console.Write (ss.ReadString ());
                    } else {
                        Console.WriteLine ("Server could not be verified.");
                    }
                    pipeClient.Close ();
                    // Give the client process some time to display results before exiting.
                    Thread.Sleep (4000);
                }
            } else {
                Console.WriteLine ("\n*** Named pipe client stream with impersonation example ***\n");
                StartClients ();
            }
        }

        static void Main (string[] args) {
            Console.WriteLine ("Starting the Client Process!");
            ClientPipe.StartClient (args);
        }

        // Helper function to create pipe client processes
        private static void StartClients () {
            string currentProcessName = Environment.CommandLine;

            // Remove extra characters when launched from Visual Studio
            currentProcessName = currentProcessName.Trim ('"', ' ');

            currentProcessName = Path.ChangeExtension (currentProcessName, ".exe");
            Process[] plist = new Process[numClients];

            Console.WriteLine ("Spawning client processes...\n");

            if (currentProcessName.Contains (Environment.CurrentDirectory)) {
                currentProcessName = currentProcessName.Replace (Environment.CurrentDirectory, String.Empty);
            }

            // Remove extra characters when launched from Visual Studio
            currentProcessName = currentProcessName.Replace ("\\", String.Empty);
            currentProcessName = currentProcessName.Replace ("\"", String.Empty);

            int i;
            for (i = 0; i < numClients; i++) {
                // Start 'this' program but spawn a named pipe client.
                plist[i] = Process.Start (currentProcessName, Utilities.Constants.ClientName);
            }
            
            while (i > 0) {
                for (int j = 0; j < numClients; j++) {
                    if (plist[j] != null) {
                        if (plist[j].HasExited) {
                            Console.WriteLine ($"Client process[{plist[j].Id}] has exited.");
                            plist[j] = null;
                            i--; // decrement the process watch count
                        } else {
                            Thread.Sleep (250);
                        }
                    }
                }
            }
            Console.WriteLine ("\nClient processes finished, exiting.");
        }
    }
}