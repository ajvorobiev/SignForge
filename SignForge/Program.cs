using System.Reflection;
using System.Threading;

namespace SignForge
{
    using System.Diagnostics;
    using System.IO;
    using System;
    using System.Linq;

    class Program
    {
        /// <summary>
        /// The main entry point to the application
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <remarks>The argument signature is as follows: SignForge [help] -s "source pbo files" [-a "authority name"] [-d "destination folder"] [-v] [-r]</remarks>
        static void Main(string[] args)
        {
            // Create keys for every pbo file in the source

            // help - (OPTIONAL) display the help information
            if (args.Contains("help"))
            {
                const string help = "Create keys for every pbo file in the source\r\n" +
                                    "\r\n" +
                                    "SignForge.exe [help] -s \"source pbo files\" [-a \"authority name\"] [-d \"destination folder\"] [-v] [-r]\r\n" +
                                    "\r\n" +
                                    "Arguments:\r\n" +
                                    "\r\n" +
                                    "help - (OPTIONAL) display the help information\r\n" +
                                    "-r - (OPTIONAL) remove all old .bisign files from source folder\r\n" +
                                    "-o - (OPTIONAL) override .bikey and .biprivatekey if they exist in the destination folder\r\n" +
                                    "-v - (OPTIONAL) verify all files\r\n" +
                                    "-s *source* - folder where the pbos that need to be signed reside\r\n" +
                                    "-a *name* - (OPTIONAL) authority name. If argument not provided then prompt\r\n" +
                                    "-d *destination*- (OPTIONAL) folder to put private and public keys into";

                Console.Write(help);
                WaitForExit();
                return;
            }

#if WINE
            typeof(Path).GetField("DirectorySeparatorChar").SetValue(null, '/');  
            typeof(Path).GetField("AltDirectorySeparatorChar").SetValue(null, '\\');
            typeof(Path).GetField("VolumeSeparatorChar").SetValue(null, '/');
#endif

            try
            {
                // -r - (OPTIONAL) remove all old .bisign files from source folder
                var remove = args.Contains("-r");
                Console.WriteLine("Remove flag set to: {0}", remove);

                // -v - (OPTIONAL) verify all files
                var verify = args.Contains("-v");
                Console.WriteLine("Verify flag set to: {0}", verify);

                // -o - (OPTIONAL) override keys
                var over = args.Contains("-o");
                Console.WriteLine("Override flag set to: {0}", over);

                // -s - folder where the pbos that need to be signed reside
                string sourcePath;

                // verify source folder
                VerifySourcePath(args, out sourcePath);
                Console.WriteLine("Source path set to: {0}", sourcePath);

                // -a - (OPTIONAL) authority name. If argument not provided then prompt
                string authorityName;

                if (!args.Contains("-a"))
                {
                    Console.WriteLine("Provide an authority name and press [Enter]");
                    authorityName = Console.ReadLine();
                }
                else
                {
                    authorityName = args[args.ToList().FindIndex(x => x == "-a") + 1];
                }

                if (string.IsNullOrWhiteSpace(authorityName))
                    throw new ApplicationException("Authority name cannot be empty.");

                Console.WriteLine("Authority set to: {0}", authorityName);

                // -d - (OPTIONAL) folder to put private and public keys into
                string destinationPath = !args.Contains("-d") ? sourcePath : args[args.ToList().FindIndex(x => x == "-d") + 1];

                Console.WriteLine("Source path set to: {0}", destinationPath);

                // verify folders exists
                if (!Directory.Exists(sourcePath))
                    throw new ApplicationException("Provided source directory is invalid.");

                if (!Directory.Exists(destinationPath))
                    throw new ApplicationException("Provided destination directory is invalid.");

                string privateKey;

                // create the signatures
                CreateSignatureFiles(authorityName, destinationPath, over, out privateKey);

                // clean up if the flag is set
                RemoveOldSignatures(remove, sourcePath);

                // sign files
                SignPbos(sourcePath, privateKey);

                // verify keys
                if (verify)
                {
#if WINE
                    Thread.Sleep(2000);
#endif
                    VerifyKeys(sourcePath, destinationPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Application Finished");
        }

        /// <summary>
        /// Verifies the keys.
        /// </summary>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="destinationPath">The destination path.</param>
        /// <exception cref="System.ApplicationException"></exception>
        private static void VerifyKeys(string sourcePath, string destinationPath)
        {
            Console.WriteLine("Start file verification");

            var startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
#if WINE
            startInfo.FileName = "/usr/bin/wine";
            startInfo.Arguments = string.Format("{2}\\DSCheckSignatures.exe \"{0}\" \"{1}\"", sourcePath, destinationPath, Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
#else
            startInfo.FileName = "DSCheckSignatures.exe";
            startInfo.Arguments = string.Format("\"{0}\" \"{1}\"", sourcePath, destinationPath);
#endif

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    if (exeProcess != null) exeProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format("Creation of bisign file aborted: {0}", ex.Message));
            }

            Console.WriteLine("Done file verification");
        }

        /// <summary>
        /// Signs the pbos.
        /// </summary>
        /// <param name="sourcePath">The source path.</param>
        /// <param name="privateKey">The private key.</param>
        /// <exception cref="System.ApplicationException"></exception>
        private static void SignPbos(string sourcePath, string privateKey)
        {
            var sourcePboFiles = Directory.GetFiles(sourcePath, "*.pbo");

            foreach (var sourcePboFile in sourcePboFiles)
            {
                Console.WriteLine(sourcePboFile);

                // create signature   
                var startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
#if WINE
                startInfo.FileName = "/usr/bin/wine";
                startInfo.Arguments = string.Format("{2}\\DSSignFile.exe \"{0}\" \"{1}\"", privateKey, sourcePboFile, Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
#else
                startInfo.FileName = "DSSignFile.exe";
                startInfo.Arguments = string.Format("\"{0}\" \"{1}\"", privateKey, sourcePboFile);
#endif

                try
                {
                    // Start the process with the info we specified.
                    // Call WaitForExit and then the using statement will close.
                    using (Process exeProcess = Process.Start(startInfo))
                    {
                        if (exeProcess != null) exeProcess.WaitForExit();
#if WINE
                        Thread.Sleep(2000);
#endif
                    }
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(string.Format("Creation of bisign file aborted: {0}", ex.Message));
                }
            }

            Console.WriteLine("Finished signing files!");
        }

        /// <summary>
        /// Removes the old signatures.
        /// </summary>
        /// <param name="remove">if set to <c>true</c> [remove].</param>
        /// <param name="sourcePath">The source path.</param>
        /// <exception cref="System.ApplicationException"></exception>
        private static void RemoveOldSignatures(bool remove, string sourcePath)
        {
            if (remove)
            {
                var sourceBisignFiles = Directory.GetFiles(sourcePath, "*.bisign");

                try
                {
                    foreach (var sourceBisignFile in sourceBisignFiles)
                    {
                        File.Delete(sourceBisignFile);
                    }
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(string.Format("Clean up failed: {0}", ex.Message));
                }
            }
        }

        /// <summary>
        /// Creates the signature files.
        /// </summary>
        /// <param name="authorityName">Name of the authority.</param>
        /// <param name="destinationPath">The destination path.</param>
        /// <param name="over">Override the files if keys for same authority exist</param>
        /// <param name="destPrivateFileName">The private key file location</param>
        /// <exception cref="System.ApplicationException"></exception>
        private static void CreateSignatureFiles(string authorityName, string destinationPath, bool over,out string destPrivateFileName)
        {
            var pubKeyFilename = string.Format("{0}.bikey", authorityName);
            var prvKeyFilename = string.Format("{0}.biprivatekey", authorityName);
#if WINE
            Thread.Sleep(2000); // give time for the files to appear
            var publicKey = string.Format("{1}/{0}", pubKeyFilename, AssemblyDirectory);
            var privateKey = string.Format("{1}/{0}", prvKeyFilename, AssemblyDirectory);
            var destPublicFileName = string.Format("{0}/{1}", destinationPath, pubKeyFilename);
            destPrivateFileName = privateKey;

#else
            var publicKey = string.Format("{0}.bikey", authorityName);
            var privateKey = string.Format("{0}.biprivatekey", authorityName);
            var destPublicFileName = string.Format("{0}\\{1}", destinationPath, publicKey);
            destPrivateFileName = privateKey;
#endif

            // if override is not set and keys all exist
            if (!over && File.Exists(privateKey) && File.Exists(destPublicFileName))
            {
                Console.WriteLine("Override not set and all files were found. Skipping creation of new keys...");
                return;
            }

            // if no override flag set but keys dont exixt then make them
            if (!over && (!File.Exists(privateKey) || !File.Exists(destPublicFileName)))
            {
                Console.WriteLine("Override was set but either one or all of the keys were not found. Proceeding to generate new ones...");
            }

            // if override set make keys anyway
            if (over)
            {
                Console.WriteLine("Override enabled. Proceeding to geenrate new keys...");
            }

            // create signature   
            var startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
#if WINE
            startInfo.FileName = "/usr/bin/wine";
            startInfo.Arguments = string.Format("{1}\\DSCreateKey.exe {0}", authorityName, Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
#else
            startInfo.FileName = "DSCreateKey.exe";
            startInfo.Arguments = authorityName;
#endif

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    if (exeProcess != null) exeProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format("Creation of key files aborted: {0}", ex.Message));
            }

            


            if (File.Exists(publicKey))
            {
                if(File.Exists(destPublicFileName))
                    File.Delete(destPublicFileName);

                File.Move(publicKey, destPublicFileName);
            }
            else
            {
                throw new ApplicationException("Cannot find the generated public key file: " + publicKey);
            }

            if (!File.Exists(privateKey))
            {
               throw new ApplicationException("Cannot find the generated private key file: " + privateKey);
            }

        }

        /// <summary>
        /// Verifies the source path.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="sourcePath">The source path.</param>
        /// <exception cref="System.ApplicationException">No source folder is provided. Quiting.</exception>
        private static void VerifySourcePath(string[] args, out string sourcePath)
        {
            if (!args.Contains("-s"))
            {
                throw new ApplicationException("No source folder is provided. Quiting.");
            }

            sourcePath = args[args.ToList().FindIndex(x => x == "-s") + 1];
        }

        /// <summary>
        /// Waits for exit.
        /// </summary>
        private static void WaitForExit()
        {
            Console.ReadKey();
        }

        /// <summary>
        /// Gets the assembly directory in unix friendly format.
        /// </summary>
        /// <value>
        /// The assembly directory.
        /// </value>
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path).Replace("Z:","");
            }
        }
    }
}
