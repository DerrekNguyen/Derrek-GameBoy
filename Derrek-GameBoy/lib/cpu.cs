using System;

public unsafe class CPUContext
{
   public CPU_Registers regs;

   // Current fetch...
   public UInt16 fetchData;
   public UInt16 memDest;
   public byte curOpcode;

   public Instruction* CurrInst;

   public bool halted;
   public bool stepping;
}

public static class CPU
{
   public static void CPU_Init()
   {

   }

   public static bool CPU_Step()
   {
      Console.WriteLine("CPU not yet implemented");
      return false;
   }
}