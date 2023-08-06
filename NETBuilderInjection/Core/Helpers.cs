using System.IO;

namespace NETBuilderInjection.Core
{
    public class Helpers
    {

        public static string ExtractLibz(string DirToExtract)
        {
            string libzPath = Path.Combine(Path.GetDirectoryName(DirToExtract), "libz.exe");
            if (File.Exists(libzPath) == false) { File.WriteAllBytes(libzPath, NETBuilderInjection.Properties.Resources.libz); }
            return libzPath;
        }

        public static string SetupStub(string DirToExtract)
        {

            string StubPath = Path.Combine(Path.GetDirectoryName(DirToExtract), "Stub.c");
            if (File.Exists(StubPath) == false) { File.WriteAllText(StubPath, NETBuilderInjection.Properties.Resources.Stub); }
            return StubPath;
        }

        public static string UnzipTCC_Compiler(string DirToExtract)
        {

            bool ExtractTCC = false;

            string CCompilerDir = Path.Combine(Path.GetDirectoryName(DirToExtract), "TCC");

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

        public static string Unzip_LizProtect(string DirToExtract)
        {

            bool ExtractTCC = false;

            string CCompilerDir = Path.Combine(Path.GetDirectoryName(DirToExtract), "LizProtect");

            if (Directory.Exists(CCompilerDir) == false) { Directory.CreateDirectory(CCompilerDir); ExtractTCC = true; }

            string CCompilerExe = Path.Combine(CCompilerDir, "Run.CLI.exe");

            if (File.Exists(CCompilerExe) == false) { ExtractTCC = true; }

            if (ExtractTCC == true)
            {
                string TempWriteZip = Path.Combine(Path.GetTempPath(), "Liz.zip");

                if (File.Exists(TempWriteZip) == true) { File.Delete(TempWriteZip); }

                File.WriteAllBytes(TempWriteZip, NETBuilderInjection.Properties.Resources.Liz);

                System.IO.Compression.ZipFile.ExtractToDirectory(TempWriteZip, CCompilerDir);
            }

            return CCompilerExe;

        }

    }
}
