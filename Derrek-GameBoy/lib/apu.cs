using System;

public enum ChannelType
{
   SQUARE_1,
   SQUARE_2,
   WAVE,
   NOISE,
   N_CHANNELS
}

public static class SquareChannel1
{
   private static byte NR10, NR11, NR12, NR13, NR14;
   public static byte Read(UInt16 address)
   {
      switch (address)
      {
         case 0xFF10: return NR10;
         case 0xFF11: return NR11;
         case 0xFF12: return NR12;
         case 0xFF13: return NR13;

         // Return bit 6 (length enable), 2, 1 and 0 (LS3B of period value)
         case 0xFF14: return (byte)(NR14 & 0x47); 

         default: return 0xFF;
      }
   }
}

public static class APU
{

}
