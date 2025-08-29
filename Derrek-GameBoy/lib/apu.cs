using System;
using System.Reflection.Metadata.Ecma335;

public class PhaseTimer
{
   public bool wave = false; // true for wave channel
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
   public bool enabled;

   // Configured values via registers
   public byte startingVolume;
   public byte configuredPeriod;
   public bool configuredDirection; // true for increment, false for decrement

   public void Clock()
   {
      if (period == 0) return;

      counter--;
      if (counter <= 0)
      {
         counter = period;
         if (counter == 0) counter = 8;

         if (enabled && period > 0)
         {
            if (direction)
            {
               if (volume < 15) volume++;
            }
            else
            {
               if (volume > 0) volume--;
            }
         }

         if (volume == 0 || volume == 15)
            enabled = false; // Stop if volume hits limits
      }
   }

   public void Trigger()
   {
      volume = startingVolume;
      direction = configuredDirection;
      period = configuredPeriod;
      enabled = true;

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
   public int step;

   public void Tick(
      PulseChannel1 ch1,
      PulseChannel2 ch2,
      WaveChannel ch3,
      NoiseChannel ch4
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
            ch3.ClockLengthCounter();
            ch4.ClockLengthCounter();
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
            ch4.ClockEnvelope();
            break;
      }

      step++;
      if (step > 7) step = 0;
   }

   public bool LengthWillClockThisStep()
   {
      return (step % 2 == 0);
   }
}

public static class APU
{
   public static byte NR50, NR51, NR52; // Sound control registers

   public static PulseChannel1 Channel1 = new PulseChannel1();
   public static PulseChannel2 Channel2 = new PulseChannel2();
   public static WaveChannel Channel3 = new WaveChannel();
   public static NoiseChannel Channel4 = new NoiseChannel();
   public static FrameSequencer _frameSequencer = new();
   public static int frameSequencerCountDown = 8192;
   public static int downSampleCounter = 95; // (4194304 / 44100) ≈ 95

   public const int sampleSize = 4096; // Size of the audio sample buffer
   public static SDL2.SDL.SDL_AudioSpec audioSpec;

   static APU()
   {
      audioSpec = new SDL2.SDL.SDL_AudioSpec
      {
         freq = 44100,
         format = SDL2.SDL.AUDIO_F32SYS,
         channels = 2,
         samples = sampleSize, // Adjust as needed
         callback = null,
         userdata = IntPtr.Zero
      };

      SDL2.SDL.SDL_AudioSpec obtainedSpec;
      SDL2.SDL.SDL_OpenAudio(ref audioSpec, out obtainedSpec);
      SDL2.SDL.SDL_PauseAudio(0); // Start audio playback
   }

   public static void Write(UInt16 address, byte data)
   {
      bool enabled = (NR52 & 0x80) != 0;
      if (!enabled)
      {
         Channel1.ClearRegisters();
         Channel2.ClearRegisters();
         Channel3.ClearRegisters();
         Channel4.ClearRegisters();
      }
      else
      {
         if (address >= 0xFF10 && address <= 0xFF14)
         {
            Channel1.Write(address, data);
         }
         else if (address >= 0xFF15 && address <= 0xFF19)
         {
            Channel2.Write(address, data);
         }
         else if (address >= 0xFF1A && address <= 0xFF1E)
         {
            Channel3.Write(address, data);
         }
         else if (address >= 0xFF1F && address <= 0xFF23)
         {
            Channel4.Write(address, data);
         }
         else if (address >= 0xFF24 && address <= 0xFF25)
         {
            switch (address)
            {
               case 0xFF24:
                  // Master volume & Vin panning (NR50)
                  NR50 = data;
                  break;

               case 0xFF25:
                  // Sound panning (NR51)
                  NR51 = data;
                  break;
            }
         }
      }

      // Control/status registers
      if (address == 0xFF26)
      {
         // Audio master control (NR52)
         NR52 = data;
      }
      else if (address >= 0xFF30 && address <= 0xFF3F)
      {
         Channel3.Write(address, data);
      }
   }

   public static void Read()
   {
      // TODO
   }

   public static void Tick()
   {
      // Ticks every 8192 CPU cycles (on a 512 scale, 4194304/512 = 8192).
      if (--frameSequencerCountDown <= 0)
      {
         frameSequencerCountDown = 8192;
         _frameSequencer.Tick(Channel1, Channel2, Channel3, Channel4);
      }

      Channel1.Tick();
      Channel2.Tick();
      Channel3.Tick();
      Channel4.Tick();

      // downsample to 44100 Hz
      if (--downSampleCounter <= 0)
      {
         downSampleCounter = 95;

         // TODO: Mix and output audio samples
      }
   }
}
