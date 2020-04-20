// -----------------------------------------------------------------------
// <copyright file="SetProjectGuid.cs" company="Ace Olszowka">
//  Copyright (c) Ace Olszowka 2018-2020. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MsBuildSetProjectGuid
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    static class SetProjectGuid
    {
        internal static void Execute(string targetDirectory, string targetProjectPath, string targetGuid)
        {
            // Load up the project into an XDocument
            XDocument targetProjXml = XDocument.Load(targetProjectPath);

            // First get the existing Guid of the project
            string existingProjectGuid = MSBuildUtilities.GetMSBuildProjectGuid(targetProjXml, targetProjectPath);

            // Now update the project Guid
            MSBuildUtilities.SetMSBuildProjectGuid(targetProjXml, targetGuid);

            // Save this file
            targetProjXml.Save(targetProjectPath);

            IEnumerable<string> projectsInDirectory = GetProjectsInDirectory(targetDirectory);

            // First Update All Projects
            Parallel.ForEach(projectsInDirectory, projectInDirectory =>
            {
                UpdateProjectReferenceGuids(projectInDirectory, targetProjectPath, existingProjectGuid, targetGuid);
            });

            IEnumerable<string> solutionsInTargetDirectory = GetSolutionsInDirectory(targetDirectory);

            // Now update Solution Files
            Parallel.ForEach(solutionsInTargetDirectory, solutionFile =>
            {
                UpdateSolutionReferenceGuids(solutionFile, targetProjectPath, existingProjectGuid, targetGuid);
            }
            );
        }

        internal static IEnumerable<string> GetSolutionsInDirectory(string targetDirectory)
        {
            return Directory.EnumerateFiles(targetDirectory, "*.sln", SearchOption.AllDirectories);
        }

        /// <summary>
        /// Gets all Project Files that are understood by this
        /// tool from the given directory and all subdirectories.
        /// </summary>
        /// <param name="targetDirectory">The directory to scan for projects.</param>
        /// <returns>All projects that this tool supports.</returns>
        internal static IEnumerable<string> GetProjectsInDirectory(string targetDirectory)
        {
            HashSet<string> supportedFileExtensions =
                new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
                {
                    ".csproj",
                    ".synproj",
                    ".vbproj",
                };

            return
                Directory
                .EnumerateFiles(targetDirectory, "*proj", SearchOption.AllDirectories)
                .Where(currentFile => supportedFileExtensions.Contains(Path.GetExtension(currentFile)));
        }

        internal static void UpdateSolutionReferenceGuids(string solutionPath, string targetProjectPath, string oldGuid, string newGuid)
        {
            // WARNING, WARNING, WARNING: You may try to be smart and just
            // do a simple find and replace of the old Guid with the new
            // Guid. Unfortunately the most likely scenario for using this
            // tool is someone did a stupid and duplicated the Guids.
            // Therefore blindly doing a find a replace on the Old Guid
            // to the new Guid will put you right back in the same boat.

            // HOWEVER we can use this to advantage, because you can never
            // have two projects with identical GUIDS in a solution we can
            // safely assume that once we've identified the Solution as
            // containing our project we CAN perform a find a replace.

            // This is going to be tricky, because we do not have an API to
            // interact with solution files; we're going to take advantage of
            // the fact that the solution file must have the relative path
            // to the project in it to perform the update of the GUID.

            // Also because of this we're going to need to know what the old
            // guid was, if some kind soul would provide an API then we could
            // greatly simplify this code.

            // First set the Old and New Guid to just the Guid (no braces)
            // All Guids should be capital
            string cleanOldGuid = Guid.Parse(oldGuid).ToString("D").ToUpperInvariant();
            string cleanNewGuid = Guid.Parse(newGuid).ToString("D").ToUpperInvariant();

            if (ShouldUpdateSolution(solutionPath, targetProjectPath, oldGuid))
            {
                string solutionContent = File.ReadAllText(solutionPath);

                // Again because the identical Guids cannot exist in the same
                // solution, we can safely perform the find and replace blindly
                // at this point because we've identified this solution as
                // containing our project.
                string solutionContentModified = Regex.Replace(solutionContent, cleanOldGuid, cleanNewGuid, RegexOptions.IgnoreCase);

                Encoding solutionFileEncoding = Encoding.ASCII;

                // See if the original file was UTF-8 BOM
                if (FileUtilities.ContainsUTF8BOM(solutionPath))
                {
                    solutionFileEncoding = Encoding.UTF8;
                }

                File.WriteAllText(solutionPath, solutionContentModified, solutionFileEncoding);
            }

        }


        /// <summary>
        /// Determines if we should update this solution file.
        /// </summary>
        /// <param name="solutionPath">The solution file to evaluate.</param>
        /// <param name="targetProjectPath">The path of the project being fixed.</param>
        /// <param name="oldGuid">The old invalid guid.</param>
        /// <returns></returns>
        internal static bool ShouldUpdateSolution(string solutionPath, string targetProjectPath, string oldGuid)
        {
            bool shouldUpdateSolution = false;

            // All Compares should be lower case
            string solutionContent = File.ReadAllText(solutionPath).ToLowerInvariant();
            string cleanOldGuid = Guid.Parse(oldGuid).ToString("D").ToLowerInvariant();

            // Again be careful here, you cannot just blindly check for the
            // old Guid because of the possibility of it being duplicated in
            // another project. Instead we want to:

            // First see if the Solution Even Contains the Guid
            if (solutionContent.Contains(cleanOldGuid))
            {
                // If we got this far now bother with calculating the relative
                // path to the project that is in question, the solution must
                // contain a relative path to any projects its including.
                string relativePathToProject = PathUtilities.GetRelativePath(solutionPath, targetProjectPath).ToLowerInvariant();

                shouldUpdateSolution = solutionContent.Contains(relativePathToProject);
            }

            return shouldUpdateSolution;
        }

        internal static void UpdateProjectReferenceGuids(string pathToProjectToUpdate, string pathToProjectWithNewGuid, string oldGuid, string newGuid)
        {
            XDocument projXml = XDocument.Load(pathToProjectToUpdate);

            // Format the old guid into the expected project format
            string oldGuidClean = Guid.Parse(oldGuid).ToString("B");

            // This will filter to only project references that are really the
            // target project this is to work around cases where someone has
            // duplicated the Guid.
            XElement[] projectReferences =
                MSBuildUtilities.GetProjectReferenceNodes(projXml)
                .Where(projectReference => MSBuildUtilities.GetProjectReferenceGUID(projectReference, pathToProjectToUpdate).Equals(oldGuidClean, StringComparison.InvariantCultureIgnoreCase))
                .Where(projectReference =>
                {
                    string directoryOfProject = PathUtilities.AddTrailingSlash(Path.GetDirectoryName(pathToProjectToUpdate));
                    string prIncludeRelative = MSBuildUtilities.GetProjectReferenceIncludeValue(projectReference, pathToProjectToUpdate);
                    string prIncludeExpanded = PathUtilities.ResolveRelativePath(directoryOfProject, prIncludeRelative);
                    return prIncludeExpanded.Equals(pathToProjectWithNewGuid);
                })
                .ToArray();

            // NOTE: We could just update every single project we find, even if
            // it was duplicated, but we'll be a sticker about it we should not
            // have more than a single reference to the same project.
            if (projectReferences.Length > 1)
            {
                string exception = $"Project `{pathToProjectToUpdate}` has more than one reference to project `{pathToProjectWithNewGuid}` this is invalid.";
                throw new InvalidOperationException(exception);
            }

            // Again there should only be 1, but don't bother with First()
            // incase we change our minds later
            foreach (XElement projectReference in projectReferences)
            {
                MSBuildUtilities.SetProjectReferenceGUID(projectReference, newGuid);
            }

            // We need to check the entire string to see if there was any real change
            bool changesMade = XDocument.Load(pathToProjectToUpdate).ToString() != projXml.ToString();

            if (changesMade)
            {
                projXml.Save(pathToProjectToUpdate);
            }
        }
    }
}
