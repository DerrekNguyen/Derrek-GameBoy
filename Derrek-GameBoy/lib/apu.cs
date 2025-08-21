using System;
using System.Reflection.Metadata.Ecma335;

public enum ChannelType
{
   SQUARE_1,
   SQUARE_2,
   WAVE,
   NOISE,
   N_CHANNELS
}

public static class APU
{
   private static SquareChannel Channel1 = new SquareChannel1();

   public static void Tick()
   {
      // Add additional ticks as time goes on.

      PulsePhaseTimer.Tick();
   }

   public static void FrequencySweep()
   {
      byte unit = Channel1.Read(0xFF10);


   }
}
