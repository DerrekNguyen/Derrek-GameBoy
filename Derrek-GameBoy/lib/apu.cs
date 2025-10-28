using System;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

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
   public static byte leftVolume => (byte)((NR50 & 0x70) >> 4);
   public static byte rightVolume => (byte)(NR50 & 0x07);
   public static byte leftEnable => (byte)(NR51 & 0xF0);
   public static byte rightEnable => (byte)(NR51 & 0x0F);

   public static PulseChannel1 Channel1 = new PulseChannel1();
   public static PulseChannel2 Channel2 = new PulseChannel2();
   public static WaveChannel Channel3 = new WaveChannel();
   public static NoiseChannel Channel4 = new NoiseChannel();
   public static FrameSequencer _frameSequencer = new();
   public static int frameSequencerCountDown = 8192;
   public static int downSampleCounter = 95; // (4194304 / 44100) ≈ 95

   public static byte[] readOrValue =
      [0x80,0x3f,0x00,0xff,0xbf,
      0xff,0x3f,0x00,0xff,0xbf,
      0x7f,0xff,0x9f,0xff,0xbf,
      0xff,0xff,0x00,0x00,0xbf,
      0x00,0x00,0x70];

   public const int sampleSize = 4096; // Size of the audio sample buffer
   public static float[] mainBuffer = new float[sampleSize];
   public static int bufferFillAmount = 0;
   public static SDL2.SDL.SDL_AudioSpec audioSpec;
   public static bool enabled = false; 

   public static void Init()
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
      if (address == 0xFF26)
      {
         // Writing to NR52 can only clear the sound enable bit (bit 7)
         NR52 &= (byte)(data & 0x80);
      }

      if (!enabled)
      {
         Channel1.ClearRegisters();
         Channel2.ClearRegisters();
         Channel3.ClearRegisters();
         Channel4.ClearRegisters();
         NR50 = 0;
         NR51 = 0;
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
         if ((data & 0x80) == 0x80)
         {
            if (!enabled)
            {
               // Powering on the APU
               _frameSequencer.step = 0;
               Channel1._dutyCycle.waveDuty = 0;
               Channel2._dutyCycle.waveDuty = 0;
               Channel3.sampleByte = 0;
               enabled = true;
            }
         }
         NR52 = data;
      }
      else if (address >= 0xFF30 && address <= 0xFF3F)
      {
         Channel3.Write(address, data);
      }
   }

   public static byte Read(UInt16 address)
   {
      byte output = 0xFF;
      if (address == 0xFF26)
      {
         // Audio master control (NR52)
         byte status = NR52;
         if (Channel1._channelEnabled) status |= 0x01;
         if (Channel2._channelEnabled) status |= 0x02;
         if (Channel3._channelEnabled) status |= 0x04;
         if (Channel4._channelEnabled) status |= 0x08;
         output = status;
      }
      else if (address >= 0xFF10 && address <= 0xFF14)
      {
         output = Channel1.Read(address);
      }
      else if (address >= 0xFF15 && address <= 0xFF19)
      {
         output = Channel2.Read(address);
      }
      else if (address >= 0xFF1A && address <= 0xFF1E)
      {
         output = Channel3.Read(address);
      }
      else if (address >= 0xFF1F && address <= 0xFF23)
      {
         output = Channel4.Read(address);
      }
      else if (address >= 0xFF24 && address <= 0xFF26)
      {
         switch (address)
         {
            case 0xFF24:
               output =  NR50;
               break;
            case 0xFF25:
               output =  NR51;
               break;
            case 0xFF26:
               Common.BIT_SET(ref NR52, 0, (sbyte)(Channel1._channelEnabled ? 1 : 0));
               Common.BIT_SET(ref NR52, 1, (sbyte)(Channel2._channelEnabled ? 1 : 0));
               Common.BIT_SET(ref NR52, 2, (sbyte)(Channel3._channelEnabled ? 1 : 0));
               Common.BIT_SET(ref NR52, 3, (sbyte)(Channel4._channelEnabled ? 1 : 0));
               output = NR52;
               break;
            default:
               output = 0xFF;
               break;
         }
      }
      else if (address >= 0xFF30 && address <= 0xFF3F)
      {
         output =  Channel3.Read(address);
      }
      if (address >= 0xFF10 && address <= 0xFF26)
      {
         output |= readOrValue[address - 0xFF10];
      }

      return output;
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

         // Left
         float bufferIn0 = 0;
         float bufferIn1 = 0;
         int volume = (128 * leftVolume) / 7; // Scale 0-7 to 0-128
         unsafe
         {
            if ((leftEnable & 0x10) != 0)
            {
               bufferIn1 = ((float)Channel1.Sample()) / 100;
               float* dst = &bufferIn0;
               float* src = &bufferIn1;
               SDL2.SDL.SDL_MixAudioFormat(
                  (nint)(byte*)(dst),
                  (nint)(byte*)(src),
                  SDL2.SDL.AUDIO_F32SYS,
                  sizeof(float),
                  volume
               );
            }
            if ((leftEnable & 0x20) != 0)
            {
               bufferIn1 = ((float)Channel2.Sample()) / 100;
               float* dst = &bufferIn0;
               float* src = &bufferIn1;
               SDL2.SDL.SDL_MixAudioFormat(
                  (nint)(byte*)(dst),
                  (nint)(byte*)(src),
                  SDL2.SDL.AUDIO_F32SYS,
                  sizeof(float),
                  volume
               );
            }
            if ((leftEnable & 0x40) != 0)
            {
               bufferIn1 = ((float)Channel3.Sample()) / 100;
               float* dst = &bufferIn0;
               float* src = &bufferIn1;
               SDL2.SDL.SDL_MixAudioFormat(
                  (nint)(byte*)(dst),
                  (nint)(byte*)(src),
                  SDL2.SDL.AUDIO_F32SYS,
                  sizeof(float),
                  volume
               );
            }
            if ((leftEnable & 0x80) != 0)
            {
               bufferIn1 = ((float)Channel4.Sample()) / 100;
               float* dst = &bufferIn0;
               float* src = &bufferIn1;
               SDL2.SDL.SDL_MixAudioFormat(
                  (nint)(byte*)(dst),
                  (nint)(byte*)(src),
                  SDL2.SDL.AUDIO_F32SYS,
                  sizeof(float),
                  volume
               );
            }
         }
         mainBuffer[bufferFillAmount] = bufferIn0;

         // Right
         bufferIn0 = 0;
         volume = (128 * rightVolume) / 7; // Scale 0-7 to 0-128
         unsafe
         {
            if ((rightEnable & 0x01) != 0)
            {
               bufferIn1 = ((float)Channel1.Sample()) / 100;
               float* dst = &bufferIn0;
               float* src = &bufferIn1;
               SDL2.SDL.SDL_MixAudioFormat(
                  (nint)(byte*)(dst),
                  (nint)(byte*)(src),
                  SDL2.SDL.AUDIO_F32SYS,
                  sizeof(float),
                  volume
               );
            }
            if ((rightEnable & 0x02) != 0)
            {
               bufferIn1 = ((float)Channel2.Sample()) / 100;
               float* dst = &bufferIn0;
               float* src = &bufferIn1;
               SDL2.SDL.SDL_MixAudioFormat(
                  (nint)(byte*)(dst),
                  (nint)(byte*)(src),
                  SDL2.SDL.AUDIO_F32SYS,
                  sizeof(float),
                  volume
               );
            }
            if ((rightEnable & 0x04) != 0)
            {
               bufferIn1 = ((float)Channel3.Sample()) / 100;
               float* dst = &bufferIn0;
               float* src = &bufferIn1;
               SDL2.SDL.SDL_MixAudioFormat(
                  (nint)(byte*)(dst),
                  (nint)(byte*)(src),
                  SDL2.SDL.AUDIO_F32SYS,
                  sizeof(float),
                  volume
               );
            }
            if ((rightEnable & 0x08) != 0)
            {
               bufferIn1 = ((float)Channel4.Sample()) / 100;
               float* dst = &bufferIn0;
               float* src = &bufferIn1;
               SDL2.SDL.SDL_MixAudioFormat(
                  (nint)(byte*)(dst),
                  (nint)(byte*)(src),
                  SDL2.SDL.AUDIO_F32SYS,
                  sizeof(float),
                  volume
               );
            }
         }
         mainBuffer[bufferFillAmount + 1] = bufferIn0;

         bufferFillAmount += 2;
      }

      if (bufferFillAmount >= sampleSize)
      {
         bufferFillAmount = 0;
         while (SDL2.SDL.SDL_GetQueuedAudioSize(1) > sampleSize * sizeof(float))
         {
            SDL2.SDL.SDL_Delay(1); // Wait for the audio buffer to have space
         }
         unsafe
         {
            fixed (float* p = mainBuffer)
            {
               SDL2.SDL.SDL_QueueAudio(1, (IntPtr)p, sampleSize * sizeof(float));
            }
         }
      }
   }
}
