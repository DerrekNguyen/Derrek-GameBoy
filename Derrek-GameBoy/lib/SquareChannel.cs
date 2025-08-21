using System;

public static class Envelope
{
   // Active counter values
   public static byte volume;
   public static byte counter;
   public static byte period;
   public static bool direction; // true for increment, false for decrement

   // Configured values via registers
   public static byte startingVolume;
   public static byte configuredPeriod;
   public static bool configuredDirection; // true for increment, false for decrement

   public static void Clock()
   {
      if (period == 0) return;

      counter--;
      if (counter == 0)
      {
         counter = period;

         if (direction)
         {
            if (volume < 15) volume++;
         }
         else
         {
            if (volume > 0) volume--;
         }
      }
   }

   public static void Trigger()
   {
      volume = startingVolume;
      direction = configuredDirection;
      period = configuredPeriod;

      counter = (period == 0) ? (byte)0 : period;
   }
} 

public static class LengthCounter
{
   public static bool enabled = false;
   public static byte counter = 64;

   public static void Load(byte length)
   {
      counter = (byte)(64 - length);
   }

   public static bool Clock()
   {
      if (!enabled || counter == 0) return false;

      counter--;
      if (counter == 0)
         enabled = false;

      return true;
   }

   public static void Trigger()
   {
      if (counter == 0)
         counter = 64;
   }
} 

public static class PulsePhaseTimer
{
   public static byte phase = 0;
   public static UInt16 counter = 0;
   public static UInt16 frequency = 0;

   public static void Tick()
   {
      counter--;
      if (counter == 0)
      {
         // Reload counter and move one step in the waveform
         counter = (UInt16)(4 * (2048 - (frequency & 0x7FF)));
         phase = (byte)((phase + 1) & 7);
      }
   }

   public static void Trigger()
   {
      counter = (UInt16)(4 * (2048 - (frequency & 0x7FF)));

      // Triggering does not reset phase!
   }
}

public abstract class SquareChannel
{
   /*
   Duty   Waveform    Ratio
   -------------------------
   0      00000001    12.5%
   1      10000001    25%
   2      10000111    50%
   3      01111110    75%
   */
   public byte[] dutyCycles = new byte[4]
   {
      0b00000001,
      0b10000001,
      0b10000111,
      0b01111110
   };

   public void Timer()
   {

   }

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