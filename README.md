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
  
  1) Download Last Release version : [https://github.com/DestroyerDarkNess/NETBuilderInjection/releases/tag/1.0.0](https://github.com/DestroyerDarkNess/NETBuilderInjection/releases/tag/1.0.0)
  2) Open Package Manager Console in your VS Proyect.
  3) Install NetBuilderInjection :
  ```
     Install-Package "C:xxxxxxx\Downloads\NETBuilderInjection.1.0.0.nupkg"
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
    }

public class dllmain
{
    [InjectionEntryPoint(CreateThread = true, BuildTarget = ".dll")]
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
End Class

Public Class dllmain


    <InjectionEntryPoint(CreateThread:=True, BuildTarget:=".dll")>
    Public Shared Sub EntryPoint()

        MessageBox.Show("Hello World", "DLL Test")

    End Sub


End Class
```
- You will find your compiled assembly with the name: "xxx.exported.dll", that is the one that you can inject with any injector.

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






