using System;

public class WaveChannel
{
   public bool _channelEnabled = false;
   private byte NR30, NR31, NR32, NR33, NR34;
   public byte[] waveTable = new byte[16]; // 32 4-bit samples
   public bool DACEnabled
   {
      get => (NR30 & 0x80) != 0;
   }
   public int volume
   {
      get
      {
         return (NR32 & 0x60) switch
         {
            0x00 => -1,    // Mute
            0x20 => 0,    // 100%
            0x40 => 1,    // 50%
            0x60 => 2,    // 25%
            _ => -1
         };
      }
   }
   private byte outputVolume;

   public LengthCounter _lengthCounter = new LengthCounter(256); // 256 for wave channel
   public PhaseTimer _timer = new PhaseTimer(true); // true for wave channel

   public byte Read(UInt16 address)
   {
      if (address >= 0xFF30 && address <= 0xFF3F)
      {
         // Wave RAM
         return waveTable[address - 0xFF30];
      }
      else
      {
         switch (address)
         {
            case 0xFF1A:
               return NR30;
            case 0xFF1B:
               return 0xFF; // NR31 is write-only
            case 0xFF1C:
               return NR32;
            case 0xFF1D:
               return 0xFF; // NR33 is write-only
            case 0xFF1E:
               return NR34;
            default:
               return 0xFF;
         }
      }
   }

   public void Write(UInt16 address, byte value)
   {
      if (address >= 0xFF30 && address <= 0xFF3F)
      {
         // Wave RAM
         waveTable[address - 0xFF30] = value;
         return;
      }
      else
      {
         switch (address)
         {
            case 0xFF1A:
               NR30 = (byte)(value & 0x80);
               if (!DACEnabled)
               {
                  // If DAC is disabled, the channel is also disabled
                  _channelEnabled = false;
               }
               break;

            case 0xFF1B:
               NR31 = value;

               _lengthCounter.Load(value);
               break;

            case 0xFF1C:
               NR32 = (byte)(value & 0x60);
               break;

            case 0xFF1D:
               NR33 = value;

               _timer.frequency = (ushort)((_timer.frequency & 0x0700) | value);
               break;

            case 0xFF1E:
               NR34 = value;

               _timer.frequency = (UInt16)((_timer.frequency & 0x00FF) | ((value & 0x07) << 8));
               _lengthCounter.enabled = (value & 0b01000000) == 0x40;
               if ((value & 0x80) == 0x80) Trigger();
               break;
            default:
               break;
         }
      }
   }

   public void Trigger()
   {
      if (!DACEnabled)
      {
         _channelEnabled = false;
         return;
      }

      _channelEnabled = true;
      _lengthCounter.Trigger();
      _timer.Trigger();
   }

   public void Tick()
   {
      _timer.Tick();
      if (_channelEnabled && DACEnabled)
      {
         int position = _timer.phase / 2; // Each byte contains two 4-bit samples
         byte sampleByte = waveTable[position];
         
         if ((_timer.phase & 0x1) == 0)
         {
            sampleByte = (byte)(sampleByte >> 4); // High nibble
         }

         sampleByte &= 0x0F; // Low nibble

         if (volume >= 0)
         {
            sampleByte >>= volume;
         }
         else
         {
            sampleByte = 0; // Mute
         }
         outputVolume = sampleByte;
      }
      else
      {
         outputVolume = 0;
      }
   }

   public byte Sample()
   {
      return outputVolume;
   }
}
