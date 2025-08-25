using System;

public class WaveChannel
{
   public bool _channelEnabled = false;
   private byte NR30, NR31, NR32, NR33, NR34;
   public byte[] waveTable = new byte[32];
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
            0x00 => 0,    // Mute
            0x20 => 1,    // 100%
            0x40 => 2,    // 50%
            0x60 => 3,    // 25%
            _ => 0
         };
      }
   }

   public LengthCounter _lengthCounter = new LengthCounter(256); // 256 for wave channel
   public PhaseTimer _timer = new PhaseTimer(true); // true for wave channel

   public byte Read(UInt16 address)
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

   public void Write(UInt16 address, byte value)
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

            _timer.frequency = (ushort)((_timer.frequency & 0x700) | value);
            break;

         case 0xFF1E:
            NR34 = value;

            _timer.frequency = (UInt16)((_timer.frequency & 0x00FF) | ((value & 0x07) << 8));
            _lengthCounter.enabled = (value & 0b01000000) != 0;
            if ((value & 0x80) != 0) Trigger(DACEnabled);
            break;
         default:
            break;
      }
   }

   public void Trigger(bool dacEnabled)
   {
      if (!dacEnabled)
      {
         _channelEnabled = false;
         return;
      }

      _channelEnabled = true;

      _lengthCounter.Trigger();
      _timer.Trigger();
   }

   // TODO: Sample and tick methods
}
