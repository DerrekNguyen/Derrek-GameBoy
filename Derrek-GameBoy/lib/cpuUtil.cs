using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class CPUUtil
{
   public static UInt16 Reverse(UInt16 n)
   {
      return (UInt16)(((n & 0xFF00) >> 8) | ((n & 0x00FF) << 8));
   }

   public static UInt16 CPUReadReg(RegType rt)
   {
      switch (rt)
      {
         case RegType.RT_A: return CPU._context.regs.a;
         case RegType.RT_F: return CPU._context.regs.f;
         case RegType.RT_B: return CPU._context.regs.b;
         case RegType.RT_C: return CPU._context.regs.c;
         case RegType.RT_D: return CPU._context.regs.d;
         case RegType.RT_E: return CPU._context.regs.e;
         case RegType.RT_H: return CPU._context.regs.h;
         case RegType.RT_L: return CPU._context.regs.l;

         case RegType.RT_AF: return Reverse(CPU._context.regs.a);
         case RegType.RT_BC: return Reverse(CPU._context.regs.b);
         case RegType.RT_DE: return Reverse(CPU._context.regs.d);
         case RegType.RT_HL: return Reverse(CPU._context.regs.h);

         case RegType.RT_PC: return CPU._context.regs.pc;
         case RegType.RT_SP: return CPU._context.regs.sp;

         default:
            return (UInt16)0;
      }
   }

   public static void CPUSetReg(RegType rt, UInt16 value)
   {
      switch (rt)
      {
         case RegType.RT_A: CPU._context.regs.a = (byte)(value & 0xFF); break;
         case RegType.RT_F: CPU._context.regs.f = (byte)(value & 0xFF); break;
         case RegType.RT_B: CPU._context.regs.b = (byte)(value & 0xFF); break;
         case RegType.RT_C: CPU._context.regs.c = (byte)(value & 0xFF); break;
         case RegType.RT_D: CPU._context.regs.d = (byte)(value & 0xFF); break;  
         case RegType.RT_E: CPU._context.regs.e = (byte)(value & 0xFF); break;
         case RegType.RT_H: CPU._context.regs.h = (byte)(value & 0xFF); break;
         case RegType.RT_L: CPU._context.regs.l = (byte)(value & 0xFF); break;

         case RegType.RT_AF: CPU._context.regs.a = (byte)Reverse(value); break;
         case RegType.RT_BC: CPU._context.regs.b = (byte)Reverse(value); break;
         case RegType.RT_DE: CPU._context.regs.d = (byte)Reverse(value); break;
         case RegType.RT_HL: CPU._context.regs.h = (byte)Reverse(value); break;

         case RegType.RT_PC: CPU._context.regs.pc = (byte)value; break;
         case RegType.RT_SP: CPU._context.regs.sp = (byte)value; break;

         default: break;
      }
   }
}
