using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public delegate void IN_PROC(CPUContext context);
public static class CPUProc
{
   private static bool CheckCondition(CPUContext ctx)
   {
      bool z = CPU.CPU_FLAG_Z;
      bool c = CPU.CPU_FLAG_C;

      switch (ctx.CurrInst.cond)
      {
         case CondType.CT_NONE: return true;
         case CondType.CT_NZ: return !z;
         case CondType.CT_Z: return z;
         case CondType.CT_NC: return !c;
         case CondType.CT_C: return c;
         default: return false;
      }
   }
   public static IN_PROC GetProc(InType type)
   {
      return type switch
      {
         InType.IN_NONE => ProcNone,
         InType.IN_NOP => ProcNoop,
         InType.IN_LD => ProcLD,
         InType.IN_JP => ProcJP,
         _ => ProcNone
      };
   }
   public static void ProcNone(CPUContext ctx)
   {
      Console.WriteLine("Invalid Instruction");
      Environment.Exit(-7);
   }

   public static void ProcNoop(CPUContext ctx)
   {

   }

   public static void ProcLD(CPUContext ctx)
   {

   }

   public static void ProcJP(CPUContext ctx)
   {
      if (CheckCondition(ctx))
      {
         ctx.regs.pc = ctx.fetchedData;
         Emulator.EmuCycle(1);
      }
   }
   
   public static IN_PROC InstGetProcessor(InType type)
   {
      return GetProc(type);
   }
}
