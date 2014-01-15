﻿using System;
using System.Runtime.InteropServices;
using LLVM;
using CC = System.Runtime.InteropServices.CallingConvention;

namespace CudaSharp
{
    static class PInvokeHelper
    {
        private static bool _initialized;

        public static byte[] EmitInMemory(Module module)
        {
            if (!_initialized)
            {
                _initialized = true;
                PInvoke.LLVMInitializeNVPTXTarget();
                PInvoke.LLVMInitializeNVPTXTargetMC();
                PInvoke.LLVMInitializeNVPTXTargetInfo();
                PInvoke.LLVMInitializeNVPTXAsmPrinter();
            }
            var triple = Marshal.PtrToStringAnsi(PInvoke.LLVMGetTarget(module));
            IntPtr errorMessage;
            IntPtr target;
            if (PInvoke.LLVMGetTargetFromTriple(triple, out target, out errorMessage))
                throw new Exception(Marshal.PtrToStringAnsi(errorMessage));
            var targetMachine = PInvoke.LLVMCreateTargetMachine(target, triple, "sm_20", "",
                PInvoke.LlvmCodeGenOptLevel.LlvmCodeGenLevelDefault, PInvoke.LlvmRelocMode.LlvmRelocDefault,
                PInvoke.LlvmCodeModel.LlvmCodeModelDefault);

            IntPtr memoryBuffer;
            PInvoke.LLVMTargetMachineEmitToMemoryBuffer(targetMachine, module, PInvoke.LlvmCodeGenFileType.LlvmAssemblyFile, out errorMessage, out memoryBuffer);

            if (errorMessage != IntPtr.Zero)
            {
                var errorMessageStr = Marshal.PtrToStringAnsi(errorMessage);
                if (string.IsNullOrWhiteSpace(errorMessageStr) == false)
                    throw new Exception(errorMessageStr);
            }
            var bufferStart = PInvoke.LLVMGetBufferStart(memoryBuffer);
            var bufferLength = PInvoke.LLVMGetBufferSize(memoryBuffer);
            var buffer = new byte[bufferLength.ToInt32()];
            Marshal.Copy(bufferStart, buffer, 0, buffer.Length);
            PInvoke.LLVMDisposeMemoryBuffer(memoryBuffer);
            return buffer;
        }
    }

    static class PInvoke
    {
        const string LlvmDll = "LLVM-3.3";

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadLibrary(string file);

        [DllImport(LlvmDll, CallingConvention = CC.Cdecl)]
        public static extern void LLVMSetTarget(IntPtr module, string triple);

        [DllImport(LlvmDll, CallingConvention = CC.Cdecl)]
        public static extern void LLVMSetDataLayout(IntPtr module, string triple);

        [DllImport(LlvmDll, CallingConvention = CC.Cdecl)]
        public static extern void LLVMAddNamedMetadataOperand(IntPtr module, string name, IntPtr value);

        // ReSharper disable once InconsistentNaming
        public static IntPtr LLVMMDNodeInContext(IntPtr context, IntPtr[] values)
        {
            return LLVMMDNodeInContext(context, values, (uint)values.Length);
        }

        [DllImport(LlvmDll, CallingConvention = CC.Cdecl)]
        public static extern IntPtr LLVMMDNodeInContext(IntPtr context, IntPtr[] values, uint count);

        // ReSharper disable once InconsistentNaming
        public static IntPtr LLVMMDStringInContext(IntPtr context, string str)
        {
            return LLVMMDStringInContext(context, str, (uint)str.Length);
        }

        [DllImport(LlvmDll, CallingConvention = CC.Cdecl)]
        public static extern IntPtr LLVMMDStringInContext(IntPtr context, string str, uint strLen);

        public enum LlvmCodeGenOptLevel
        {
            LlvmCodeGenLevelNone,
            LlvmCodeGenLevelLess,
            LlvmCodeGenLevelDefault,
            LlvmCodeGenLevelAggressive
        }

        public enum LlvmRelocMode
        {
            LlvmRelocDefault,
            LlvmRelocStatic,
            LlvmRelocPic,
            LlvmRelocDynamicNoPic
        }

        public enum LlvmCodeModel
        {
            LlvmCodeModelDefault,
            LlvmCodeModelJitDefault,
            LlvmCodeModelSmall,
            LlvmCodeModelKernel,
            LlvmCodeModelMedium,
            LlvmCodeModelLarge
        }

        [DllImport(LlvmDll, CallingConvention = CC.Cdecl)]
        public static extern IntPtr LLVMCreateTargetMachine(IntPtr target, string triple, string cpu, string features, LlvmCodeGenOptLevel level, LlvmRelocMode reloc, LlvmCodeModel codeModel);

        public enum LlvmCodeGenFileType : uint
        {
            LlvmAssemblyFile,
            LlvmObjectFile
        };

        [DllImport(LlvmDll, CallingConvention = CC.Cdecl)]
        public static extern void LLVMInitializeNVPTXAsmPrinter();

        [DllImport(LlvmDll, CallingConvention = CC.Cdecl)]
        public static extern void LLVMInitializeNVPTXTarget();

        [DllImport(LlvmDll, CallingConvention = CC.Cdecl)]
        public static extern void LLVMInitializeNVPTXTargetMC();

        [DllImport(LlvmDll, CallingConvention = CC.Cdecl)]
        public static extern void LLVMInitializeNVPTXTargetInfo();

        [DllImport(LlvmDll, CallingConvention = CC.Cdecl)]
        public static extern IntPtr LLVMGetTarget(IntPtr module);

        [DllImport(LlvmDll, CallingConvention = CC.Cdecl)]
        public static extern bool LLVMGetTargetFromTriple(string triple, out IntPtr target, out IntPtr errorMessage);

        [DllImport(LlvmDll, CallingConvention = CC.Cdecl)]
        public static extern bool LLVMTargetMachineEmitToMemoryBuffer(IntPtr targetMachine, IntPtr module, LlvmCodeGenFileType codegen, out IntPtr errorMessage, out IntPtr memoryBuffer);

        [DllImport(LlvmDll, CallingConvention = CC.Cdecl)]
        public static extern bool LLVMTargetMachineEmitToFile(IntPtr targetMachine, IntPtr module, string filename, LlvmCodeGenFileType codegen, out IntPtr errorMessage);

        [DllImport(LlvmDll, CallingConvention = CC.Cdecl)]
        public static extern IntPtr LLVMGetBufferStart(IntPtr memoryBuffer);

        [DllImport(LlvmDll, CallingConvention = CC.Cdecl)]
        public static extern IntPtr LLVMGetBufferSize(IntPtr memoryBuffer);

        [DllImport(LlvmDll, CallingConvention = CC.Cdecl)]
        public static extern void LLVMDisposeMemoryBuffer(IntPtr memoryBuffer);
    }
}
