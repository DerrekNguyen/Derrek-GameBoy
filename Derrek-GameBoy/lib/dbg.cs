using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class DBG
{
   private static char[] DBGMsg = new char[1024];
   private static int MsgSize = 0;

   public static void Update()
   {
      if (Bus.BusRead(0xFF02) == 0x81)
      {
         byte c = Bus.BusRead(0xFF01);
         DBGMsg[MsgSize++] = (char)c;
         Bus.BusWrite(0xFF02, 0);
      }
   } 

   public static void Print()
   {
      if (MsgSize > 0)
      {
         string msg = new string(DBGMsg, 0, MsgSize);
         Console.WriteLine($"DBG: {msg}");
      }
   }
}
