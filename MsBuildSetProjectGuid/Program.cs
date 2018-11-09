﻿using System;
using System.IO;
using System.Threading.Tasks;

namespace MsBuildSetProjectGuid
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] targetVersions = new string[] { "6x", "7x", "8x" };
            string targetGuid = Guid.NewGuid().ToString();
            string partialProjectPath = @"Fusion\ServiceHosts\WindowsServices\BulkGasReadingServiceHost\CU.BulkGasReadingService.TestClient\CU.Job.BulkGasReadingService.TestClient.csproj";

            Parallel.ForEach(targetVersions, targetVersion =>
            {
                string targetDirectory = $"s:\\timssvn\\{targetVersion}\\trunk";
                string targetProject = Path.Combine(targetDirectory, partialProjectPath);

                if (File.Exists(targetProject))
                {
                    SetProjectGuid.Execute(targetDirectory, targetProject, targetGuid);
                }
            }
            );

        }



    }
}
