using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class IO 
{
   private static byte[] SerialData = new byte[2];

   private static byte ly = 0;

   public static byte IORead(UInt16 address)
   {
      if (address == 0xFF00)
      {
         return GamePad.GetOutput();
      }
      if (address == 0xFF01)
      {
         return SerialData[0];
      } 
      else if (address == 0xFF02)
      {
         return SerialData[1];
      } 
      else if (Common.BETWEEN(address, 0xFF04, 0xFF07))
      {
         return Timer.Read(address);
      } 
      else if (address == 0xFF0F) {
         return CPU._context.intFlags;
      }
      else if (Common.BETWEEN(address, 0xFF40, 0xFF4B))
      {
         return LCD.Read(address);
      }

      //Console.WriteLine($"UNSUPPORTED BusRead({address:X4})");
      return 0;
   }

   public static void IOWrite(UInt16 address, byte value)
   {
      if (address == 0xFF00)
      {
         GamePad.SetSel(value);
         return;
      }
      if (address == 0xFF01)
      {
         SerialData[0] = value;
         return;
      }
      else if (address == 0xFF02)
      {
         SerialData[1] = value;
         return;
      }
      else if (Common.BETWEEN(address, 0xFF04, 0xFF07))
      {
         Timer.Write(address, value);
         return;
      }
      else if (address == 0xFF0F)
      {
         CPU._context.intFlags = value;
         return;
      }
      else if (Common.BETWEEN(address, 0xFF40, 0xFF4B))
      {
         LCD.Write(address, value);
      }

      //Console.WriteLine($"UNSUPPORTED BusWrite({address:X4})");
   }
}

