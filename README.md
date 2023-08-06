<h1 align="center">NETBuilderInjection</h1>
<p align="center">
  <a href="https://github.com/DestroyerDarkNess/NETBuilderInjection/blob/master/LICENSE">
    <img src="https://img.shields.io/github/license/Rebzzel/kiero.svg?style=flat-square"/>
  </a>
   <img src="https://img.shields.io/badge/platform-Windows-0078d7.svg"/>
  <br>
  Add the injection capability to your managed DLL
</p>

# Install

- Install via NuGet Package :
  ```
  [In process of publication]
  ```
- Manual Installation :
  
  1) Download Last Release version : [NETBuilderInjection v1.0.2](https://github.com/DestroyerDarkNess/NETBuilderInjection/releases/tag/1.0.2)
  2) Open Package Manager Console in your VS Proyect.
  3) Install NetBuilderInjection :
  ```
     Install-Package "C:xxxxxxx\Downloads\NETBuilderInjection.1.0.2.nupkg"
  ```

# Features
**NETBuilderInjection** It has very useful features, which I will explain.

- NETBuilderInjection Works with a special Attribute called "InjectionEntryPoint" which has several Pre-Configuration options:

| Name | Type | Description       |
|----------|--------|---------------|
| CreateThread | bool | If set to true, a new thread will be created for your EntryPoint. |
| BuildTarget  | string | In case your target plugin is not .dll, but rather .asi, then with this you easily configure the target extension. It also has the function of executable (.exe) which is explained below. |
| MergeLibs | bool | Match your assembly with the libraries it needs, if you get a single binary. |

After the first use, **NETBuilderInjection** will extract important resources to use, among them is the Stub.c that would be the container of your application. **That's right NetBuilderInjection is basically a C-packer for .NET that adds a native layer to your assembly.**

**You can edit that Stub.c and create your own packer , you can find it in the following path:**

```
$(ProjectSolution)\packages\NETBuilderInjection.1.0.2\tools
```

# How to Use
- Write your DLL and Build Proyect .

```C

[AttributeUsage(AttributeTargets.Method)]
public class InjectionEntryPoint : Attribute
{
    public bool CreateThread { get; set; } = true;
    public string BuildTarget { get; set; } = ".dll";
    public bool MergeLibs { get; set; } = false;
    public bool ILoader { get; set; } = false;
    public string ProtectionRules { get; set; } = string.Empty;
    public string ILoaderProtectionRules { get; set; } = string.Empty;
    public string PreCompiler { get; set; } = string.Empty;
    public string CloneTo { get; set; } = string.Empty;
    public bool BasicILoaderProtection { get; set; } = false;
}

public class dllmain
{
    [InjectionEntryPoint(MergeLibs = true, CreateThread = true, BuildTarget = ".dll")]
    public static void EntryPoint()
    {
        MessageBox.Show("Hello World", "DLL Test");
    }
}
```

```VB
<AttributeUsage(AttributeTargets.Method)>
Public Class InjectionEntryPoint
    Inherits Attribute

    Public Property CreateThread As Boolean = True
    Public Property BuildTarget As String = ".dll"
    Public Property MergeLibs As Boolean = False
    Public Property ILoader As Boolean = False
    Public Property ProtectionRules As String = String.Empty
    Public Property ILoaderProtectionRules As String = String.Empty
    Public Property PreCompiler As String = String.Empty
    Public Property CloneTo As String = String.Empty
    Public Property BasicILoaderProtection As Boolean = False

End Class

Public Class dllmain


    <InjectionEntryPoint(MergeLibs:=True, CreateThread:=True, BuildTarget:=".dll")>
    Public Shared Sub EntryPoint()

        MessageBox.Show("Hello World", "DLL Test")

    End Sub


End Class
```
- You will find your compiled assembly with the name: "xxx.exported.dll", that is the one that you can inject with any injector.

# Other uses
**NETBuilderInjection** is not limited only to being able to generate a DLL with Injection capacity, it can also generate your executable (.exe) in addition to the ability to join your assembly and libraries in 1 only.

1) In your WinForms Project, go to *Program.cs* or *Program.vb* **(By default you won't see it in your VisualBasic Project, you must add the class manually)**

2) Write the following code in your Program class:

```VB
using System.Runtime.InteropServices;
using System.Threading;

[AttributeUsage(AttributeTargets.Method)]
public class InjectionEntryPoint : Attribute
{
    public bool CreateThread { get; set; } = true;
    public string BuildTarget { get; set; } = ".dll";
    public bool MergeLibs { get; set; } = false;
}

public class Program
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]  private static bool SetProcessDPIAware();

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)] private static bool FreeConsole();
    
    [InjectionEntryPoint(MergeLibs = true, CreateThread = false, BuildTarget = ".exe")]
    [STAThread]
    public static void Main()
    {
        FreeConsole();

        bool Runtime = true;
        Thread t = new Thread(() =>
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SetProcessDPIAware();

            Application.Run(new Form1()); // Start your MainForm of your application
            Runtime = false;
        });

        t.SetApartmentState(ApartmentState.STA);
        t.Start();

        while ((Runtime)) { }
        Environment.Exit(0);
  }
}

```

```VB
Imports System.Runtime.InteropServices
Imports System.Threading

<AttributeUsage(AttributeTargets.Method)>
Public Class InjectionEntryPoint
    Inherits Attribute

    Public Property CreateThread As Boolean = True
    Public Property BuildTarget As String = ".dll"
    Public Property MergeLibs As Boolean = False
End Class

NotInheritable Class Program

    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function SetProcessDPIAware() As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True, ExactSpelling:=True)>
    Private Shared Function FreeConsole() As Boolean
    End Function


    <InjectionEntryPoint(MergeLibs:=True, CreateThread:=False, BuildTarget:=".exe")>
    <STAThread>
    Friend Shared Sub Main()
        FreeConsole()

        Dim Runtime As Boolean = True
        Dim t As Thread = New Thread(Sub()
                                         Application.EnableVisualStyles()
                                         Application.SetCompatibleTextRenderingDefault(False)

                                         SetProcessDPIAware()

                                         Application.Run(New Form1) ' Start your MainForm of your application
                                         Runtime = False
                                     End Sub)

        t.SetApartmentState(ApartmentState.STA)
        t.Start()

        Do While (Runtime) : Loop
        Environment.Exit(0)
    End Sub
End Class

```

**This should work for WPF projects as well, with a little more work on Stub.c you could make your own native packer.**

### limitations

```VB
At the moment only the .NET Framework is supported, in the future .NET Core will be supported.

Although you can still edit the **Stub.c** file to load .NET Core assemblies.
```
  
### License
```
MIT License

Copyright (c) 2019-2023 DestroyerDarkNess

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```






