using Microsoft.ApplicationInsights.Extensibility.Implementation;
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
         InType.IN_NONE => ProcNONE,
         InType.IN_NOP => ProcNOP,
         InType.IN_LD => ProcLD,
         InType.IN_LDH => ProcLDH,
         InType.IN_JP => ProcJP,
         InType.IN_DI => ProcDI,
         InType.IN_AND => ProcAND,
         InType.IN_OR => ProcOR,
         InType.IN_CP => ProcCP,
         InType.IN_XOR => ProcXOR,
         InType.IN_CB => ProcCB,
         InType.IN_POP => ProcPOP,
         InType.IN_PUSH => ProcPUSH,
         InType.IN_JR => ProcJR,
         InType.IN_CALL => ProcCALL,
         InType.IN_RST => ProcRST,
         InType.IN_RET => ProcRET,
         InType.IN_RETI => ProcRETI,
         InType.IN_INC => ProcINC,
         InType.IN_DEC => ProcDEC,
         InType.IN_ADD => ProcADD,
         InType.IN_SUB => ProcSUB,
         InType.IN_ADC => ProcADC,
         InType.IN_SBC => ProcSBC,
         _ => ProcNONE
      };
   }

   private static RegType[] RTLookUp = new RegType[8]
   {
      RegType.RT_B,
      RegType.RT_C,
      RegType.RT_D,
      RegType.RT_E,
      RegType.RT_H,
      RegType.RT_L,
      RegType.RT_HL,
      RegType.RT_A,
   };

   public static RegType DecodeReg(byte reg) 
   {
      return reg < RTLookUp.Length ? RTLookUp[reg] : RegType.RT_NONE;
   }

   public static void ProcNONE(CPUContext ctx)
   {
      Console.WriteLine("Invalid Instruction");
      Environment.Exit(-7);
   }

   public static void ProcNOP(CPUContext ctx)
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
   /// Helper function. Check whether register is 16-bit 
   /// </summary>
   /// <param name="rt">register</param>
   /// <returns>true if register is 16-bit, false otherwise</returns>
   private static bool Is16Bit(RegType rt)
   {
      return (rt >= RegType.RT_AF);
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
         if (Is16Bit(ctx.CurrInst.reg2))
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
   /// Process AND instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcAND(CPUContext ctx)
   {
      ctx.regs.a &= (byte)ctx.fetchedData;
      CPUSetFlags(ctx, (sbyte)(ctx.regs.a == 0 ? 1 : 0), 0, 1, 0);
   }

   /// <summary>
   /// Process OR instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcOR(CPUContext ctx)
   {
      ctx.regs.a |= (byte)(ctx.fetchedData & 0xFF);
      CPUSetFlags(ctx, (sbyte)(ctx.regs.a == 0 ? 1 : 0), 0, 0, 0);
   }

   /// <summary>
   /// Process compare (CP) instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcCP(CPUContext ctx)
   {
      int n = (int)ctx.regs.a - (int)ctx.fetchedData;

      CPUSetFlags(ctx, 
         (sbyte)(n == 0 ? 1 : 0), 
         1, 
         (sbyte)(((int)ctx.regs.a & 0x0F) - ((int)ctx.fetchedData & 0x0F) < 0 ? 1 : 0), 
         (sbyte)(n < 0 ? 1 : 0));
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
   /// Process CB prefix (CB) instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcCB(CPUContext ctx)
   {
      byte op = (byte)ctx.fetchedData;
      RegType reg = DecodeReg((byte)(op & 0b111));

      byte bit = (byte)((op >> 3) & 0b111);
      byte bitOp = (byte)((op >> 6) & 0b111);
      byte regVal = CPUUtil.CPUReadReg8(reg);

      Emulator.EmuCycle(1);

      if (reg == RegType.RT_HL)
      {
         Emulator.EmuCycle(2);
      }

      switch(bitOp)
      {
         // BIT
         case 1:
            CPUSetFlags(ctx, (sbyte)((regVal & (1 << bit)) == 0 ? 0 : 1), 0, 1, -1);
            break;

         // RST
         case 2:
            regVal &= (byte)~(1 << bit);
            CPUUtil.CPUSetReg8(reg, regVal);
            break;

         // SET
         case 3:
            regVal |= (byte)(1 << bit);
            CPUUtil.CPUSetReg8(reg, regVal);
            break;
      }

      bool flagC = CPU.CPU_FLAG_C;

      //TODO: part 8
      switch (bit)
      {
         // RLC
         case 0:

            return;

         // RRC
         case 1:

            return;

         // RL
         case 2:

            return;

         // RR
         case 3:

            return;

         // SLA
         case 4:

            return;

         // SRA
         case 5:

            return;

         // SWAP
         case 6:

            return;

         // SRL
         case 7:

            return;
      }
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
      UInt16 lo = (UInt16)Stack.Pop();
      Emulator.EmuCycle(1);
      UInt16 hi = (UInt16)Stack.Pop();
      Emulator.EmuCycle(1);

      UInt16 n = (UInt16)((hi << 8) | lo);

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
      UInt16 hi = (UInt16)((CPUUtil.CPUReadReg(ctx.CurrInst.reg1) >> 8) & 0xFF);
      Emulator.EmuCycle(1);
      Stack.Push((byte)hi);

      UInt16 lo = (UInt16)(CPUUtil.CPUReadReg(ctx.CurrInst.reg1) & 0xFF);
      Emulator.EmuCycle(1);
      Stack.Push((byte)lo);

      Emulator.EmuCycle(1);
   }

   /// <summary>
   /// Process increment (INC) instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcINC(CPUContext ctx)
   {
      UInt16 val = (UInt16)(CPUUtil.CPUReadReg(ctx.CurrInst.reg1) + 1);

      if (Is16Bit(ctx.CurrInst.reg1))
      {
         Emulator.EmuCycle(1);
      }

      if (ctx.CurrInst.reg1 == RegType.RT_HL && ctx.CurrInst.mode == AddrMode.AM_MR)
      {
         val = (UInt16)(Bus.BusRead(CPUUtil.CPUReadReg(ctx.CurrInst.reg1)) + 1);
         val &= 0xFF;
         Bus.BusWrite(CPUUtil.CPUReadReg(ctx.CurrInst.reg1), (byte)val);
      } else
      {
         CPUUtil.CPUSetReg(ctx.CurrInst.reg1, val);
         val = CPUUtil.CPUReadReg(ctx.CurrInst.reg1); 
      }

      if ((ctx.curOpcode & 0x03) == 0x03)
      {
         return;
      }

      CPUSetFlags(ctx, (sbyte)(val == 0 ? 1 : 0), 0, (sbyte)((val & 0x0F) == 0 ? 1 : 0), -1);
   }

   /// <summary>
   /// Process decrement (DEC) instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcDEC(CPUContext ctx)
   {
      UInt16 val = (UInt16)(CPUUtil.CPUReadReg(ctx.CurrInst.reg1) - 1);

      if (Is16Bit(ctx.CurrInst.reg1))
      {
         Emulator.EmuCycle(1);
      }

      if (ctx.CurrInst.reg1 == RegType.RT_HL && ctx.CurrInst.mode == AddrMode.AM_MR)
      {
         val = (UInt16)(Bus.BusRead(CPUUtil.CPUReadReg(ctx.CurrInst.reg1)) - 1);
         Bus.BusWrite(CPUUtil.CPUReadReg(ctx.CurrInst.reg1), (byte)val);
      }
      else
      {
         CPUUtil.CPUSetReg(ctx.CurrInst.reg1, val);
         val = CPUUtil.CPUReadReg(ctx.CurrInst.reg1);
      }

      if ((ctx.curOpcode & 0x03) == 0x0B)
      {
         return;
      }

      CPUSetFlags(ctx, (sbyte)(val == 0 ? 1 : 0), 1, (sbyte)((val & 0x0F) == 0x0F ? 1 : 0), -1);
   }

   /// <summary>
   /// Process ADD instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcADD(CPUContext ctx)
   {
      UInt32 val = (UInt32)(CPUUtil.CPUReadReg(ctx.CurrInst.reg1) + ctx.fetchedData);

      bool Is16 = Is16Bit(ctx.CurrInst.reg1);

      if (Is16)
      {
         Emulator.EmuCycle(1);
      }

      if (ctx.CurrInst.reg1 == RegType.RT_SP)
      {
         val = (UInt32)(CPUUtil.CPUReadReg(ctx.CurrInst.reg1) + (sbyte)ctx.fetchedData);
      }

      sbyte z = (sbyte)((val & 0xFF) == 0 ? 1 : 0);
      sbyte h = (sbyte)((CPUUtil.CPUReadReg(ctx.CurrInst.reg1) & 0xF) + (ctx.fetchedData & 0xF) >= 0x10 ? 1 : 0);
      sbyte c = (sbyte)((CPUUtil.CPUReadReg(ctx.CurrInst.reg1) & 0xFF) + (ctx.fetchedData & 0xFF) >= 0x100 ? 1 : 0);

      if (Is16)
      {
         z = -1;
         h = (sbyte)((CPUUtil.CPUReadReg(ctx.CurrInst.reg1) & 0xFFF) + (ctx.fetchedData & 0xFFF) >= 0x1000 ? 1 : 0);
         c = (sbyte)((UInt32)CPUUtil.CPUReadReg(ctx.CurrInst.reg1) + (UInt32)ctx.fetchedData >= 0x10000 ? 1 : 0);
      }

      if (ctx.CurrInst.reg1 == RegType.RT_SP)
      {
         z = 0;
         h = (sbyte)((CPUUtil.CPUReadReg(ctx.CurrInst.reg1) & 0xF) + (ctx.fetchedData & 0xF) >= 0x10 ? 1 : 0);
         c = (sbyte)((CPUUtil.CPUReadReg(ctx.CurrInst.reg1) & 0xFF) + (ctx.fetchedData & 0xFF) > 0x100 ? 1 : 0);
      }

      CPUUtil.CPUSetReg(ctx.CurrInst.reg1, (UInt16)(val & 0xFFFF));
      CPUSetFlags(ctx, z, 0, h, c);
   }

   /// <summary>
   /// Process add carry (ADC) instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcADC(CPUContext ctx)
   {
      UInt16 u = ctx.fetchedData;
      UInt16 a = ctx.regs.a;
      UInt16 c = (UInt16)(CPU.CPU_FLAG_C ? 1 : 0);

      ctx.regs.a = (byte)((u + a + c) & 0xFF);

      CPUSetFlags(
         ctx,
         (sbyte)(ctx.regs.a == 0 ? 1 : 0),
         0,
         (sbyte)((u & 0xF) + (a & 0xF) + c > 0xF ? 1 : 0),
         (sbyte)((u + a + c) > 0xFF ? 1 : 0)
         );
   }

   /// <summary>
   /// Process subtract (SUB) instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcSUB(CPUContext ctx)
   {
      UInt16 value = (UInt16)(CPUUtil.CPUReadReg(ctx.CurrInst.reg1) - ctx.fetchedData);

      sbyte z = (sbyte)(value == 0 ? 1 : 0);
      sbyte h = (sbyte)((CPUUtil.CPUReadReg(ctx.CurrInst.reg1) & 0xF) - (ctx.fetchedData & 0xF) < 0 ? 1 : 0);
      sbyte c = (sbyte)(CPUUtil.CPUReadReg(ctx.CurrInst.reg1) - ctx.fetchedData < 0 ? 1 : 0);

      CPUUtil.CPUSetReg(ctx.CurrInst.reg1, value);
      CPUSetFlags(ctx, z, 1, h, c);
   }

   /// <summary>
   /// Process subtract carry (SBC) instructions
   /// </summary>
   /// <param name="ctx">The instance of CPUContext</param>
   public static void ProcSBC(CPUContext ctx)
   {
      byte value = (byte)(ctx.fetchedData + (CPU.CPU_FLAG_C ? 1 : 0));

      sbyte z = (sbyte)(CPUUtil.CPUReadReg(ctx.CurrInst.reg1) - value == 0 ? 1 : 0);
      sbyte h = (sbyte)
         ((CPUUtil.CPUReadReg(ctx.CurrInst.reg1) & 0xF) 
         - (ctx.fetchedData & 0xF) - (CPU.CPU_FLAG_C ? 1 : 0) < 0 ? 1 : 0);
      sbyte c = (sbyte)
         (CPUUtil.CPUReadReg(ctx.CurrInst.reg1) 
         - ctx.fetchedData - (CPU.CPU_FLAG_C ? 1 : 0) < 0 ? 1 : 0);

      CPUUtil.CPUSetReg(ctx.CurrInst.reg1, (UInt16)(CPUUtil.CPUReadReg(ctx.CurrInst.reg1) - value));
      CPUSetFlags(ctx, z, 1, h, c);
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
