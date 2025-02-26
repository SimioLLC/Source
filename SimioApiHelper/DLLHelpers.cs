﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using LoggertonHelpers;

namespace SimioApiHelper
{
    public static class DLLHelpers
    {
        /// <summary>
        /// Get the AssemblyName object and create a string from it.
        /// </summary>
        /// <param name="filepathForDll"></param>
        /// <returns></returns>
        public static string GetDllInfo(string filepathForDll)
        {
            try
            {
                AssemblyName aName = AssemblyName.GetAssemblyName(filepathForDll);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"FullName={aName.FullName}");
                sb.AppendLine($"Name={aName.Name}");
                sb.AppendLine($"ProcessorArchitecture={aName.ProcessorArchitecture}");
                sb.AppendLine($"Version={aName.Version}");
                sb.AppendLine($"VersionCompatiblity={aName.CodeBase}");
                sb.AppendLine($"Flags={aName.Flags}");

                return sb.ToString();

            }
            catch (Exception ex)
            {
                throw new ApplicationException($"File={filepathForDll} Err={ex}");
            }
        }

        /// <summary>
        /// Round up the usual suspects as folders for Simio DLLs,
        /// and return them as a list of paths to those folders.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetSimioExtensionsLocations()
        {
            string marker = "Begin.";
            try
            {
                List<string> locations = new List<string>();

                // User extensions
                {
                    string myDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string fullPath = Path.Combine(myDocs, "SimioUserExtensions");
                    marker = $"UserExtensions={fullPath}";

                    if (Directory.Exists(fullPath))
                        locations.Add(fullPath);
                    else
                        LogIt($"Info: Cannot find Path={fullPath} (there may not be any personal UserExtensions)");

                }


                // public extensions
                {
                    string simioDesktop = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    string installPath = Path.Combine(simioDesktop, "Simio LLC", "Simio");


                    marker = $"Public Desktop Extensions={installPath}";

                    if (Directory.Exists(installPath) == false)
                    {
                        LogIt($"Cannot find Path={installPath}. Check if Simio is installed.");
                    }
                    else
                    {
                        string userExtensionsPath = Path.Combine(installPath, "UserExtensions");
                        if (Directory.Exists(userExtensionsPath))
                            locations.Add(userExtensionsPath);
                        else
                            LogIt($"Cannot find Path={userExtensionsPath}");
                    }
                }

                // Simio installation (Program Files (x86)) - This options is deprecated as of version 14.229. See Program Files instead
                {
                    string simioProgramsx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                    string installPath = Path.Combine(simioProgramsx86, "Simio" );
                    marker = $"Public Desktop (x86) Install={installPath}";

                    if ( Directory.Exists(installPath) == false)
                    {
                        LogIt($"Cannot find Path={installPath}. Check if Simio is installed.");
                    }
                    else
                    {
                        string userExtensionsPath = Path.Combine(installPath, "UserExtensions");
                        if (Directory.Exists(userExtensionsPath))
                            locations.Add(userExtensionsPath);
                        else
                            LogIt($"Cannot find Path={userExtensionsPath}");
                    }

                }

                // Simio installation (Program Files)
                ////{
                ////    string simioPrograms = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                ////    string fullPath = Path.Combine(simioPrograms, "Simio LLC", "Simio");
                ////    marker = $"ProgramFiles Install={fullPath}";

                ////    if ( Directory.Exists(fullPath) && !locations.Contains(fullPath))
                ////    {
                ////        locations.Add(fullPath);
                ////        // Simio UserExtensions underneath Program Files
                ////        fullPath = SimEngineLibrary.SimEngineHelpers.BuildSimioDesktopExtensionsPath(fullPath, false);
                ////        locations.Add(fullPath);
                ////    }
                ////}

                return locations;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Marker={marker} Err={ex.Message}");
            }
        }

        /// <summary>
        /// Return a list of DLL files. The regex exclude filter (pipe delimited) is used to exclude files.
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="regexExcludeFilter"></param>
        /// <returns></returns>
        public static List<string> GetDllFiles(string folderPath, string regexExcludePatterns)
        {
            try
            {
                List<string> filteredFiles = new List<string>();

                if (!Directory.Exists(folderPath))
                {
                    LogIt($"Info: No such folder={folderPath}");
                    return filteredFiles;
                }

                string[] files = Directory.GetFiles(folderPath, "*.DLL", SearchOption.AllDirectories);

                List<string> regexTokens = regexExcludePatterns.Split('|').ToList();
                foreach ( string file in files)
                {
                    foreach ( string pattern in regexTokens)
                    {
                        if (Regex.IsMatch(file, pattern.Trim(), RegexOptions.IgnoreCase))
                            goto GetNextFile;
                    }

                    filteredFiles.Add(file);

                GetNextFile:;
                }

                return filteredFiles;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Folder={folderPath} Err={ex}");
            }
        }

        /// <summary>
        /// Extension to fetch loadable types.
        /// Ref:https://stackoverflow.com/questions/7889228/how-to-prevent-reflectiontypeloadexception-when-calling-assembly-gettypes
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        private static bool IsIgnoredAssembly(string name)
        {
            string[] IgnoredPrefixes = {
                "System",
                "mscorlib",
                "PresentationCore",
                "PresentationFramework",
                "ReachFramework",
                "UIAutomationProvider",
                "UIAutomationTypes",
                "UIAutomationFramework",
                "WindowsBase"
            };

            bool ignore = IgnoredPrefixes.Any(rr => name.ToLower().StartsWith(rr.ToLower()));
            return ignore;

        }

        /// <summary>
        /// Load the assembly of the selected DLL file, and
        /// also discover the chain of DLL references (recursively discovered)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static bool GetDependencies(AssemblyReference assemblyRef, Dictionary<string, AssemblyReference> dependentAssemblyDict, 
            Stack<AssemblyReference> stack, out string explanation)
        {
            const int MAX_DEPENDENCIES = 550;

            string marker = "begin";
            explanation = "";
            stack.Push(assemblyRef);

            if ( assemblyRef.Name.StartsWith("System.Data.SqlXml"))
            {
//                string xx = "";
            }

            if ( stack.Count > MAX_DEPENDENCIES )
            {
                LogIt($"Dependencies are too large (Count={stack.Count} Max={MAX_DEPENDENCIES}. Exiting...");
                return false;
            }

            try
            {
                StringBuilder sb = new StringBuilder();
                marker = $"Loading Assembly={assemblyRef}";

                Assembly myAssembly = null;
                if (assemblyRef.AssemblyPath != null)
                {
                    myAssembly = Assembly.LoadFrom(assemblyRef.AssemblyPath);
                }
                else
                {
                    myAssembly = Assembly.Load(assemblyRef.AssemblyName);
                    if (!string.IsNullOrEmpty(myAssembly.Location))
                        assemblyRef.AssemblyPath = myAssembly.Location;

                }

                if (myAssembly == null)
                    return true;

                marker = "Getting Referenced Assemblies";
                try
                {
                    
                    List<AssemblyName> aNameList = myAssembly.GetReferencedAssemblies().ToList();

                    string MyName = assemblyRef.Name;
                    if ( !IsIgnoredAssembly(MyName) )
                    {
                        foreach (AssemblyName aName in aNameList)
                        {
                            if (aName == assemblyRef.AssemblyName)
                            {
                                //string xx = ""; // looking for recursion
                            }

                            if (!IsIgnoredAssembly(aName.Name))
                            {
                                // See if it is already there
                                if (!dependentAssemblyDict.TryGetValue(aName.FullName, out AssemblyReference aRef))
                                {
                                    aRef = new AssemblyReference(aName, assemblyRef.Level);
                                    dependentAssemblyDict.Add(aRef.Key, aRef);
                                }

                                if (aRef != null)
                                {
                                    aRef.AddReferencedBy(assemblyRef);
                                    if (!GetDependencies(aRef, dependentAssemblyDict, stack, out explanation))
                                        return false;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    explanation = $"Marker={marker} Err={ex.Message}";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                explanation = $"Marker={marker} Err={ex.Message}";
                return false;
            }
        }

        private static void LogIt(string message)
        {
            if (message.ToLower().StartsWith("info:"))
                Loggerton.Instance.LogIt(EnumLogFlags.Information, message);
            else
                Loggerton.Instance.LogIt(EnumLogFlags.Error, message);
        }


    }

    public class AssemblyReference
    {
        
        /// <summary>
        /// Unique key (fullname from AssemblyName??)
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Level of dependency
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Where the assembly lives (could be null)
        /// </summary>
        public string AssemblyPath { get; set; } 

        public AssemblyName AssemblyName { get; set; }

        /// <summary>
        /// Short name from AssemblyName
        /// </summary>
        public string Name => AssemblyName.Name;

        /// <summary>
        /// Full name from AssemblyName. Includes version and token
        /// </summary>
        public string FullName => AssemblyName.FullName;

        /// <summary>
        /// A list of assemblies that reference this assembly
        /// </summary>
        public List<AssemblyReference> ReferencedBy { get; private set; }

        public AssemblyReference( AssemblyName aName, int callingLevel)
        {
            if (aName == null)
                throw new ApplicationException($"AssemblyName cannot be null.");

            AssemblyName = aName;

            AssemblyPath = aName.CodeBase;

            Key = FullName;
            Level = callingLevel+1;

            ReferencedBy = new List<AssemblyReference>();

        }

        /// <summary>
        /// Add a referenced-by entry, ignoring duplicates.
        /// </summary>
        /// <param name="path"></param>
        public void AddReferencedBy(AssemblyReference aRef)
        {
            if (ReferencedBy.Contains(aRef))
                return;

            ReferencedBy.Add(aRef);
        }

        public override string ToString()
        {
            if (Level > 100)
                return $"Level too deep={Level}";


            if (AssemblyPath != null)
                return $"Name={Name} Level={Level} FullName={AssemblyName.FullName} Path={AssemblyPath}";
            else
            {
                if ( Name == "System" )
                {
                    //string xx = "";
                }
                return $"Level={Level} FullName={AssemblyName.FullName} *No Path*";
            }
        }
    }
}
