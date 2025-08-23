using System;
using System.Reflection.Metadata.Ecma335;

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
