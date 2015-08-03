using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Management.Automation;

namespace InceptionPowerShell
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No arguments were supplied");
            }
            else
            {
                //process args
                foreach (var arg in args)
                {
                    //run these in order of importance
                    if (arg.ToLower().Contains("signscripts"))
                    {
                        // The InvokePSScriptSigning script should be signed already, so this allow us to pop up a process and run it as a ps1 file
                        // This only works an administrator context
                        // The PowerShell API doesn't allow you to run a script (somefile.ps1) very easily.
                        PowerShell.Create().AddScript(@"Set-ExecutionPolicy -Scope Process -ExecutionPolicy RemoteSigned").Invoke();

                        // This is assuming we're running this from debug or release
                        var workingDir = System.IO.Path.GetFullPath(@"..\..\BuildTools");

                        Process process = new Process()
                        {
                            StartInfo = new ProcessStartInfo()
                            {
                                FileName = @"powershell.exe",
                                Arguments = @"& '.\Invoke-PSScriptSigning.ps1';Read-Host 'Press any button to continue...';",
                                WorkingDirectory = workingDir
                            }
                        };

                        process.Start();

                        //block execution until all scripts are signed
                        bool stillRunning = true;
                        while (stillRunning)
                        {
                            try
                            {
                                Process.GetProcessById(process.Id);
                                System.Threading.Thread.Sleep(5000);
                            }
                            catch
                            {
                                stillRunning = false;
                            }
                        }
                    }

                    if (arg.ToLower().Contains("runprovisioningscript"))
                    {
                        // This is assuming we're running this from debug or release
                        var workingDir = System.IO.Path.GetFullPath(@"..\..\ProvisionScript");

                        Process process = new Process()
                        {
                            StartInfo = new ProcessStartInfo()
                            {
                                FileName = @"powershell.exe",
                                Arguments = @"& '.\Invoke-AzureInceptionProvision.ps1';Read-Host 'Press any button to continue...';",
                                WorkingDirectory = workingDir
                            }
                        };
                        
                        process.Start();
                    }
                }                
            }

            Console.WriteLine("Press any button to continue...");
            Console.ReadKey();
        }
    }
}