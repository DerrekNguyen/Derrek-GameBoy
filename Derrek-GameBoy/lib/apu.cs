using System;
using System.Reflection.Metadata.Ecma335;

public class PhaseTimer
{
   public bool wave = false; // true for wave channel, false for pulse channels
   public byte phase = 0;
   public UInt16 counter = 0;
   public UInt16 frequency = 0;
   private UInt16 reloadPeriod = 0;

   public PhaseTimer(bool isWaveChannel)
   {
      wave = isWaveChannel;
   }

   public UInt16 CalculatePeriod()
   {
      if (!wave)
         return (UInt16)(4 * (2048 - (frequency & 0x7FF)));
      else
         return (UInt16)(2 * (2048 - (frequency & 0x7FF)));
   }

   public void Tick()
   {
      counter--;
      if (counter <= 0)
      {
         reloadPeriod = CalculatePeriod();
         counter = reloadPeriod;
         // Reload counter and move one step in the waveform

         if (!wave)
         {
            phase = (byte)((phase + 1) & 7);
         } 
         else
         {
            phase = (byte)((phase + 1) & 31);
         }
      }
   }

   public void Trigger()
   {
      reloadPeriod = CalculatePeriod();
      counter = reloadPeriod;

      if (wave)
         phase = 0; // Wave channel resets phase on trigger
   }
}

public class Envelope
{
   // Active counter values
   public byte volume;
   public byte counter;
   public byte period;
   public bool direction; // true for increment, false for decrement

   // Configured values via registers
   public byte startingVolume;
   public byte configuredPeriod;
   public bool configuredDirection; // true for increment, false for decrement

   public void Clock()
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

   public void Trigger()
   {
      volume = startingVolume;
      direction = configuredDirection;
      period = configuredPeriod;

      counter = (period == 0) ? (byte)0 : period;
   }
}

public class LengthCounter
{
   public bool enabled;
   public int limit; // Max length (64 for pulse and noise, 256 for wave)
   public int counter;

   public LengthCounter(int lengthLimit)
   {
      enabled = false;
      limit = lengthLimit;
      counter = lengthLimit;
   }

   public void Load(byte length)
   {
      counter = (byte)(limit - length);
   }

   public bool Clock(ref bool channelEnabled)
   {
      if (!enabled || counter == 0) return false;

      counter--;
      if (counter == 0)
         channelEnabled = false;

      return true;
   }

   public void Trigger()
   {
      if (counter == 0)
         counter = limit;
   }
}

public class FrameSequencer
{
   private int step = 0;

   public void Tick(
      PulseChannel1 ch1,
      PulseChannel2 ch2
      //WaveChannel ch3,
      //NoiseChannel ch4
   )
   {
      switch (step)
      {
         case 0:
         case 2:
         case 4:
         case 6:
            ch1.ClockLengthCounter();
            ch2.ClockLengthCounter();
            //ch3.ClockLengthCounter();
            //ch4.ClockLengthCounter();
            break;
      }

      switch (step)
      {
         case 2:
         case 6:
            ch1._sweep.Clock(ref ch1._timer.frequency); // only channel 1 has sweep
            break;
      }

      switch (step)
      {
         case 7:
            ch1.ClockEnvelope();
            ch2.ClockEnvelope();
            //ch4.ClockEnvelope();
            break;
      }
   }

   public bool LengthWillClockThisStep()
   {
      return (step % 2 == 0);
   }
}

public static class APU
{
   public static PulseChannel1 Channel1 = new PulseChannel1();
   public static PulseChannel2 Channel2 = new PulseChannel2();
   public static FrameSequencer _frameSequencer = new();
   private static int _frameSequencerStep = 0;

   public static void Tick()
   {
      Channel1.Tick();
      Channel2.Tick();
      //Channel3.Tick();
      //Channel4.Tick();

      if (++_frameSequencerStep >= 8192)
      {
         _frameSequencerStep = 0;
         _frameSequencer.Tick(Channel1, Channel2);
      }

      //// Optionally: sample mixer at your chosen rate
      //// e.g., downsample to 44100 Hz
      //sampleClock++;
      //if (sampleClock >= cyclesPerSample)
      //{
      //   sampleClock -= cyclesPerSample;
      //   Mixer.PushSample(ch1.Sample(), ch2.Sample(), ch3.Sample(), ch4.Sample());
      //}
   }
}
