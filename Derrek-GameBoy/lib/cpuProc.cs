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
         InType.IN_JR => ProcJR,
         InType.IN_CALL => ProcCALL,
         InType.IN_RST => ProcRST,
         InType.IN_RET => ProcRET,
         InType.IN_RETI => ProcRETI,
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
            Emulator.EmuCycle(2); // pass 2 cycles since pushing 16-bit data (2 instances of 8-bit)
            Stack.Push16(ctx.regs.pc);
         }

         ctx.regs.pc = address;
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
   /// Process Jump relative (JR) instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcJR(CPUContext ctx)
   {
      sbyte rel = (sbyte)(ctx.fetchedData & 0xFF);
      UInt16 addr = (UInt16)(ctx.regs.pc + rel);
      GotoAddr(ctx, addr, false);
   }

   /// <summary>
   /// Process CALL instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcCALL(CPUContext ctx)
   {
      GotoAddr(ctx, ctx.fetchedData, true);
   }

   /// <summary>
   /// Process Restart (RST) instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcRST(CPUContext ctx)
   {
      GotoAddr(ctx, ctx.CurrInst.param, true);
   }

   /// <summary>
   /// Process Return (RET) instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcRET(CPUContext ctx)
   {
      if (ctx.CurrInst.cond != CondType.CT_NONE)
      {
         Emulator.EmuCycle(1);
      }

      if (CheckCondition(ctx))
      {
         byte lo = Stack.Pop();
         Emulator.EmuCycle(1);
         byte hi = Stack.Pop();
         Emulator.EmuCycle(1);

         UInt16 n = (UInt16)(hi << 8 | lo);
         ctx.regs.pc = n;

         Emulator.EmuCycle(1);
      }
   }

   /// <summary>
   /// Process Returning from interrupt (RETI) instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcRETI(CPUContext ctx)
   {
      ctx.intMasterEnabled = true;
      ProcRET(ctx);
   }

   /// <summary>
   /// Helper function for ProcXOR. Set the bits of the flag register in 'ctx' according to flags z, n, h, c
   /// </summary>
   private static void CPUSetFlags(CPUContext ctx, sbyte z, sbyte n, sbyte h, sbyte c)
   {
      if (z != -1)
      {
         Common.BIT_SET(ref ctx.regs.f, 7, z);
      }

      if (n != -1)
      {
         Common.BIT_SET(ref ctx.regs.f, 6, n);
      }

      if (h != -1)
      {
         Common.BIT_SET(ref ctx.regs.f, 5, h);
      }

      if (c != -1)
      {
         Common.BIT_SET(ref ctx.regs.f, 4, c);
      }
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
         }
         else
         {
            Bus.BusWrite(ctx.memDest, (byte)ctx.fetchedData);
         }

         Emulator.EmuCycle(1);

         return;
      }

      if (ctx.CurrInst.mode == AddrMode.AM_HL_SPR)
      {
         sbyte hflag = (sbyte)(((CPUUtil.CPUReadReg(ctx.CurrInst.reg2) & 0xF) + (ctx.fetchedData & 0xF)) >= 0x10 ? 1 : 0);
         sbyte cflag = (sbyte)(((CPUUtil.CPUReadReg(ctx.CurrInst.reg2) & 0xFF) + (ctx.fetchedData & 0xFF)) >= 0x100 ? 1 : 0);
         CPUSetFlags(ctx, 0, 0, hflag, cflag);

         CPUUtil.CPUSetReg(ctx.CurrInst.reg1, (ushort)(CPUUtil.CPUReadReg(ctx.CurrInst.reg2) + (sbyte)ctx.fetchedData));

         return;
      }

      // Load the fetched data into register
      CPUUtil.CPUSetReg(ctx.CurrInst.reg1, ctx.fetchedData);
   }

   /// <summary>
   /// Process XOR instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcXOR(CPUContext ctx)
   {
      ctx.regs.a ^= (byte)ctx.fetchedData;
      CPUSetFlags(ctx, (sbyte)(ctx.regs.a == 0 ? 1 : 0), 0, 0, 0);
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
