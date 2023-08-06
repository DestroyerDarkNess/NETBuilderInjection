using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NETBuilderInjection.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace NETBuilderInjection
{
    public class BuilderTask : Task
    {
        #region Public Properties

        public virtual ITaskItem[] InputAssemblies { get; set; }

        public string[] AdditionalLocations { get; set; }

        [Required]
        public string ConfigurationFilePath { get; set; }

        [Required]
        public string SolutionDir { get; set; }

        public string SolutionPath { get; set; }

        [Required]
        public string ProjectDir { get; set; }

        public string ProjectFileName { get; set; }

        public string ProjectPath { get; set; }

        [Required]
        public string TargetDir { get; set; }

        public string TargetPath { get; set; }

        public string TargetFileName { get; set; }

        [Required]
        public string TargetFrameworkVersion { get; set; }

        public string TargetArchitecture { get; set; }

        public string ILMergeConsolePath { get; set; }

        public string KeyFile { get; set; }

        #endregion

        #region Constructors

        public BuilderTask()
        {
            this.InputAssemblies = new ITaskItem[0];
        }

        #endregion

        #region Public Methods

        public override bool Execute()
        {

            string AssemblyPath = this.TargetPath; if (Path.GetExtension(AssemblyPath).ToLower() == ".exe") { Log.LogWarning(""); return true; }

            Packer PackEngine = new Packer(AssemblyPath);

            string Compiler = string.Empty; string LibzMerger = string.Empty; string LizProtect = string.Empty;


            // Resources Extract..

            try { Compiler = Helpers.UnzipTCC_Compiler(this.GetType().Assembly.Location); Log.LogWarning("Tcc Extracted!"); } catch (Exception ex) { Log.LogErrorFromException(ex); return false; }

            try { LibzMerger = Helpers.ExtractLibz(this.GetType().Assembly.Location); Log.LogWarning("Libz Extracted!"); } catch (Exception ex) { Log.LogErrorFromException(ex); return false; }

            try { LizProtect = Helpers.Unzip_LizProtect(this.GetType().Assembly.Location); Log.LogWarning("LizProtect Extracted!"); } catch (Exception ex) { Log.LogErrorFromException(ex); return false; }

            string StubDir = Helpers.SetupStub(this.GetType().Assembly.Location);


            PackEngine.BuilderTask = this;

            if (PackEngine.IsValidAssembly() == false)
            {
                Log.LogWarning("Invalid Assembly!");
                return true;
            }

            if (PackEngine.Load() == false)
            {
                Log.LogWarning("Entry point not found.");
                return true;
            }

            if (PackEngine.Build(Compiler, LibzMerger, StubDir, LizProtect) == true)
            {
                if (File.Exists(PackEngine.BuildPath) == true)
                {
                    Log.LogWarning("[Success Packaging] '" + PackEngine.BuildPath + "'");
                    Log.LogWarning("[Done] Thanks for Using.");
                }
                else { Log.LogWarning("[ERROR] '" + "Compiler Error :(" + "'"); }


            }
            else { Log.LogWarning("[ERROR] '" + PackEngine.BuildPath + "'"); }

            return true;

        }



        #endregion

        #region Private Methods
        private void LogInputVariables()
        {
            Log.LogWarning($"SolutionDir: {SolutionDir}");
            Log.LogWarning($"SolutionPath: {SolutionPath}");
            Log.LogWarning($"ProjectDir: {ProjectDir}");
            Log.LogWarning($"ProjectFileName: {ProjectFileName}");
            Log.LogWarning($"ProjectPath: {ProjectPath}");
            Log.LogWarning($"TargetDir: {TargetDir}");
            Log.LogWarning($"TargetPath: {TargetPath}");
            Log.LogWarning($"TargetFileName: {TargetFileName}");
            Log.LogWarning($"TargetFrameworkVersion: {TargetFrameworkVersion}");
            Log.LogWarning($"TargetArchitecture: {TargetArchitecture}");
            Log.LogWarning($"KeyFile: {KeyFile}");
            Log.LogWarning($"ConfigurationFilePath: {ConfigurationFilePath}");
        }



        public string GetILMergePath()
        {
            string exePath = null;
            string errMsg;
            var failedPaths = new List<string>();
            if (string.IsNullOrWhiteSpace(ILMergeConsolePath))
            {
                Log.LogWarning("Variable $(ILMergeConsolePath) is not available. For a better experience please make sure you are using the latest version of ilmerge's nuget pakcage.");
            }
            else
            {
                exePath = Path.GetFullPath(ILMergeConsolePath);
                Log.LogMessage($"ILMerge.exe found at $(ILMergeConsolePath): {exePath}");
                return exePath;
            }

            Log.LogMessage($"ILMerge.exe found at (task location): {Path.GetDirectoryName(this.GetType().Assembly.Location)}");

            Log.LogMessage($"ILMerge.exe found at (solution dir): {this.SolutionDir}");

            return exePath;

        }

        #endregion

    }
}
