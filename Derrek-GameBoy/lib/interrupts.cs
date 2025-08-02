using NUnit.Framework.Internal.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum InterruptType {
   IT_VBLANK = 1,
   IT_LCD_STAT = 2,
   IT_TIMER = 4,
   IT_SERIAL = 8,
   IT_JOYPAD = 16
};

public static class Interrupt
{
   private static void IntHandle(CPUContext ctx, UInt16 address)
   {
      Stack.Push16(ctx, ctx.regs.pc);
      ctx.regs.pc = address;
   }

   private static bool IntCheck(CPUContext ctx, UInt16 address, InterruptType it)
   {
      int mask = (int)it;
      if ((ctx.intFlags & mask) != 0 && (ctx.ieRegister & mask) != 0)
      {
         IntHandle(ctx, address);
         ctx.intFlags &= (byte)~mask;
         ctx.halted = false;
         ctx.intMasterEnabled = false;

         return true;
      }

      return false;
   }
   public static void CPURequestInterrupt(InterruptType t)
   {

   }

   public static void CPUHandleInterrupt(CPUContext ctx)
   {
      if (IntCheck(ctx, 0x40, InterruptType.IT_VBLANK))
      {

      }
      else if (IntCheck(ctx, 0x48, InterruptType.IT_LCD_STAT))
      {

      }
      else if (IntCheck(ctx, 0x50, InterruptType.IT_TIMER))
      {

      }
      else if (IntCheck(ctx, 0x58, InterruptType.IT_SERIAL))
      {

      }
      else if (IntCheck(ctx, 0x60, InterruptType.IT_JOYPAD))
      {

      }
   }
}