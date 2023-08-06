using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace MassCloner
{
    public class ResourceEditor
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ResourceEditor(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException(filename);
            _filename = filename;
        }

        /// <summary>
        /// Set the resources of a file
        /// </summary>
        public void SetResources(Resource[] resources)
        {
            var handle = BeginUpdateResource(_filename, true);
            foreach (var resource in resources)
            {
                var res = UpdateResource(handle, resource.Type, resource.Name, 1033, resource.Pointer, resource.Size);
                if (res == default(IntPtr))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            var result = EndUpdateResource(handle, false);
        }

        /// <summary>
        /// Get the resources of a file
        /// </summary>
        public Resource[] GetResources(params ResourceType[] types)
        {
            _module = LoadLibraryEx(_filename, default(IntPtr), 0x1 | 0x2 | 0x20);

            if (_module == default(IntPtr))
                throw new Exception("Could not load module: " + _filename);

            var manifests = new List<Resource>();

            var callback = new EnumResourceProcedure((IntPtr module, ResourceType type, IntPtr name, IntPtr lparam) =>
            {
                var resource = default(IntPtr);
                if (name.ToInt32() > 1000)
                {
                    resource = FindResource(module, Marshal.PtrToStringUni(name), type);
                }
                else
                {
                    resource = FindResource(module, name, type);
                }
                var size = SizeofResource(module, resource);
                var data = LoadResource(module, resource);
                var ptr = LockResource(data);

                if (ptr != default(IntPtr) && size != default(IntPtr))
                {
                    var manifest = new Resource(ptr) { Type = type, Size = size, Name = name };
                    manifests.Add(manifest);
                }

                FreeResource(ptr);
                return true;
            });

            foreach (var type in types)
            {
                EnumResourceNamesW(_module, type, callback, default(IntPtr));
            }

            return manifests.ToArray();
        }

        /// <summary>
        /// Deconstructor
        /// </summary>
        ~ResourceEditor()
        {
            if (_module != default(IntPtr))
            {
                FreeLibrary(_module);
            }
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr BeginUpdateResource(string filename, bool overwrite);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibraryEx(string filename, IntPtr unused, uint flags);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr module);

        [DllImport("kernel32.dll")]
        private static extern bool EnumResourceNamesW(IntPtr module, ResourceType type, EnumResourceProcedure enumerationProc, IntPtr unused);

        [DllImport("kernel32.dll")]
        private static extern IntPtr UpdateResource(IntPtr resources, ResourceType type, IntPtr name, uint language, IntPtr data, IntPtr length);

        [DllImport("kernel32.dll")]
        private static extern IntPtr EndUpdateResource(IntPtr resources, bool cancel);

        [DllImport("kernel32.dll")]
        private static extern IntPtr FindResource(IntPtr handle, IntPtr name, ResourceType type);

        [DllImport("kernel32.dll")]
        private static extern IntPtr FindResource(IntPtr handle, string name, ResourceType type);

        [DllImport("kernel32.dll")]
        private static extern IntPtr FreeResource(IntPtr dataptr);

        [DllImport("kernel32.dll")]
        private static extern IntPtr SizeofResource(IntPtr module, IntPtr resource);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadResource(IntPtr module, IntPtr resource);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LockResource(IntPtr data);

        private delegate bool EnumResourceProcedure(IntPtr module, ResourceType type, IntPtr name, IntPtr unused);

        private IntPtr _module;
        private string _filename;
    }

    public class Resource
    {
        public IntPtr Pointer
        {
            get { return _ptr; }
        }

        public ResourceType Type
        {
            get; set;
        }

        public IntPtr Size
        {
            get; set;
        }

        public IntPtr Name
        {
            get; set;
        }

        public Resource(IntPtr ptr)
        {
            _ptr = ptr;
        }

        public string GetName()
        {
            return Marshal.PtrToStringUni(Name);
        }

        public int GetSize()
        {
            if (IntPtr.Size * 8 == 64)
            {
                return (int)((Size.ToInt64() << 32) >> 32);
            }
            else
            {
                return Size.ToInt32();
            }
        }

        private IntPtr _ptr;
    }

    public enum ResourceType : uint
    {
        RT_ACCELERATOR = 9,
        RT_ANICURSOR = 21,
        RT_ANIICON = 22,
        RT_BITMAP = 2,
        RT_CURSOR = 1,
        RT_DIALOG = 5,
        RT_DLGINCLUDE = 17,
        RT_FONT = 8,
        RT_FONTDIR = 7,
        RT_GROUP_CURSOR = ((RT_CURSOR) + 11),
        RT_GROUP_ICON = ((RT_ICON) + 11),
        RT_HTML = 23,
        RT_ICON = 3,
        RT_MANIFEST = 24,
        RT_MENU = 4,
        RT_MESSAGETABLE = 11,
        RT_PLUGPLAY = 19,
        RT_RCDATA = 10,
        RT_STRING = 6,
        RT_VERSION = 16,
        RT_VXD = 20,
        RT_DLGINIT = 240,
        RT_TOOLBAR = 241,
    }
}
