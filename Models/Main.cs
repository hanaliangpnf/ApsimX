﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using APSIM.Shared.JobRunning;
using APSIM.Shared.Utilities;
using CommandLine;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.ConfigFile;
using Models.Core.Run;
using Models.Storage;

namespace Models
{

    /// <summary>Class to hold a static main entry point.</summary>
    public class Program
    {
        private static object lockObject = new object();
        private static int exitCode = 0;
        private static List<Exception> exceptionsWrittenToConsole = new List<Exception>();

        /// <summary>
        /// Main program entry point.
        /// </summary>
        /// <param name="args"> Command line arguments</param>
        /// <returns> Program exit code (0 for success)</returns>
        public static int Main(string[] args)
        {
            bool isApplyOptionPresent = false;
            // Required to allow the --apply switch functionality of not including
            // an apsimx file path on the command line.
            if (args.Length > 0 && args[0].Equals("--apply"))
            {
                isApplyOptionPresent = true;
                string[] empty = { " " };
                empty = empty.Concat(args).ToArray();
                args = empty;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            ReplaceObsoleteArguments(ref args);

            Parser parser = new Parser(config =>
            {
                config.AutoHelp = true;
                config.HelpWriter = Console.Out;
            });

            // Shows the switch(es) used in the command line call.
            ParserResult<Options> result = parser.ParseArguments<Options>(args);

            if (isApplyOptionPresent)
            {
                if (args.Length > 2)
                {
                    result.Value.Apply = args[3];
                }
                else
                {
                    string argsListString = string.Join(" ", args.ToList());
                    throw new Exception($"No config file was given with the --apply switch. Arguments given: {argsListString}");
                }
            }

            result.WithParsed(Run).WithNotParsed(HandleParseError);
            return exitCode;
        }

        /// <summary>
        /// Handles parser errors to ensure that a non-zero exit code
        /// is returned when parse errors are encountered.
        /// </summary>
        /// <param name="errors">Parse errors.</param>
        private static void HandleParseError(IEnumerable<Error> errors)
        {
            if (!(errors.IsHelp() || errors.IsVersion()))
                exitCode = 1;
        }

        /// <summary>
        /// Run Models with the given set of options.
        /// </summary>
        /// <param name="options"></param>
        public static void Run(Options options)
        {
            try
            {
                string[] files = options.Files.SelectMany(f => DirectoryUtilities.FindFiles(f, options.Recursive)).ToArray();
                if (files == null || files.Length < 1 && string.IsNullOrEmpty(options.Apply))
                    throw new ArgumentException($"No files were specified");
                if (options.NumProcessors == 0)
                    throw new ArgumentException($"Number of processors cannot be 0");
                if (options.Upgrade)
                {
                    foreach (string file in files)
                    {
                        UpgradeFile(file);
                        if (options.Verbose)
                            Console.WriteLine("Successfully upgraded " + file);
                    }
                }
                else if (options.ListSimulationNames)
                    foreach (string file in files)
                        ListSimulationNames(file, options.SimulationNameRegex);
                else if (options.ListReferencedFileNames)
                {
                    foreach (string file in files)
                        ListReferencedFileNames(file);
                }
                else if (options.MergeDBFiles)
                {
                    string[] dbFiles = files.Select(f => Path.ChangeExtension(f, ".db")).ToArray();
                    string outFile = Path.Combine(Path.GetDirectoryName(dbFiles[0]), "merged.db");
                    DBMerger.MergeFiles(dbFiles, outFile);
                }
                // --apply switch functionality.
                else if (!string.IsNullOrWhiteSpace(options.Apply))
                {
                    List<string> commands = ConfigFile.GetConfigFileCommands(options.Apply);
                    bool isSimToBeRun = false;
                    string[] commandsArray = commands.ToArray();
                    string savePath = "";
                    string loadPath = "";

                    if (files.Length > 0)
                    {
                        foreach (string file in files)
                        {
                            for (int i = 0; i < commandsArray.Length; i++)
                            {
                                string[] splitCommand = commandsArray[i].Split(" ");
                                if (splitCommand[0] == "save")
                                {
                                    savePath = splitCommand[1];
                                }
                                else if (splitCommand[0] == "load")
                                {
                                    loadPath = splitCommand[1];
                                }
                                else if (splitCommand[0] == "run")
                                {
                                    isSimToBeRun = true;
                                }

                                // Required as RunConfigCommands() requires list, not just a string.
                                List<string> commandWrapper = new()
                                {
                                    commandsArray[i]
                                };

                                Simulations sim = ConfigFile.RunConfigCommands(file, commands) as Simulations;

                                sim.Write(file);

                                if (isSimToBeRun)
                                {
                                    Runner runner = new Runner(sim,
                                                                true,
                                                                true,
                                                                options.RunTests,
                                                                runType: options.RunType,
                                                                numberOfProcessors: options.NumProcessors,
                                                                simulationNamePatternMatch: options.SimulationNameRegex);
                                    RunSimulations(runner, options);
                                }
                            }
                        }
                    }
                    // If no apsimx file path included proceeding --apply switch...              
                    else if (files.Length < 1) // TODO: Create the 'Create' option functionality.
                    {
                        // Create a new simulation as an existing apsimx file was not included.
                        Simulations sims = CreateMinimalSimulation();

                        savePath = "";
                        loadPath = sims.FileName;

                        for (int i = 0; i < commandsArray.Length; i++)
                        {
                            string[] splitCommand = commandsArray[i].Split(" ");
                            if (splitCommand[0] == "save")
                            {
                                savePath = splitCommand[1];
                                continue;
                            }
                            else if (splitCommand[0] == "load")
                            {
                                loadPath = splitCommand[1];
                                continue;
                            }
                            else if (splitCommand[0] == "run")
                            {
                                isSimToBeRun = true;
                                continue;
                            }

                            // Throw if the first command is not a save or load command.
                            if (i == 0 && String.IsNullOrEmpty(loadPath) && String.IsNullOrEmpty(savePath))
                            {
                                throw new Exception("First command in a config file can only be either a save or load command if no apsimx file is included.");
                            }

                            // Required as RunConfigCommands() requires list, not just a string.
                            List<string> commandWrapper = new()
                            {
                                commandsArray[i]
                            };

                            // As long as a file can be loaded any other command can be run.
                            if (!String.IsNullOrEmpty(loadPath))
                            {
                                Simulations sim = ConfigFile.RunConfigCommands(loadPath, commandWrapper) as Simulations;

                                if (!String.IsNullOrEmpty(loadPath) && String.IsNullOrEmpty(savePath))
                                {
                                    sim.Write(loadPath);
                                }
                                else if (!String.IsNullOrEmpty(loadPath) && !String.IsNullOrEmpty(savePath))
                                {
                                    sim.Write(savePath);
                                }

                                if (isSimToBeRun)
                                {
                                    Runner runner = new Runner(sim,
                                                            true,
                                                            true,
                                                            options.RunTests,
                                                            runType: options.RunType,
                                                            numberOfProcessors: options.NumProcessors,
                                                            simulationNamePatternMatch: options.SimulationNameRegex);
                                    RunSimulations(runner, options);
                                }
                            }
                            else throw new Exception("--apply switch used without apsimx file and no load command. Include a load command in the config file.");
                        }
                    }
                }
                else
                {
                    Runner runner = null;
                    if (string.IsNullOrEmpty(options.EditFilePath))
                    {
                        runner = new Runner(files,
                                            options.RunTests,
                                            options.RunType,
                                            numberOfProcessors: options.NumProcessors,
                                            simulationNamePatternMatch: options.SimulationNameRegex);

                        RunSimulations(runner, options);

                    }
                    else if (!string.IsNullOrEmpty(options.EditFilePath))
                    {

                        runner = new Runner(files.Select(f => ApplyConfigToApsimFile(f, options.EditFilePath)),
                                            true,
                                            true,
                                            options.RunTests,
                                            runType: options.RunType,
                                            numberOfProcessors: options.NumProcessors,
                                            simulationNamePatternMatch: options.SimulationNameRegex);

                        RunSimulations(runner, options);
                    }

                    // If errors occurred, write them to the console.
                    if (exitCode != 0)
                        Console.WriteLine("ERRORS FOUND!!");
                    if (options.Verbose)
                        Console.WriteLine("Elapsed time was " + runner.ElapsedTime.TotalSeconds.ToString("F1") + " seconds");
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
                exitCode = 1;
            }
        }

        private static IModel ApplyConfigToApsimFile(string fileName, string configFilePath)
        {
            Simulations file = FileFormat.ReadFromFile<Simulations>(fileName, e => throw e, false).NewModel as Simulations;
            var overrides = Overrides.ParseStrings(File.ReadAllLines(configFilePath));
            Overrides.Apply(file, overrides);
            return file;
        }

        private static void ReplaceObsoleteArguments(ref string[] args)
        {
            if (args == null)
                return;
            List<KeyValuePair<string, string>> replacements = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("/Recurse", "--recursive"),
                new KeyValuePair<string, string>("/SingleThreaded", "--single-threaded"),
                new KeyValuePair<string, string>("/RunTests", "--run-tests"),
                new KeyValuePair<string, string>("/Csv", "--csv"),
                new KeyValuePair<string, string>("/Version", "--version"),
                new KeyValuePair<string, string>("/Verbose", "--verbose"),
                new KeyValuePair<string, string>("/Upgrade", "--upgrade"),
                new KeyValuePair<string, string>("/MultiProcess", "--multi-process"),
                new KeyValuePair<string, string>("/NumberOfProcessors:", "--cpu-count="),
                new KeyValuePair<string, string>("/SimulationNameRegexPattern:", "--simulation-names="),
                new KeyValuePair<string, string>("/MergeDBFiles", "--merge-db-files"),
                new KeyValuePair<string, string>("/Edit", "--edit"),
                new KeyValuePair<string, string>("/ListSimulations", "--list-simulations"),
                new KeyValuePair<string, string>("/?", "--help"),
            };
            for (int i = 0; i < args.Length; i++)
                foreach (KeyValuePair<string, string> replacement in replacements)
                    args[i] = args[i].Replace(replacement.Key, replacement.Value);
        }

        /// <summary>
        /// Write the APSIM version to the console.
        /// </summary>
        private static void WriteVersion()
        {
            Console.WriteLine(Simulations.GetApsimVersion());
        }

        /// <summary>
        /// Upgrade a file to the latest APSIM version.
        /// </summary>
        /// <param name="file">The name of the file to upgrade.</param>
        private static void UpgradeFile(string file)
        {
            string contents = File.ReadAllText(file);
            ConverterReturnType converter = Converter.DoConvert(contents, fileName: file);
            if (converter.DidConvert)
                File.WriteAllText(file, converter.Root.ToString());
        }

        private static void ListSimulationNames(string fileName, string simulationNameRegex)
        {
            Simulations file = FileFormat.ReadFromFile<Simulations>(fileName, e => throw e, false).NewModel as Simulations;

            SimulationGroup jobFinder = new SimulationGroup(file, simulationNamePatternMatch: simulationNameRegex);
            jobFinder.FindAllSimulationNames(file, null).ForEach(name => Console.WriteLine(name));

        }

        private static void ListReferencedFileNames(string fileName)
        {
            Simulations file = FileFormat.ReadFromFile<Simulations>(fileName, e => throw e, false).NewModel as Simulations;

            foreach (var referencedFileName in file.FindAllReferencedFiles())
                Console.WriteLine(referencedFileName);
        }

        /// <summary>Job has completed</summary>
        private static void OnJobCompleted(object sender, JobCompleteArguments e)
        {
            if (e.ExceptionThrowByJob != null)
            {
                lock (lockObject)
                {
                    exceptionsWrittenToConsole.Add(e.ExceptionThrowByJob);
                    Console.WriteLine("----------------------------------------------");
                    Console.WriteLine(e.ExceptionThrowByJob.ToString());
                    exitCode = 1;
                }
            }
        }

        /// <summary>All jobs for a file have completed</summary>
        private static void OnSimulationGroupCompleted(object sender, EventArgs e)
        {
            if (sender is SimulationGroup group)
            {
                string fileName = Path.ChangeExtension(group.FileName, ".db");
                var storage = new Storage.DataStore(fileName);
                Report.WriteAllTables(storage, fileName);
                Console.WriteLine("Successfully created csv file " + Path.ChangeExtension(fileName, ".csv"));
            }
        }

        /// <summary>All jobs have completed</summary>
        private static void OnAllJobsCompleted(object sender, Runner.AllJobsCompletedArgs e)
        {
            if (sender is Runner runner)
                (sender as Runner).DisposeStorage();

            if (e.AllExceptionsThrown == null)
                return;

            foreach (Exception error in e.AllExceptionsThrown)
            {
                if (!exceptionsWrittenToConsole.Contains(error))
                {
                    Console.WriteLine("----------------------------------------------");
                    Console.WriteLine(error.ToString());
                    exitCode = 1;
                }
            }
        }

        /// <summary>
        /// Write a complete message to the console.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">The event arguments of the completed job.</param>
        private static void WriteCompleteMessage(object sender, JobCompleteArguments e)
        {
            if (e.Job == null)
                return;

            var message = new StringBuilder(e.Job.Name);
            if (e.Job is SimulationDescription sim && !string.IsNullOrEmpty(sim.SimulationToRun?.FileName))
                message.Append($" ({sim.SimulationToRun.FileName})");
            string duration = e.ElapsedTime.TotalSeconds.ToString("F1");
            message.Append($" has finished. Elapsed time was {duration} seconds.");
            Console.WriteLine(message);
        }

        /// <summary>
        /// Runs a specified runner.
        /// </summary>
        /// <param cref="Runner" name="runner">The runner to be ran.</param>
        /// <param cref="Options" name="options">The command line switches/flags.</param>
        private static void RunSimulations(Runner runner, Options options)
        {
            runner.SimulationCompleted += OnJobCompleted;
            if (options.Verbose)
                runner.SimulationCompleted += WriteCompleteMessage;
            if (options.ExportToCsv)
                runner.SimulationGroupCompleted += OnSimulationGroupCompleted;
            runner.AllSimulationsCompleted += OnAllJobsCompleted;
            // Run simulations.
            runner.Run();
        }

        private static Simulations CreateMinimalSimulation() // TODO: needs testing.
        {
            try
            {
                Simulations sims = new Simulations()
                {
                    Children = new List<IModel>()
                    {
                        new DataStore()
                    }
                };

                sims.Write("NewSimulation");
                return sims;
            }
            catch (Exception ex)
            {
                throw new Exception("An error occured when trying to create a new minimal simulation." + ex.Message);
            }

        }

    }
}