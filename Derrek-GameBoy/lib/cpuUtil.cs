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

         case RegType.RT_AF: return (ushort)((CPU._context.regs.a << 8) | CPU._context.regs.f);
         case RegType.RT_BC: return (ushort)((CPU._context.regs.b << 8) | CPU._context.regs.c);
         case RegType.RT_DE: return (ushort)((CPU._context.regs.d << 8) | CPU._context.regs.e);
         case RegType.RT_HL: return (ushort)((CPU._context.regs.h << 8) | CPU._context.regs.l);

         case RegType.RT_PC: return CPU._context.regs.pc;
         case RegType.RT_SP: return CPU._context.regs.sp;

         default:
            Console.WriteLine($"ERR INVALID REG16: {rt}");
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

         case RegType.RT_AF:
            ushort AF = value;
            CPU._context.regs.a = (byte)(AF >> 8);
            CPU._context.regs.f = (byte)(AF & 0xFF);
            break;

         case RegType.RT_BC:
            ushort BC = value;
            CPU._context.regs.b = (byte)(BC >> 8);
            CPU._context.regs.c = (byte)(BC & 0xFF);
            break;

         case RegType.RT_DE:
            ushort DE = value;
            CPU._context.regs.d = (byte)(DE >> 8);
            CPU._context.regs.e = (byte)(DE & 0xFF);
            break;

         case RegType.RT_HL:
            ushort HL = value;
            CPU._context.regs.h = (byte)(HL >> 8);
            CPU._context.regs.l = (byte)(HL & 0xFF);
            break;

         case RegType.RT_PC: CPU._context.regs.pc = value; break;
         case RegType.RT_SP: CPU._context.regs.sp = value; break;

         default: break;
      }
   }

   public static byte CPUReadReg8(RegType rt)
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
         case RegType.RT_HL: return Bus.BusRead(CPUReadReg(RegType.RT_HL));

         default:
            Console.WriteLine($"ERR INVALID REG8: {rt}");
            return 0;
      }
   }

   public static void CPUSetReg8(RegType rt, byte value)
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
         case RegType.RT_HL:
            Bus.BusWrite(CPUReadReg(RegType.RT_HL), value);
            break;

         default:
            Console.WriteLine($"ERR INVALID REG8: {rt}"); 
            break;
      }
   }
}
