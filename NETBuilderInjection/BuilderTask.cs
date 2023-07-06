﻿using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using Microsoft.Build.Framework;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.Build.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

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
            //LogInputVariables();

            string AssemblyPath = this.TargetPath;
            string Compiler;
            ModuleDefMD AssemblyModule = null;

            try { Compiler = UnzipTCC_Compiler(); Log.LogWarning("Tcc Extracted!"); } catch (Exception ex) { Log.LogErrorFromException(ex); return false; }

            try { AssemblyModule = ModuleDefMD.Load(AssemblyPath); Log.LogWarning("Assembly Target Loaded!"); } catch (Exception ex) { Log.LogErrorFromException(ex); return false; }

            string StubDir = SetUpStup();


            CustomAttribute InjAttribute = null;

            MethodDef EntryMethod = GetEntryPointWithAttr(AssemblyModule, out InjAttribute);

            if (EntryMethod == null) { Log.LogWarning("[Error] The EntryPoint was not found in your dll"); return true; } else { Log.LogWarning("EntryPoint Loaded!"); }

            bool CreateThreadStart = true;

            string BuildTarget = ".dll";

            try
            {
                //Setup Config parameters 
                CANamedArgument InjAtributo = InjAttribute.GetProperty("CreateThread"); if (InjAtributo != null) { CreateThreadStart = (bool)InjAtributo.Value; }

                CANamedArgument BuildFormatAtrrib = InjAttribute.GetProperty("BuildTarget"); if (BuildFormatAtrrib != null) { BuildTarget = BuildFormatAtrrib.Value.ToString(); }


                Log.LogWarning($"CreateThreadStart : {CreateThreadStart}");
                Log.LogWarning($"Build Target Format : {BuildTarget}");

            }
            catch (Exception ex) { Log.LogWarning(ex.Message); Log.LogWarning($"Attribute parameters Error : Please check the format!!"); }



            string TempNameAssembly = Path.GetFileNameWithoutExtension(AssemblyPath) + ".exported" + BuildTarget;
            string TempAssembly = System.IO.Path.Combine(System.IO.Path.GetTempPath(), TempNameAssembly);

            try
            {
            AssemblyModule.Kind = ModuleKind.Dll;

            EntryMethod.ExportInfo = new MethodExportInfo();
            EntryMethod.IsUnmanagedExport = true;
            
            ModuleWriterOptions opts = new ModuleWriterOptions(AssemblyModule);
            opts.Cor20HeaderOptions.Flags = 0;
            AssemblyModule.Write(TempAssembly, opts);
            }
            catch (Exception ex) { Log.LogWarning(ex.Message); Log.LogWarning($"Error writing .NET Assembly!!"); return true; }

            string TCC_Args = "";

            if (BuildTarget.ToLower() != ".exe") { TCC_Args = " -shared"; }

            System.Text.StringBuilder StringMemory = new System.Text.StringBuilder();
            byte[] FileBytes;

            try
            {
                Log.LogWarning("[INFO] Reading Assembly to ByteArray.");

                using (FileStream Input = new FileStream(TempAssembly, FileMode.Open, FileAccess.Read)) {

                using (BinaryReader Reader = new BinaryReader(Input)) {

                    FileBytes = Reader.ReadBytes(System.Convert.ToInt32(Input.Length));

                    List<string> ListHexStr = HexEncoding.GetString(FileBytes); // Core.Utils.BytesToHex(FileBytes)

                    int MaxStr = 0;
                    string Separator = ", ";
                    int IntCount = 0;
                    foreach (string HexByte in ListHexStr)
                    {
                        if (MaxStr == ListHexStr.Count)
                            Separator = string.Empty;
                        if (IntCount >= 20)
                        {
                            StringMemory.Append("0x" + HexByte + Separator + Environment.NewLine);
                            IntCount = 0;
                        }
                        else
                        {
                            StringMemory.Append("0x" + HexByte + Separator);
                            IntCount += 1;
                        }
                        MaxStr += 1;
                    }


                }

            }

                Log.LogWarning("[INFO] FileBytes.Length : " + FileBytes.Length.ToString());

                string StubStr = File.ReadAllText(StubDir);

                StubStr = StubStr.Replace("$DataLength$", FileBytes.Length.ToString());
                StubStr = StubStr.Replace("$DataByte$", StringMemory.ToString());
                StubStr = StubStr.Replace("$DllName$", TempNameAssembly);
                StubStr = StubStr.Replace("$DllMain$", EntryMethod.Name);
                StubStr = StubStr.Replace("$AsyncThread$", CreateThreadStart.ToString().ToLower());

                string StubTempFile = Path.Combine(Path.GetTempPath(), "StubTemp.c");
                if (File.Exists(StubTempFile) == true) { File.Delete(StubTempFile); }

                File.WriteAllText(StubTempFile, StubStr);

                Log.LogWarning("[INFO] Stub Writed! -> " + StubTempFile);

                string SavePath = Path.Combine(Path.GetDirectoryName(AssemblyPath), TempNameAssembly);

                if (TCC_Args == "") { Log.LogWarning("[INFO] " + "Compiling in executable mode!!"); }

                Log.LogWarning("[RUNNING] TCC  -> " + Compiler);

                string TccResult = RuntccHost(Compiler, StubTempFile, SavePath, TCC_Args);

                Log.LogWarning("[DEBUG] " + TccResult);

                Log.LogWarning("[DEBUG] Saving to '" + SavePath + "'...");
                Log.LogWarning("[INFO] Done.");


            }
            catch (Exception ex) { Log.LogWarning(ex.Message); Log.LogWarning($"Unexpected Error!!"); }



          return true;

        }

        public string RuntccHost(string tcc, string Stub, string Out, string Argument = "")
        {
            try
            {
                string FullArguments = "\"" + Stub + "\"" + Argument + " -o " + "\"" + Out + "\"";
                Process cmdProcess = new Process();
                {
                    var withBlock = cmdProcess;
                    withBlock.StartInfo = new ProcessStartInfo(tcc, FullArguments);
                    {
                        var withBlock1 = withBlock.StartInfo;
                        withBlock1.CreateNoWindow = true;
                        withBlock1.UseShellExecute = false;
                        withBlock1.RedirectStandardOutput = true;
                        withBlock1.RedirectStandardError = true;
                    }
                    withBlock.Start();
                    withBlock.WaitForExit();
                }

                string HostOutput = cmdProcess.StandardOutput.ReadToEnd().ToString() + Environment.NewLine + cmdProcess.StandardError.ReadToEnd().ToString();

                return HostOutput.ToString();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
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

        public string UnzipTCC_Compiler() {

            bool ExtractTCC = false;

            string CCompilerDir = Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), "TCC");

            if (Directory.Exists(CCompilerDir) == false) { Directory.CreateDirectory(CCompilerDir); ExtractTCC = true; }

            string CCompilerExe = Path.Combine(CCompilerDir, "tcc.exe");

            if (File.Exists(CCompilerExe) == false) { ExtractTCC = true; }

            if (ExtractTCC == true)
            {
                string TempWriteZip = Path.Combine(Path.GetTempPath(), "Tcc.zip");

                if (File.Exists(TempWriteZip) == true) { File.Delete(TempWriteZip); }

                File.WriteAllBytes(TempWriteZip, NETBuilderInjection.Properties.Resources.Bin);

                System.IO.Compression.ZipFile.ExtractToDirectory(TempWriteZip, CCompilerDir);
            }

            return CCompilerExe;

        }

        public string SetUpStup() {

            string StubPath = Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), "Stub.c");
            if (File.Exists(StubPath) == false) { File.WriteAllText(StubPath, NETBuilderInjection.Properties.Resources.Stub); }
            return StubPath;
        }

        public MethodDef GetEntryPointWithAttr(ModuleDefMD asm, out CustomAttribute InjectionAtrribute) {

            if (asm.IsILOnly == false)
            {
                InjectionAtrribute = null;
                Log.LogError("NETBuilderInjection only supports .NET binaries"); return null;
            }

            MethodDef MethodEntryPoint = null;
            CustomAttribute InjAttribute = null;

            IEnumerable<TypeDef> TypesClass = asm.GetTypes();

            foreach (TypeDef Classes in TypesClass)
            {
                IList<MethodDef> MethodList = Classes.Methods;

                foreach (MethodDef Method in MethodList)
                {
                    try {
                        if (Method.CustomAttributes != null && Method.CustomAttributes.Count >= 1)
                        {

                            CustomAttribute InjAtributo = Method.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.Name == "InjectionEntryPoint");
                            
                            if (Method.IsStatic == true && InjAtributo != null)
                            {

                                MethodEntryPoint = Method;
                                InjAttribute = InjAtributo;
                                break;
                            }
                        }
                    } catch (Exception ex) { Log.LogWarning(ex.Message); }
                }
                if (MethodEntryPoint != null) { break; }
            }

            InjectionAtrribute = InjAttribute;
            return MethodEntryPoint;

           

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

        private string ToAbsolutePath(string relativePath)
        {

            // if path is not rooted assume it is relative.
            // convert relative to absolute using project dir as root.

            if (string.IsNullOrWhiteSpace(relativePath)) throw new ArgumentNullException(relativePath);

            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            return Path.GetFullPath(Path.Combine(ProjectDir, relativePath));

        }


        #endregion

    }
}