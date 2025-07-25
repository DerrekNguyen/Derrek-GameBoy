using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
    STACK

    SP=0xDFFF

    MEMORY:
    0xDFF7: 00
    0xDFF8: 00
    0xDFF9: 00
    0xDFFA: 00
    0xDFFB: 00
    0xDFFC: 00
    0xDFFD: 00
    0xDFFE: 00
    0xDFFF: 00 <- SP

    PUSH 0x55

    SP-- = 0xDFFE
    MEMORY[0xDFFE] = 0x55

    MEMORY:
    0xDFF7: 00
    0xDFF8: 00
    0xDFF9: 00
    0xDFFA: 00
    0xDFFB: 00
    0xDFFC: 00
    0xDFFD: 00
    0xDFFE: 55 <- SP
    0xDFFF: 00

    PUSH 0x77

    SP-- = 0xDFFD
    MEMORY[0xDFFD] = 0x77

    MEMORY:
    0xDFF7: 00
    0xDFF8: 00
    0xDFF9: 00
    0xDFFA: 00
    0xDFFB: 00
    0xDFFC: 00
    0xDFFD: 77 <- SP
    0xDFFE: 55
    0xDFFF: 00

    val = POP

    val = MEMORY[0xDFFD] = 0x77
    SP++ = 0xDFFE

    MEMORY:
    0xDFF7: 00
    0xDFF8: 00
    0xDFF9: 00
    0xDFFA: 00
    0xDFFB: 00
    0xDFFC: 00
    0xDFFD: 77 
    0xDFFE: 55 <- SP
    0xDFFF: 00


    PUSH 0x88

    SP-- = 0xDFFD
    MEMORY[0xDFFD] = 0x88

    MEMORY:
    0xDFF7: 00
    0xDFF8: 00
    0xDFF9: 00
    0xDFFA: 00
    0xDFFB: 00
    0xDFFC: 00
    0xDFFD: 88 <- SP
    0xDFFE: 55 
    0xDFFF: 00
*/

/// <summary>
/// The instruction stack and all relevant operations
/// </summary>
public static class Stack
{
   /// <summary>
   /// Push 8-bit 'data' into the stack and decrement the stack pointer
   /// </summary>
   public static void Push(byte data)
   {
      CPU._context.regs.sp--;
      Bus.BusWrite(CPU._context.regs.sp, data);
   }

   /// <summary>
   /// Push 16-bit 'data' into the stack and decrement the stack pointer
   /// </summary>
   public static void Push16(UInt16 data)
   {
      Push((byte)((data >> 8) & 0xFF));
      Push((byte)(data & 0xFF));
   }

   /// <summary>
   /// Pop and return the 8-bit data at the top of the stack. Increment the stack pointer
   /// </summary>
   public static byte Pop()
   {
      return Bus.BusRead(CPU._context.regs.sp++);
   }

   /// <summary>
   /// Pop two 8-bit data at the top of the stack, combine and return as 16-bit. Increment the stack pointer.
   /// </summary>
   public static UInt16 Pop16()
   {
      byte hi = Pop();
      byte lo = Pop();
      return (UInt16)(hi << 8 | lo);
   }
}
