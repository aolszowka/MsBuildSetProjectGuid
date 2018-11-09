// -----------------------------------------------------------------------
// <copyright file="MSBuildUtilities.cs" company="Ace Olszowka">
//  Copyright (c) Ace Olszowka 2018. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MsBuildSetProjectGuid
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    public static class MSBuildUtilities
    {
        internal static XNamespace msbuildNS = @"http://schemas.microsoft.com/developer/msbuild/2003";

        /// <summary>
        /// Extracts the Project GUID from the specified proj File.
        /// </summary>
        /// <param name="projXml">The XDocument representation fo the project file.</param>
        /// <param name="pathToProjFile">The path to this project file.</param>
        /// <returns>The specified proj File's Project GUID.</returns>
        public static string GetMSBuildProjectGuid(XDocument projXml, string pathToProjFile)
        {
            XElement projectGuid = projXml.Descendants(msbuildNS + "ProjectGuid").FirstOrDefault();

            if (projectGuid == null)
            {
                string exception = $"Project {pathToProjFile} did not contain a ProjectGuid.";
                throw new InvalidOperationException(pathToProjFile);
            }

            return projectGuid.Value;
        }

        public static void SetMSBuildProjectGuid(XDocument projXml, string newGuid)
        {
            IEnumerable<XElement> projectGuidNodes = projXml.Descendants(msbuildNS + "ProjectGuid");

            // Never trust the guid coming in, always clean it up
            string newGuidClean = Guid.Parse(newGuid).ToString("B").ToUpperInvariant();

            // In theory there should never be more than one but just set em all just in case
            foreach (XElement projectGuidNode in projectGuidNodes)
            {
                projectGuidNode.Value = newGuidClean;
            }
        }

        public static IEnumerable<XElement> GetProjectReferenceNodes(XDocument projXml)
        {
            return projXml.Descendants(msbuildNS + "ProjectReference");
        }

        public static string GetProjectReferenceGUID(XElement projectReference, string projectPath)
        {
            string projectReferenceGuid = string.Empty;

            // Get the existing Project Reference GUID
            XElement projectReferenceGuidElement = projectReference.Descendants(msbuildNS + "Project").FirstOrDefault();

            if (projectReferenceGuidElement == null)
            {
                //string exception = $"A ProjectReference in {projectPath} does not contain a Project Element; this is invalid.";
                //throw new InvalidOperationException(exception);
            }
            else
            {
                // This is the referenced project
                projectReferenceGuid = projectReferenceGuidElement.Value;
            }

            return projectReferenceGuid;
        }

        public static void SetProjectReferenceGUID(XElement projectReference, string projectGuid)
        {
            string cleanProjectGuid = Guid.Parse(projectGuid).ToString("B").ToUpperInvariant();
            projectReference.Descendants(msbuildNS + "Project").First().SetValue(cleanProjectGuid);
        }

        public static string GetProjectReferenceIncludeValue(XElement projectReference, string projectPath)
        {
            // Get the existing Project Reference Include Value
            XAttribute projectReferenceIncludeAttribute = projectReference.Attribute("Include");

            if (projectReferenceIncludeAttribute == null)
            {
                string exception = $"A ProjectReference in {projectPath} does not contain an Include Attribute on it; this is invalid.";
                throw new InvalidOperationException(exception);
            }

            // This is the referenced project
            string projectReferenceInclude = projectReferenceIncludeAttribute.Value;

            return projectReferenceInclude;
        }
    }
}
