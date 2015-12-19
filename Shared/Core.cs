﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace SharpAllods.Shared
{
    class AllodsException : SystemException
    {
        internal AllodsException(string text) : base(text) { /* stub */ }
    }

    class Core
    {
        public static void Abort(string format, params object[] args)
        {
            throw new AllodsException(String.Format(format, args));
        }

        public static string UnpackByteString(int encoding, byte[] bytes)
        {
            string str_in = Encoding.GetEncoding(encoding).GetString(bytes);
            string str_out = str_in;
            int i = str_out.IndexOf('\0');
            if (i >= 0) str_out = str_out.Substring(0, i);
            return str_out;
        }

        public static byte[] PackByteString(int encoding, string str, uint length)
        {
            byte[] b_out = new byte[length];
            if (b_out.Length > length)
                Array.Resize<byte>(ref b_out, (int)length);
            return b_out;
        }

        private static Stopwatch TickCounter = null; // since first GetTickCount
        public static long GetTickCount()
        {
            if (TickCounter == null)
            {
                TickCounter = new Stopwatch();
                TickCounter.Start();
            }

            return TickCounter.ElapsedMilliseconds;
        }
    }

    // copypasted from MSDN
    public unsafe class Memory
    {
        // Handle for the process heap. This handle is used in all calls to the
        // HeapXXX APIs in the methods below.
        static int ph = GetProcessHeap();
        // Private instance constructor to prevent instantiation.
        private Memory() { }
        // Allocates a memory block of the given size. The allocated memory is
        // automatically initialized to zero.
        public static void* Alloc(int size)
        {
            void* result = HeapAlloc(ph, HEAP_ZERO_MEMORY, size);
            if (result == null) throw new OutOfMemoryException();
            return result;
        }
        // Copies count bytes from src to dst. The source and destination
        // blocks are permitted to overlap.
        public static void Copy(void* src, void* dst, int count)
        {
            byte* ps = (byte*)src;
            byte* pd = (byte*)dst;
            if (ps > pd)
            {
                for (; count != 0; count--) *pd++ = *ps++;
            }
            else if (ps < pd)
            {
                for (ps += count, pd += count; count != 0; count--) *--pd = *--ps;
            }
        }
        // Frees a memory block.
        public static void Free(void* block)
        {
            if (!HeapFree(ph, 0, block)) throw new InvalidOperationException();
        }
        // Re-allocates a memory block. If the reallocation request is for a
        // larger size, the additional region of memory is automatically
        // initialized to zero.
        public static void* ReAlloc(void* block, int size)
        {
            void* result = HeapReAlloc(ph, HEAP_ZERO_MEMORY, block, size);
            if (result == null) throw new OutOfMemoryException();
            return result;
        }
        // Returns the size of a memory block.
        public static int SizeOf(void* block)
        {
            int result = HeapSize(ph, 0, block);
            if (result == -1) throw new InvalidOperationException();
            return result;
        }
        // Heap API flags
        const int HEAP_ZERO_MEMORY = 0x00000008;
        // Heap API functions
        [DllImport("kernel32")]
        static extern int GetProcessHeap();
        [DllImport("kernel32")]
        static extern void* HeapAlloc(int hHeap, int flags, int size);
        [DllImport("kernel32")]
        static extern bool HeapFree(int hHeap, int flags, void* block);
        [DllImport("kernel32")]
        static extern void* HeapReAlloc(int hHeap, int flags,
            void* block, int size);
        [DllImport("kernel32")]
        static extern int HeapSize(int hHeap, int flags, void* block);
    }
}
