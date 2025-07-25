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
         InType.IN_LDH => ProcLDH,
         InType.IN_JP => ProcJP,
         InType.IN_DI => ProcDI,
         InType.IN_XOR => ProcXOR,
         InType.IN_POP => ProcPOP,
         InType.IN_PUSH => ProcPUSH,
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

   /// <summary>
   /// Process Load (LD) instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcLD(CPUContext ctx)
   {
      // Special cases (loading into memory)
      if (ctx.destIsMem)
      {
         // If 16-bit register
         if (ctx.CurrInst.reg2 >= RegType.RT_AF)
         {
            Bus.BusWrite16(ctx.memDest, ctx.fetchedData);
            Emulator.EmuCycle(1);
         } else
         {
            Bus.BusWrite(ctx.memDest, (byte)ctx.fetchedData);
         }

         Emulator.EmuCycle(1);

         return;
      }

      if (ctx.CurrInst.mode == AddrMode.AM_HL_SPR)
      {
         bool hflag = ((CPUUtil.CPUReadReg(ctx.CurrInst.reg2) & 0xF) + (ctx.fetchedData & 0xF)) >= 0x10;
         bool cflag = ((CPUUtil.CPUReadReg(ctx.CurrInst.reg2) & 0xFF) + (ctx.fetchedData & 0xFF)) >= 0x100;
         CPUSetFlags(ctx, false, false, hflag, cflag);

         CPUUtil.CPUSetReg(ctx.CurrInst.reg1, (ushort)(CPUUtil.CPUReadReg(ctx.CurrInst.reg2) + ctx.fetchedData));

         return;
      }

      // Load the fetched data into register
      CPUUtil.CPUSetReg(ctx.CurrInst.reg1, ctx.fetchedData);
   }

   /// <summary>
   /// Process Load High Ram (LDH) instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcLDH(CPUContext ctx)
   {
      if (ctx.CurrInst.reg1 == RegType.RT_A)
      {
         CPUUtil.CPUSetReg(ctx.CurrInst.reg1, Bus.BusRead((UInt16)(0xFF00 | ctx.fetchedData)));
      } 
      else
      {
         Bus.BusWrite((UInt16)(0xFF00 | ctx.fetchedData), (byte)CPUUtil.CPUReadReg(ctx.CurrInst.reg2));
      }

      Emulator.EmuCycle(1);
   }

   /// <summary>
   /// Generic jump instruction.
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   /// <param name="address">Destination address</param>
   /// <param name="pushpc">Whether to push pc to stack</param>
   public static void GotoAddr(CPUContext ctx, UInt16 address, bool pushpc)
   {
      if (CheckCondition(ctx)) 
      {
         if (pushpc)
         {
            Stack.Push16(ctx.regs.pc);
            Emulator.EmuCycle(2); // pass 2 cycles since pushing 16-bit data (2 instances of 8-bit)
         }

         ctx.regs.pc = ctx.fetchedData;
         Emulator.EmuCycle(1);
      }
   }

   /// <summary>
   /// Process Jump (JP) instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcJP(CPUContext ctx)
   {
      GotoAddr(ctx, ctx.fetchedData, false);
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

   /// <summary>
   /// Process XOR instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcXOR(CPUContext ctx)
   {
      ctx.regs.a ^= (byte)ctx.fetchedData;
      CPUSetFlags(ctx, ctx.regs.a == 0, false, false, false);
   }

   /// <summary>
   /// Process Disable Interrupts (DI) instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcDI(CPUContext ctx)
   {
      ctx.intMasterEnabled = false;
   }

   /// <summary>
   /// Process stack pop (POP) instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcPOP(CPUContext ctx)
   {
      UInt16 hi = (UInt16)Stack.Pop();
      Emulator.EmuCycle(1);
      UInt16 lo = (UInt16)Stack.Pop();
      Emulator.EmuCycle(1);

      UInt16 n = (ushort)((hi << 8) | lo);

      CPUUtil.CPUSetReg(ctx.CurrInst.reg1, n);

      if (ctx.CurrInst.reg1 == RegType.RT_AF)
      {
         CPUUtil.CPUSetReg(ctx.CurrInst.reg1, (UInt16)(n & 0xFFF0));
      }
   }

   /// <summary>
   /// Process stack push (PUSH) instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcPUSH(CPUContext ctx)
   {
      UInt16 hi = (UInt16)(CPUUtil.CPUReadReg(ctx.CurrInst.reg1) & 0xFF);
      Emulator.EmuCycle(1);
      Stack.Push((byte)hi);

      UInt16 lo = (UInt16)(CPUUtil.CPUReadReg(ctx.CurrInst.reg1) & 0xFF);
      Emulator.EmuCycle(1);
      Stack.Push((byte)lo);

      Emulator.EmuCycle(1);
   }

   /// <summary>
   /// return the function pointer corresponding to the Instruction type
   /// </summary>
   /// <param name="type">type of instruction</param>
   public static IN_PROC InstGetProcessor(InType type)
   {
      return GetProc(type);
   }
}
