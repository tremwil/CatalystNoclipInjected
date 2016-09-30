using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Catalyst;
using Catalyst.Memory;

namespace CatalystNoclip
{
    static class FileMappingWrapper
    {
        const int PAGE_READWRITE = 0x04;
        const int FILE_MAP_ALL_ACCESS = 0xF001F;
        const int NULL = 0x00;
        static readonly IntPtr INVALID_HANDLE_VALUE = (IntPtr)(-1);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr CreateFileMapping(
            IntPtr hFile,
            object lpFileMappingAttributes,
            uint flProtect,
            uint dwMaximumSizeHigh,
            uint dwMaximumSizeLow,
            [MarshalAs(UnmanagedType.LPStr)]
            string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr MapViewOfFile(
            IntPtr hFileMappingObject,
            uint dwDesiredAccess,
            uint dwFileOffsetHigh,
            uint dwFileOffsetLow,
            uint dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        public static IntPtr CreateFileMapping(string name, int size)
        {
            return CreateFileMapping(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, (uint)size, name);
        }

        public static IntPtr GetFileMapPtr(IntPtr hMapFile, int size)
        {
            return MapViewOfFile(hMapFile, FILE_MAP_ALL_ACCESS, 0, 0, (uint)size);
        }

        public static T ReadGenericFromFileMap<T>(IntPtr fileMapPtr) where T : struct
        {
            return Marshal.PtrToStructure<T>(fileMapPtr);
        }

        public static void WriteGenericToFileMap<T>(IntPtr fileMapPtr, T value) where T : struct
        {
            byte[] bytes = GenericBitConverter.ToBytes(value);
            Marshal.Copy(bytes, 0, fileMapPtr, bytes.Length);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SharedInfo
    {
        public int injectorPID;
        public int noclipState;
        public int noStumbleState;
        public Vec3 dpos;
    }
}
