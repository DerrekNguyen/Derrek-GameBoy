using System;

public struct CPU_Registers
{
   public byte a;
   public byte f;
   public byte b;
   public byte c;
   public byte d;
   public byte e;
   public byte h;
   public byte l;
   public UInt16 pc;
   public UInt16 sp;

}

public class CPUContext
{
   public CPU_Registers regs;

   // Current fetch...
   public UInt16 fetch_data;
   public UInt16 mem_dest;
   public byte cur_opcode;

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