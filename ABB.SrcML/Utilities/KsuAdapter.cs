/******************************************************************************
 * Copyright (c) 2010 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security;
using System.Security.Permissions;

namespace ABB.SrcML.Utilities
{
    /// <summary>
    /// Collection of functions for working with the Kent State SrcML executables
    /// </summary>
    public static class KsuAdapter
    {
        /// <summary>
        /// Combine the strings into a space seperated list that can be passed to a Process.
        /// </summary>
        /// <param name="arguments">the arguments to be combined</param>
        /// <returns>the arguments combined into a string, separated by spaces</returns>
        public static string MakeArgumentString(Collection<string> arguments)
        {
            return String.Join(" ", arguments.ToArray());
        }

        /// <summary>
        /// Get the string representation of the Language enumeration
        /// </summary>
        /// <param name="language">a Language value</param>
        /// <returns>The string representation of language</returns>
        public static string GetLanguage(Language language)
        {
            switch (language)
            {
                case Language.Any:
                    return "Any";
                case Language.CPlusPlus:
                    return "C++";
                case Language.C:
                    return "C";
                case Language.Java:
                    return "Java";
                case Language.AspectJ:
                    return "AspectJ";
                case Language.CSharp:
                    return "C#";
                default:
                    throw new SrcMLException(String.Format(CultureInfo.CurrentCulture, "This value needs to be added to the set of languages"));
            }
        }

        /// <summary>
        /// Converts an extension mapping dictionary to a string that can be passed to src2srcml.exe.
        /// If the extensions begin with a dot, these are stripped to conform with src2srcml.exe's input format.
        /// </summary>
        /// <param name="extensionMapping">An extension mapping dictionary</param>
        /// <returns>A comma separated list of mappings of the form ("EXT=LANG").</returns>
        public static string ConvertMappingToString(IDictionary<string, Language> extensionMapping) {
            var mapping = from kvp in extensionMapping
                          select String.Format(CultureInfo.InvariantCulture, "{0}={1}", kvp.Key.TrimStart(new[] {'.'}), KsuAdapter.GetLanguage(kvp.Value));
            var result = String.Join(",", mapping.ToArray());
            return result;
        }

        internal static string GetKsuExtension(string filePath)
        {
            var extension = Path.GetExtension(filePath);

            if (extension.Length < 1)
                return extension;
            return extension.Substring(1);
        }

        internal static string GetKsuExtension(FileInfo fileInfo)
        {
            var extension = fileInfo.Extension;

            if (extension.Length < 1)
                return extension;

            return extension.Substring(1);
        }

        [SecurityCritical]
        [SecurityPermission(SecurityAction.Demand, Unrestricted=true)]
        internal static void RunExecutable(string executableFileName, string arguments)
        {
            using(new ErrorModeContext(ErrorModes.FailCriticalErrors | ErrorModes.NoGpFaultErrorBox)) {
                using(Process p = new Process()) {
                    p.StartInfo.FileName = executableFileName;
                    p.StartInfo.Arguments = arguments;
                    p.StartInfo.CreateNoWindow = true;

                    p.StartInfo.UseShellExecute = false;

                    p.Start();

                    p.WaitForExit();
                    RaiseExceptionOnError(executableFileName, arguments, (ExecutableReturnValue) p.ExitCode);
                }
            }
        }
        
        [SecurityCritical]
        [SecurityPermission(SecurityAction.Demand, Unrestricted = true)]
        internal static string RunExecutable(string executableFileName, string arguments, string standardInput)
        {
            string output;
            
            using(new ErrorModeContext(ErrorModes.FailCriticalErrors | ErrorModes.NoGpFaultErrorBox)) {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = executableFileName;
                    p.StartInfo.Arguments = arguments;
                    p.StartInfo.CreateNoWindow = true;

                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardInput = true;
                    p.StartInfo.RedirectStandardOutput = true;

                    p.Start();

                    p.StandardInput.Write(standardInput);
                    p.StandardInput.Close();
                    output = p.StandardOutput.ReadToEnd();

                    p.WaitForExit();
                    RaiseExceptionOnError(executableFileName, arguments, (ExecutableReturnValue)p.ExitCode);
                }
            }
            return output;
        }

        internal static void RaiseExceptionOnError(string pathToExecutable, string arguments, ExecutableReturnValue value)
        {
            if (ExecutableReturnValue.Normal != value)
                throw new SrcMLRuntimeException(pathToExecutable, arguments, value);
        }

        internal static string GetErrorMessageFromReturnCode(ExecutableReturnValue value)
        {
            switch (value)
            {
                case ExecutableReturnValue.Normal:
                    return "Normal";
                case ExecutableReturnValue.Error:
                    return "Unspecified Error";
                case ExecutableReturnValue.ProblemWithInputFile:
                    return "Problem with input file";
                case ExecutableReturnValue.UnknownOption:
                    return "Unknown option";
                case ExecutableReturnValue.UnknownEncoding:
                    return "Unknown encoding";
                case ExecutableReturnValue.InvalidLanguage:
                    return "Invalid languageFilter";
                case ExecutableReturnValue.LanguageOptionSpecifiedButValueMissing:
                    return "Language option specified, but value missing";
                case ExecutableReturnValue.FilenameOptionSpecifiedButValueMissing:
                    return "Filename option specified, but value missing";
                case ExecutableReturnValue.DirectoryOptionSpecifiedButValueMissing:
                    return "Directory option specified, but value missing";
                case ExecutableReturnValue.VersionOptionSpecifiedButValueMissing:
                    return "Version option specified, but value missing";
                case ExecutableReturnValue.TextEncodingOptionSpecifiedButValueMissing:
                    return "Text encoding option specified, but value missing";
                case ExecutableReturnValue.XmlEncodingOptionSpecifiedButValueMissing:
                    return "XML encoding option specified, but value missing";
                case ExecutableReturnValue.UnitOptionSpecifiedButValueMissing:
                    return "Unit option specified, but value missing";
                case ExecutableReturnValue.UnitOptionValueIsNotValid:
                    return "Unit option value is not valid";
                case ExecutableReturnValue.InvalidCombinationOfOptions:
                    return "Invalid combination of options";
                case ExecutableReturnValue.IncompleteOutputDueToTermination:
                    return "Incomplete output due to termination";
                default:
                    return "Unknown error code. Please report.";
            }
        }
    }
}
