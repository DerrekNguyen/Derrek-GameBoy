using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class IO 
{
   private static byte[] SerialData = new byte[2];

   public static byte IORead(UInt16 address)
   {
      if (address == 0xFF01)
      {
         return SerialData[0];
      } else if (address == 0xFF02)
      {
         return SerialData[1];
      }

      Console.WriteLine($"UNSUPPORTED BusRead({address:X4})");
      return 0;
   }

   public static void IOWrite(UInt16 address, byte value)
   {
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
   }
}

