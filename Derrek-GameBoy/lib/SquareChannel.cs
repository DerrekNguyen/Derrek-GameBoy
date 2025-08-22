using System;

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
   public bool enabled = false;
   public byte counter = 64;

   public void Load(byte length)
   {
      counter = (byte)(64 - length);
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
         counter = 64;
   }
} 

public class PulsePhaseTimer
{
   public byte phase = 0;
   public UInt16 counter = 0;
   public UInt16 frequency = 0;

   public void Tick()
   {
      counter--;
      if (counter == 0)
      {
         // Reload counter and move one step in the waveform
         counter = (UInt16)(4 * (2048 - (frequency & 0x7FF)));
         phase = (byte)((phase + 1) & 7);
      }
   }

   public void Trigger()
   {
      counter = (UInt16)(4 * (2048 - (frequency & 0x7FF)));

      // Triggering does not reset phase!
   }
}

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

public abstract class PulseChannel
{
   public bool _channelEnabled;
   public PulsePhaseTimer _timer = new();
   public DutyCycle _dutyCycle = new();
   public LengthCounter _lengthCounter = new();
   public Envelope _envelope = new();


   public void Tick()
   {
      _timer.Tick();
   }

   public void ClockLengthCounter()
   {
      _lengthCounter.Clock(ref _channelEnabled);
   }

   public void ClockEnvelope()
   {
      _envelope.Clock();
   }

   public void Trigger()
   {
      // Note: If the DAC is disabled, triggering should not re-enable the channel
      _channelEnabled = true;

      _timer.Trigger();
      _lengthCounter.Trigger();
      _envelope.Trigger();
   }

   public abstract byte Sample();

   public abstract byte Read(UInt16 address);
   public abstract void Write(UInt16 address, byte value);
}

public class PulseChannel1 : PulseChannel
{
   private byte NR10, NR11, NR12, NR13, NR14;

   public override byte Sample()
   {
      if (!_channelEnabled) return 0;
 
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
         case 0xFF13: return NR13;

         // Return bit 6 (length enable), 2, 1 and 0 (LS3B of period value)
         case 0xFF14: return (byte)(NR14 & 0x47);

         default: return 0xFF;
      }
   }

   public override void Write(UInt16 address, byte value)
   {
      switch (address)
      {
         case 0xFF10:
            NR10 = value;

            // TODO: Implement sweep.
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
            break;

         case 0xFF13:
            NR13 = value;

            _timer.frequency = (UInt16)(_timer.frequency & 0x0700| (UInt16)value);
            break;

         case 0xFF14:
            NR14 = value;

            _timer.frequency = (UInt16)(_timer.frequency & 0x00FF | (UInt16)((value & 0x07) << 8));
            _lengthCounter.enabled = (value & 0b01000000) != 0;
            // TODO: Implement trigger (bit 7 (LSB) / 0 (MSB))
            break;
      }
   }
}