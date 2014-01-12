CudaSharp
=========

A library to make C# run on CUDA-enabled GPUs

This library is currently far from complete and is not intended for general use.

C# is executed on the GPU like so:
1) Your C# compiler translates C# to .net CIL
2) Calling the CudaSharp.Translate method reads the CIL, translates to LLVM IR, and compilers to NVIDIA PTX
3) Use your GPU library of choice (for example, ManagedCuda) to read in the PTX file and execute it on the GPU

For an example usage, see CudaSharpTest/Program.cs
