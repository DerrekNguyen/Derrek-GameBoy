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
         InType.IN_DI => ProcDI,
         InType.IN_XOR => ProcXOR,
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

   /// <summary>
   /// Helper function for ProcXOR. Set the bits of the flag register in 'ctx' according to flags z, n, h, c
   /// </summary>
   private static void CPUSetFlags(CPUContext ctx, bool? z, bool? n, bool? h, bool? c)
   {
      if (z.HasValue)
      {
         Common.BIT_SET(ctx.regs.f, 7, z.Value);
      }

      if (n.HasValue)
      {
         Common.BIT_SET(ctx.regs.f, 6, n.Value);
      }

      if (h.HasValue)
      {
         Common.BIT_SET(ctx.regs.f, 5, h.Value);
      }

      if (c.HasValue)
      {
         Common.BIT_SET(ctx.regs.f, 4, c.Value);
      }
   }

   public static void ProcXOR(CPUContext ctx)
   {
      ctx.regs.a ^= (byte)ctx.fetchedData;
      CPUSetFlags(ctx, ctx.regs.a == 0, false, false, false);
   }

   public static void ProcDI(CPUContext ctx)
   {
      ctx.intMasterEnabled = false;
   }

   public static IN_PROC InstGetProcessor(InType type)
   {
      return GetProc(type);
   }
}
