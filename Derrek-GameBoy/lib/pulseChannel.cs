using System;

public class DutyCycle
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

   public byte waveDuty;
}

public class Sweep
{
   public byte period;
   public bool negate; // true for decrement, false for increment
   public byte shift;
   public UInt16 shadow;

   public byte periodLoad;
   public bool enabled;

   public void Clock(ref UInt16 timerFrequency)
   {
      if (period == 0 || !enabled) return;

      period--;
      if (period == 0)
      {
         period = periodLoad;
         if (period == 0) period = 8; // Reset to default if period is zero
      }
      else
      {
         period = periodLoad;
         if (period == 0) period = 8;
         if (enabled && periodLoad > 0)
         {
            UInt16 newFreq = CalculateSweep(true);
            if (shift != 0 && newFreq <= 0x7FF)
            {
               timerFrequency = newFreq;
               if (CalculateSweep(false) > 0x7FF)
               {
                  enabled = false; // Disable if overflow occurs
               }
            }
            else if (newFreq > 0x7FF)
            {
               enabled = false; // Disable if overflow occurs
            }
         }
      }
   }

   public UInt16 CalculateSweep(bool apply)
   {
      UInt16 newFreq = (UInt16)(shadow >> shift);
      if (negate)
      {
         newFreq = (UInt16)(shadow - newFreq);
      }
      else
      {
         newFreq = (UInt16)(shadow + newFreq);
      }

      // Overflow check
      if (newFreq > 0x7FF)
      {
         return 0xFFFF; // Indicate overflow
      }

      // if apply is true, we update the shadow frequency
      if (apply && shift > 0)
      {
         shadow = newFreq;
      }

      return (UInt16)newFreq;
   }

   public void Trigger(UInt16 frequency) // Timer's frequency
   {
      shadow = frequency;
      period = periodLoad;
      if (period == 0) period = 8;
      enabled = period != 0 || shift != 0;

      // If the sweep shift is non-zero, frequency calculation and the overflow check are performed immediately.
      if (shift != 0)
      {
         if (CalculateSweep(false) > 0x7FF)
         {
            enabled = false; // Disable if overflow occurs
         }
      }
   }
}

public abstract class PulseChannel
{
   public bool _channelEnabled;
   public PhaseTimer _timer = new(false);
   public DutyCycle _dutyCycle = new();
   public LengthCounter _lengthCounter = new(64);
   public Envelope _envelope = new();

   public void ClockLengthCounter()
   {
      _lengthCounter.Clock(ref _channelEnabled);
   }

   public void ClockEnvelope()
   {
      _envelope.Clock();
   }

   public void Trigger(bool DACEnabled)
   {
      // Note: If the DAC is disabled, triggering should not re-enable the channel
      if (!DACEnabled)
      {
         _channelEnabled = false;
         return;
      }

      _channelEnabled = true;

      _timer.Trigger();
      _lengthCounter.Trigger();
      _envelope.Trigger();

      // length & trigger coinciding
      if (_lengthCounter.enabled &&
          APU._frameSequencer.LengthWillClockThisStep())
      {
         _lengthCounter.Clock(ref _channelEnabled);
      }
   }

   public abstract byte Sample();

   public abstract byte Read(UInt16 address);
   public abstract void Write(UInt16 address, byte value);
   public abstract void Tick();
}

public class PulseChannel1 : PulseChannel
{
   private byte NR10, NR11, NR12, NR13, NR14;
   public bool DACEnabled {
     get => (NR12 & 0xF8) != 0;
   }
   public Sweep _sweep = new();

   public override byte Sample()
   {
      if (!_channelEnabled || !DACEnabled) return 0;
 
      // GameBoy bits are MSB, meaning bit 0 means the MSB. Therefore, we need to reverse the duty bit (0 => 7, 1 => 6, etc.)
      byte waveformStep = (byte)((_dutyCycle.dutyCycles[_dutyCycle.waveDuty] >> (7 - _timer.phase)) & 0b1);
      byte volume = _envelope.volume;

      return (byte)(waveformStep * volume);
   }

   public override byte Read(UInt16 address)
   {
      switch (address)
      {
         case 0xFF10: return NR10;
         case 0xFF11: return NR11;
         case 0xFF12: return NR12;
         case 0xFF13: return 0xFF; // write-only
         case 0xFF14: return (byte)(NR14 & 0b1100_0111);

         default: return 0xFF;
      }
   }

   public override void Write(UInt16 address, byte value)
   {
      switch (address)
      {
         case 0xFF10:
            NR10 = value;

            _sweep.shift = (byte)(value & 0x7);
            _sweep.negate = ((value >> 3) & 0b1) != 0; // 0 = Addition (period increases), 1 = Subtraction (period decreases)
            _sweep.periodLoad = (byte)((value >> 4) & 0x7);
            break;

         case 0xFF11:
            NR11 = value;

            _dutyCycle.waveDuty = (byte)((value >> 6) & 0b00000011);
            _lengthCounter.Load((byte)(value & 0b00111111));   
            break;

         case 0xFF12:
            NR12 = value;

            _envelope.startingVolume = (byte)((value >> 4) & 0b00001111);
            _envelope.configuredDirection = ((value >> 3) & 0b1) != 0;
            _envelope.configuredPeriod = (byte)(value & 0b00000111);

            if (!DACEnabled) _channelEnabled = false;
            break;

         case 0xFF13:
            NR13 = value;

            _timer.frequency = (UInt16)((_timer.frequency & 0x0700) | value);
            break;

         case 0xFF14:
            NR14 = value;

            _timer.frequency = (UInt16)((_timer.frequency & 0x00FF) | ((value & 0x07) << 8));
            _lengthCounter.enabled = (value & 0b01000000) != 0;
            if ((value & 0x80) != 0) Trigger(DACEnabled);
            break;
      }
   }

   public override void Tick()
   {
      _timer.Tick();
   }
}

public class PulseChannel2 : PulseChannel
{
   private byte NR21, NR22, NR23, NR24;
   public bool DACEnabled
   {
      get => (NR22 & 0xF8) != 0;
   }

   public override byte Sample()
   {
      if (!_channelEnabled || !DACEnabled) return 0;

      // GameBoy bits are MSB, meaning bit 0 means the MSB. Therefore, we need to reverse the duty bit (0 => 7, 1 => 6, etc.)
      byte waveformStep = (byte)((_dutyCycle.dutyCycles[_dutyCycle.waveDuty] >> (7 - _timer.phase)) & 0b1);
      byte volume = _envelope.volume;

      return (byte)(waveformStep * volume);
   }

   public override byte Read(UInt16 address)
   {
      switch (address)
      {
         case 0xFF16: return NR21;
         case 0xFF17: return NR22;
         case 0xFF18: return 0xFF; // write-only
         case 0xFF19: return (byte)(NR24 & 0b1100_0111);

         default: return 0xFF;
      }
   }

   public override void Write(UInt16 address, byte value)
   {
      switch (address)
      {
         case 0xFF16:
            NR21 = value;

            _dutyCycle.waveDuty = (byte)((value >> 6) & 0b00000011);
            _lengthCounter.Load((byte)(value & 0b00111111));
            break;

         case 0xFF17:
            NR22 = value;

            _envelope.startingVolume = (byte)((value >> 4) & 0b00001111);
            _envelope.configuredDirection = ((value >> 3) & 0b1) != 0;
            _envelope.configuredPeriod = (byte)(value & 0b00000111);

            if (!DACEnabled) _channelEnabled = false;
            break;

         case 0xFF18:
            NR23 = value;

            _timer.frequency = (UInt16)((_timer.frequency & 0x0700) | value);
            break;

         case 0xFF19:
            NR24 = value;

            _timer.frequency = (UInt16)((_timer.frequency & 0x00FF) | ((value & 0x07) << 8));
            _lengthCounter.enabled = (value & 0b01000000) != 0;
            if ((value & 0x80) != 0) Trigger(DACEnabled);
            break;
      }
   }

   public override void Tick()
   {
      _timer.Tick();
   }
}