using System;

// linear feedback shift register for noise generation
public class LFSR
{
   public UInt16 lfsr = 0x7FFF; // 15 bits, initialized to all 1s
   public bool widthMode = false; // false: 15-bit, true: 7-bit
   public int divisorCode = 0; // 0-7
   public int[] divisor = [8, 16, 32, 48, 64, 80, 96, 112];

   public void Tick()
   {
      bool setHigh = ((lfsr & 0b1) ^ ((lfsr >> 1) & 0b1)) != 0;
      lfsr >>= 1;
      if (setHigh)
         lfsr |= 0x4000; // Set the 15th bit
      else
         lfsr &= 0x3FFF; // Clear the 15th bit
      if (widthMode)
      {
         // Also set/clear the 7th bit
         if (setHigh)
            lfsr |= 0x40;
         else
            lfsr &= 0xBF;
      }
   }

   public bool Output()
   {
      return (lfsr & 0x1) == 0;
   }
}

public class NoiseChannel
{
   private byte NR41, NR42, NR43, NR44;
   public bool _channelEnabled = false;
   public bool DACEnabled
   {
      get => (NR42 & 0xF8) != 0;
   }

   public UInt16 timer;
   public byte clockShift;
   public LengthCounter _lengthCounter = new LengthCounter(64);
   public Envelope _envelope = new Envelope();
   public LFSR _lfsr = new LFSR();

   public byte Read(UInt16 address)
   {
      return address switch
      {
         0xFF20 => NR41,
         0xFF21 => NR42,
         0xFF22 => NR43,
         0xFF23 => NR44,
         _ => 0xFF,
      };
   }

   public void Write(UInt16 address, byte value)
   {
      switch (address)
      {
         case 0xFF20:
            NR41 = value;

            _lengthCounter.Load((byte)(value & 0b00111111));
            break;

         case 0xFF21:
            NR42 = value;

            _envelope.startingVolume = (byte)((value >> 4) & 0b00001111);
            _envelope.configuredDirection = ((value >> 3) & 0b1) != 0;
            _envelope.configuredPeriod = (byte)(value & 0b00000111);
            break;

         case 0xFF22:
            NR43 = value;

            _lfsr.divisorCode = value & 0x7;
            _lfsr.widthMode = (value & 0x8) == 0x8;
            clockShift = (byte)((value >> 4) & 0xF);
            break;

         case 0xFF23:
            NR44 = value;

            _lengthCounter.enabled = ((value >> 6) & 0b1) != 0;
            if ((value & 0x80) == 0x80) Trigger();
            break;
      }
   }

   public void Tick()
   {
      if (--timer <= 0)
      {
         timer = (UInt16)(_lfsr.divisor[_lfsr.divisorCode] << clockShift);
         _lfsr.Tick();
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
      timer = (UInt16)(_lfsr.divisor[_lfsr.divisorCode] << clockShift);
      _envelope.Trigger();
      _lfsr.lfsr = 0x7FFF;
   }

   public byte Sample()
   {
      if (_channelEnabled && DACEnabled && _lfsr.Output())
      {
         return _envelope.volume;
      }
      return 0;
   }
}