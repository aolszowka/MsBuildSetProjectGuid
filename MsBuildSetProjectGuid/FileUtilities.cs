// --------------------------------------------------------
// <copyright file="FileUtilities.cs" company="Ace Olszowka">
//  Copyright (c) Ace Olszowka 2018. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MsBuildSetProjectGuid
{
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Utility class for File Functions.
    /// </summary>
    public static class FileUtilities
    {
        private static byte[] UTF8BOM = new byte[] { 0xEF, 0xBB, 0xBF };

        /// <summary>
        /// Determines if the given file contains the UTF-8 BOM (0xEF,0xBB,0xBF).
        /// </summary>
        /// <param name="inputFile">The path to the file to evaluate.</param>
        /// <returns><c>true</c> if the file contains the UTF-8 BOM; Otherwise, <c>false</c>.</returns>
        public static bool ContainsUTF8BOM(string inputFile)
        {
            using (FileStream fs = File.OpenRead(inputFile))
            {
                byte[] possibleBOM = new byte[3];
                fs.Read(possibleBOM, 0, possibleBOM.Length);

                return possibleBOM.SequenceEqual(UTF8BOM);
            }
        }
    }
}
