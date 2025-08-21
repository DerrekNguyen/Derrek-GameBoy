using System;

public abstract class SquareChannel
{
   // Duty   Waveform    Ratio
   // -------------------------
   // 0      00000001    12.5%
   // 1      10000001    25%
   // 2      10000111    50%
   // 3      01111110    75%
   public byte[] dutyCycles = new byte[4]
   {
      0b00000001,
      0b10000001,
      0b10000111,
      0b01111110
   };

   public abstract byte Read(UInt16 address);
   public abstract void Write(UInt16 address, byte value);
}

public class SquareChannel1 : SquareChannel
{
   private byte NR10, NR11, NR12, NR13, NR14;
   public override byte Read(UInt16 address)
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

   public override void Write(UInt16 address, byte value)
   {
      switch (address)
      {
         case 0xFF10:
            NR10 = value;
            break;

         case 0xFF11:
            NR11 = value;
            break;

         case 0xFF12:
            NR12 = value;
            break;

         case 0xFF13:
            NR13 = value;
            break;

         case 0xFF14:
            NR14 = value;
            break;
      }
   }
}