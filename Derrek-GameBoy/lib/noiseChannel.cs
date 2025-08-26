using System;

public class NoiseChannel
{
   private byte NR41, NR42, NR43, NR44;
   public bool _channelEnabled = false;

   public LengthCounter _lengthCounter = new LengthCounter(64); // 64 for noise channel
   public Envelope _envelope = new Envelope();

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

            // TODO: random amplitude
            break;
      }
   }
}