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
   private static PulseChannel Channel1 = new PulseChannel1();

   public static void Tick()
   {
      // Add additional ticks as time goes on.
      Channel1.Tick();
   }

   public static void FrequencySweep()
   {
      byte unit = Channel1.Read(0xFF10);


   }
}
