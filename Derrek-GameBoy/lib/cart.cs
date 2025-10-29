using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct RomHeader
{
   public fixed byte Entry[4];
   public fixed byte Logo[0x30];

   public fixed byte Title[16];

   public UInt16 NewLicenseCode;
   public byte SgbFlag;
   public byte Type;
   public byte RomSize;
   public byte RamSize;
   public byte DestinationCode;
   public byte LicenseCode;
   public byte Version;
   public byte Checksum;
   public UInt16 GlobalChecksum;
}

/// <summary>
/// Real Time Clock (for MBC3)
/// </summary>
public class RTC
{
   // Real time values
   public byte RealSeconds;
   public byte RealMinutes;
   public byte RealHours;

   private UInt16 RealDayCounter;
   public bool RealHalt;
   public bool RealCarry;

   public byte RealDayLower
   {
      get { return (byte)(RealDayCounter & 0xFF); }
      set { RealDayCounter = (UInt16)((RealDayCounter & 0xFF00) | value); }
   }
   public byte RealDayUpper
   {
      get
      {
         byte result = 0;
         result |= (byte)((RealDayCounter >> 8) & 0x01);
         if (RealHalt) result |= 0x40;
         if (RealCarry) result |= 0x80;
         return result;
      }
      set
      {
         RealHalt = (value & 0x40) != 0;
         RealCarry = (value & 0x80) != 0;
         if ((value & 0x01) != 0)
         {
            RealDayCounter |= 0x0100;
         }
         else
         {
            RealDayCounter &= 0xFEFF;
         }
      }
   }
   public UInt16 GetRealDay()
   {
      return (ushort)(RealDayCounter & 0x01FF);
   }

   // Latched values
   public byte LatchSeconds;
   public byte LatchMinutes;
   public byte LatchHours;

   private UInt16 LatchDayCounter;
   public bool LatchHalt;
   public bool LatchCarry;

   public byte LatchDayLower
   {
      get { return (byte)(LatchDayCounter & 0xFF); }
      set { LatchDayCounter = (UInt16)((LatchDayCounter & 0xFF00) | value); }
   }
   public byte LatchDayUpper
   {
      get {
         byte result = 0;
         result |= (byte)((LatchDayCounter >> 8) & 0x01);
         if (LatchHalt) result |= 0x40;
         if (LatchCarry) result |= 0x80;
         return result;
      }
      set
      {
         LatchHalt = (value & 0x40) != 0;
         LatchCarry = (value & 0x80) != 0;
         if ((value & 0x01) != 0)
         {
            LatchDayCounter |= 0x0100;
         } else
         {
            LatchDayCounter &= 0xFEFF;
         }
      }
   }
   public UInt16 GetLatchDay()
   {
      return (UInt16)(LatchDayCounter & 0x01FF);
   }

   // The current time since the last update
   public TimeSpan CurrentTime;

   // Methods
   public void UpdateTimer()
   {
      // We using epoch to calculate elapsed time. That way it can only either go up or stay still
      TimeSpan newTime = DateTime.UtcNow - DateTime.UnixEpoch;
      UInt32 elapsedSeconds = (UInt32)(newTime - CurrentTime).TotalSeconds;

      CurrentTime = newTime;
      // Return if halted, or if we somehow stopped time
      if (elapsedSeconds <= 0 || RealHalt) return;

      RealSeconds = (byte)((RealSeconds + elapsedSeconds) % 60);
      RealMinutes = (byte)((RealMinutes + (elapsedSeconds / 60)) % 60);
      RealHours = (byte)((RealHours + (elapsedSeconds / 3600)) % 24);

      UInt32 daysToAdd = GetRealDay() + elapsedSeconds / 86400;
      
      RealDayLower = (byte)(daysToAdd & 0xFF);
      RealDayUpper = (byte)((daysToAdd >> 8) & 0x01);
      if (RealDayCounter > 511)
      {
         RealDayCounter %= 512;
         RealCarry = true;
      }
   }

   public void LatchClock()
   {
      UpdateTimer();
      LatchSeconds = RealSeconds;
      LatchMinutes = RealMinutes;
      LatchHours = RealHours;
      LatchDayLower = RealDayLower;
      LatchDayUpper = RealDayUpper;
   }
}

public class CartContext
{
   public string Filename = string.Empty;
   public UInt32 RomSize;
   public Byte[] RomData = Array.Empty<Byte>();
   public RomHeader? Header;

   // mbc1 related data
   public bool RamEnabled;
   public bool RamBanking;

   public int RomBankXOffset;
   public byte BankingMode;

   public byte RomBankValue;
   public byte RamBankValue;

   public byte[]? RamBank; // Current selected ram bank
   public byte[][] RamBanks = new byte[16][]; // All ram banks

   // For battery
   public bool Battery; // Has battery
   public bool NeedSave; // Should save battery backup

   // For MBC3 RTC
   public bool timerPresent = false;
   public RTC? _RTC = null;   
   public bool Latch = false; // Checks the last write to the latch register (latch happens from low to high)
}

public static class Cart
{
   private static CartContext _cartContext = new CartContext();

   public static bool CartNeedSave()
   {
      return _cartContext.NeedSave;
   }

   /// <summary>
   /// Verify if cart is MBC1
   /// </summary>
   /// <returns>
   /// true if MBC1, false otherwise
   /// </returns>
   public static bool CartMBC1()
   {
      if (_cartContext.Header is RomHeader header)
      {
         return Common.BETWEEN(header.Type, 1, 3);
      }
      return false;
   }

   /// <summary>
   /// Verify if cart is MBC2
   /// </summary>
   /// <returns>
   /// true if MBC2, false otherwise
   /// </returns>
   public static bool CartMBC2()
   {
      if (_cartContext.Header is RomHeader header)
      {
         return Common.BETWEEN(header.Type, 5, 6);
      }
      return false;
   }

   /// <summary>
   /// Verify if cart is MBC3
   /// </summary>
   /// <returns>
   /// <list type="bullet">
   /// <item><description><c>0</c> — Not MBC3</description></item>
   /// <item><description><c>1</c> — MBC3 without RTC</description></item>
   /// <item><description><c>2</c> — MBC3 with RTC</description></item>
   /// </list>
   /// </returns>
   public static int CartMBC3() 
   {
      int ret = 0;
      if (_cartContext.Header is RomHeader header)
      {
         if (Common.BETWEEN(header.Type, 0x0F, 0x13)) ret++; // MBC3
         if (Common.BETWEEN(header.Type, 0x0F, 0x10)) ret++; // RTC
      }
      return ret;
   }

   public static bool CartBattery()
   {
      if (_cartContext.Header is RomHeader header)
      {
         switch (header.Type)
         {
            case 0x03: // MBC1 + RAM + Battery
            case 0x06: // MBC2 + Battery
            case 0x0F: // MBC3 + Timer + Battery
            case 0x10: // MBC3 + Timer + RAM + Battery
            case 0x13: // MBC3 + RAM + Battery
            case 0x1B: // MBC5 + RAM + Battery
            case 0x1E: // MBC5 + RAM + Battery
               return true;
         }
      }
      return false;
   }

   /// <summary>
   /// 
   /// </summary>
   /// <returns>
   /// - The License Name from the License Code from the cart.
   /// - "UNKNOWN" otherwise.
   /// </returns>
   public static string CartLICName()
   {
      if (_cartContext.Header is not RomHeader header)
      {
         return "UNKNOWN HEADER";
      }

      if (header.NewLicenseCode <= 0xA4)
      {
         if (header.LicenseCode == 0x33)
         {
            // new License Code
            return LookUp.LIC_CODE[(byte)header.NewLicenseCode];
         }

         // old License Code
         return LookUp.LIC_CODE[header.LicenseCode];
      }

      return "UNKNOWN";
   }

   /// <summary>
   /// 
   /// </summary>
   /// <returns>
   /// - The Rom Type from the Header Type from the cart.
   /// - "UNKNOWN" otherwise.
   /// </returns>
   public static string CartTypeName()
   {
      if (_cartContext.Header is not RomHeader header)
      {
         return "UNKNOWN HEADER";
      }

      if (header.Type <= 0x22)
      {
         return LookUp.ROM_TYPES[header.Type];
      }

      return "UNKNOWN";
   }

   public static void CartSetupBanking()
   {
      if (_cartContext.Header is RomHeader header)
      {
         for (int i = 0; i < 16; i++)
         {
            _cartContext.RamBanks[i] = null;

            if ((header.RamSize == 2 && i == 0) ||
                (header.RamSize == 3 && i < 4) ||
                (header.RamSize == 4 && i < 16) ||
                (header.RamSize == 5 && i < 8))
            {
               _cartContext.RamBanks[i] = new byte[0x2000];
            }
         }

         _cartContext.RamBank = _cartContext.RamBanks[0];
         _cartContext.RomBankXOffset = 0x4000; // Rom Bank 1
      }
   }

   /// <summary>
   /// Loads a cart from the file name.
   /// </summary>
   /// <param name="cart">the file name of the cart.</param>
   /// <returns>true/false depending on the success of the process.</returns>
   public static bool CartLoad(string cart)
   {
      // Load cart info
      _cartContext.Filename = cart;
      _cartContext.RomData = File.ReadAllBytes(cart);
      _cartContext.RomSize = (uint)_cartContext.RomData.Length;

      // Load title from file
      byte[] titleBytes = new byte[16];
      unsafe
      {
         fixed (byte* ptr = _cartContext.RomData)
         {
            _cartContext.Header = Marshal.PtrToStructure<RomHeader>((IntPtr)(ptr + 0x100));
         }

         Buffer.BlockCopy(_cartContext.RomData, 0x134, titleBytes, 0, 16);

         if (_cartContext.Header is RomHeader _header)
         {
            for (int i = 0; i < 16; i++)
            {
               _header.Title[i] = (byte)(titleBytes[i]);
            }

            _cartContext.Header = _header;
         }
         else return false;
      }

      // Check if Header exists
      if (_cartContext.Header is not RomHeader header)
      {
         return false;
      }

      _cartContext.Battery = Cart.CartBattery();
      _cartContext.NeedSave = false;

      // Read cart file
      if (!File.Exists(cart))
      {
         Console.WriteLine($"Failed to open: {cart}");
         return false;
      }
      Console.WriteLine($"Opened: {_cartContext.Filename}");

      // Debug Logs
      Console.WriteLine("Cartridge Loaded:");
      Console.WriteLine($"\t Title : {System.Text.Encoding.ASCII.GetString(titleBytes)}");
      Console.WriteLine($"\t Type     : {header.Type:X2} ({CartTypeName()})");
      Console.WriteLine($"\t ROM Size : {32 << header.RomSize} KB");
      Console.WriteLine($"\t RAM Size : {header.RamSize:X2}");
      Console.WriteLine($"\t LIC Code : {header.LicenseCode:X2} ({CartLICName()})");
      Console.WriteLine($"\t ROM Vers : {header.Version:X2}");

      Cart.CartSetupBanking();

      // Verify the checksum
      UInt16 x = 0;
      for (UInt16 i = 0x0134; i <= 0x014C; ++i)
      {
         x = (UInt16)(x - _cartContext.RomData[i] - 1);
      }
      bool passed = (x & 0xFF) == header.Checksum;
      Console.WriteLine($"\t Checksum : {header.Checksum:X2} ({(passed ? "PASSED" : "FAILED")})");

      if (Cart.CartBattery())
      {
         Cart.CartBatteryLoad();
      }

      // RTC for MBC3 (if exists)
      if (Cart.CartMBC3() == 2)
      {
         _cartContext.timerPresent = true;
         _cartContext._RTC = new RTC();
      }

      return true;
   }

   /// <summary>
   /// Load battery from file.<br/>
   /// Structure: [bank0][bank1][bank2]...[bank15][RTC bytes (optional)]
   /// </summary>
   public static void CartBatteryLoad()
   {
      if (_cartContext.RamBank == null)
      {
         return;
      }

      string fn;
      fn = $"{_cartContext.Filename}.battery";

      try
      {
         using (var fs = new FileStream(fn, FileMode.Open, FileAccess.Read))
         {
            for (int i = 0; i < 16; i++)
            {
               if (_cartContext.RamBanks[i] != null)
               {
                  fs.Read(_cartContext.RamBanks[i], 0, 0x2000);
               }
               else
               {
                  fs.Seek(0x2000, SeekOrigin.Current); // skip over that portion
               }
            }

            // Load RTC data if present
            if (Cart.CartMBC3() > 0 && _cartContext.timerPresent && _cartContext._RTC != null)
            {
               _cartContext._RTC.RealSeconds = (byte)fs.ReadByte();
               _cartContext._RTC.RealMinutes = (byte)fs.ReadByte();
               _cartContext._RTC.RealHours = (byte)fs.ReadByte();
               _cartContext._RTC.RealDayLower = (byte)fs.ReadByte();
               _cartContext._RTC.RealDayUpper = (byte)fs.ReadByte();

               _cartContext._RTC.UpdateTimer();
            }
         }
      }
      catch (Exception e)
      {
         Console.WriteLine($"FAILED TO OPEN: {fn}");
         Console.WriteLine(e.Message);
         return;
      }
   }

   /// <summary>
   /// Save battery to file.<br/>
   /// Structure: [bank0][bank1][bank2]...[bank15][RTC bytes (optional)]
   /// </summary>
   public static void CartBatterySave()
   {
      if (_cartContext.RamBank == null)
      {
         return;
      }

      string fn;
      fn = $"{_cartContext.Filename}.battery";

      try
      {
         using (var fs = new FileStream(fn, FileMode.Create, FileAccess.Write))
         {
            for (int i = 0; i < 16; i++)
            {
               if (_cartContext.RamBanks[i] != null)
               {
                  fs.Write(_cartContext.RamBanks[i], 0, 0x2000);
               }
               else
               {
                  // write blank data for missing banks to keep consistent file size
                  fs.Write(new byte[0x2000], 0, 0x2000);
               }
            }


            // Save RTC data if present
            if (Cart.CartMBC3() > 0 && _cartContext.timerPresent && _cartContext._RTC != null)
            {
               fs.WriteByte(_cartContext._RTC.RealSeconds);
               fs.WriteByte(_cartContext._RTC.RealMinutes);
               fs.WriteByte(_cartContext._RTC.RealHours);
               fs.WriteByte(_cartContext._RTC.RealDayLower);
               fs.WriteByte(_cartContext._RTC.RealDayUpper);
            }
         }
      } catch (Exception e)
      {
         Console.WriteLine($"FAILED TO OPEN: {fn}");
         Console.WriteLine(e.Message);
         return;
      }
   }

   /// <summary>
   /// Helper function to validate cart read address.
   /// </summary>
   /// <param name="address"></param>
   /// <returns>"true" if address is within boundary, "false" otherwise</returns>
   private static bool ValidAddress(ushort address)
   {
      return (address >= 0x0000 && address <= 0xBFFF);
   }

   /// <summary>
   /// Helper function to read from MBC1 cart.
   /// </summary>
   /// <param name="address"></param>
   /// <returns>ROM/RAM data from that address</returns>
   private static byte CartMBC1Read(ushort address)
   {
      // External RAM range (A000-BFFF)
      if ((address & 0xE000) == 0xA000)
      {
         if (!_cartContext.RamEnabled || _cartContext.RamBank == null)
            return 0xFF;

         return _cartContext.RamBank[address - 0xA000];
      }

      // ROM Bank (0000-7FFF)
      return _cartContext.RomData[address - 0x4000 + _cartContext.RomBankXOffset];
   }

   /// <summary>
   /// Helper function to read from MBC2 cart.
   /// </summary>
   /// <param name="address"></param>
   /// <returns>ROM/RAM data from that address</returns>
   private static byte CartMBC2Read(ushort address)
   {
      // Built-in RAM range (A000-A1FF)
      if (Common.BETWEEN(address, 0xA000, 0xA1FF))
      {
         if (!_cartContext.RamEnabled || _cartContext.RamBank == null)
            return 0xFF;

         return _cartContext.RamBank[address - 0xA000];
      }

      // ROM Bank (0000-7FFF)
      return _cartContext.RomData[address - 0x4000 + _cartContext.RomBankXOffset];
   }

   /// <summary>
   /// Helper function to read from MBC3 cart.
   /// </summary>
   /// <param name="address"></param>
   /// <returns>ROM/RAM - RTC data from that address</returns>
   private static byte CartMBC3Read(ushort address)
   {
      // RAM Bank or RTC range (A000-BFFF)
      if (Common.BETWEEN(address, 0xA000, 0xBFFF))
      {
         if (_cartContext.RamEnabled)
         {
            // Read from RAM Bank
            if (_cartContext.RamBank != null && _cartContext.RamBankValue < 4)
            {
               return _cartContext.RamBank[address - 0xA000];
            }
            // RTC register
            else if (_cartContext.timerPresent &&
               _cartContext._RTC != null &&
               Common.BETWEEN(_cartContext.RamBankValue, 0x08, 0x0C))
            {
               switch (_cartContext.RamBankValue)
               {
                  case 0x08:
                     // Seconds
                     return _cartContext._RTC.LatchSeconds;
                  case 0x09:
                     // Minutes
                     return _cartContext._RTC.LatchMinutes;
                  case 0x0A:
                     // Hours
                     return _cartContext._RTC.LatchHours;
                  case 0x0B:
                     // Lower 8 bits of Day Counter
                     return _cartContext._RTC.LatchDayLower;
                  case 0x0C:
                     // Upper Day Counter, Half, Carry
                     return _cartContext._RTC.LatchDayUpper;
               }
            }
         }
         return 0xFF;
      }

      // ROM Bank (0000-7FFF)
      return _cartContext.RomData[address - 0x4000 + _cartContext.RomBankXOffset];
   }

   public static byte CartRead(ushort address)
   {
      if (!ValidAddress(address))
         return 0xFF;

      // Direct read if no MBC or address in 0x0000-0x3FFF
      if ((!Cart.CartMBC1() && !Cart.CartMBC2() && Cart.CartMBC3() == 0) || address < 0x4000)
         return _cartContext.RomData[address];

      // MBC1 Read
      if (Cart.CartMBC1())
         return CartMBC1Read(address);

      // MBC2 Read
      if (Cart.CartMBC2())
         return Cart.CartMBC2Read(address);

      // MBC3 Read
      return Cart.CartMBC3Read(address);
   }

   public static void CartWrite(UInt16 address, byte value)
   {
      if (Cart.CartMBC1())
      {
         if (address < 0x2000)
         {
            _cartContext.RamEnabled = ((value & 0xF) == 0xA);
         }

         if ((address & 0xE000) == 0x2000)
         {
            // Rom bank number
            if (value == 0)
            {
               value = 1;
            }

            value &= 0b11111;

            _cartContext.RomBankValue = value;
            _cartContext.RomBankXOffset = 0x4000 * _cartContext.RomBankValue;
         }

         if ((address & 0xE000) == 0x4000)
         {
            // Ram bank number
            _cartContext.RamBankValue = (byte)(value & 0b11);

            if (_cartContext.RamBanking)
            {
               if (_cartContext.NeedSave)
               {
                  Cart.CartBatterySave();
               }

               _cartContext.RamBank = _cartContext.RamBanks[_cartContext.RamBankValue];   
            }
         }

         if ((address & 0xE000) == 0x6000)
         {
            // Banking mode select
            _cartContext.BankingMode = (byte)(value & 1);
            _cartContext.RamBanking = _cartContext.BankingMode != 0;

            if (_cartContext.RamBanking)
            {
               _cartContext.RamBank = _cartContext.RamBanks[_cartContext.RamBankValue];
            }
         }

         if ((address & 0xE000) == 0xA000)
         {
            if (!_cartContext.RamEnabled)
               return;
            if (_cartContext.RamBank == null)
               return;

            _cartContext.RamBank[address - 0xA000] = value;


            if (_cartContext.Battery)
            {
               _cartContext.NeedSave = true;
            }
         }
      }

      else if (Cart.CartMBC2())
      {
         // Bit 8 is clear
         if ((address & 0x0100) == 0)
         {
            _cartContext.RamEnabled = ((value & 0xF) == 0xA);
         }
         // Bit 8 is set
         else
         {
            // Rom bank number
            if (value == 0)
            {
               value = 1;
            }

            value &= 0x0F;

            _cartContext.RomBankValue = value;
            _cartContext.RomBankXOffset = 0x4000 * _cartContext.RomBankValue;
         }
      }

      // TODO: MBC3 Write
      else if (Cart.CartMBC3() > 0)
      {
         // RAM and Timer Enable
         if (Common.BETWEEN(address, 0x0000, 0x1FFF))
         {
            if (_cartContext.Battery && _cartContext.NeedSave && ((value & 0x0F) != 0x0A) && _cartContext.RamEnabled)
            {
               Cart.CartBatterySave();
            }
            _cartContext.RamEnabled = ((value & 0x0F) == 0x0A);
         }

         // ROM Bank Number
         else if (Common.BETWEEN(address, 0x2000, 0x3FFF))
         {
            if (value == 0)
            {
               value = 1;
            }

            value &= 0x7F;
            _cartContext.RomBankValue = value;
            _cartContext.RomBankXOffset = 0x4000 * _cartContext.RomBankValue;

         }

         // RAM Bank Number / RTC Register Select
         else if (Common.BETWEEN(address, 0x4000, 0x5FFF))
         {
            _cartContext.RamBankValue = (byte)(value % 0x0D);
         }

         // Latch Clock Data
         else if (Common.BETWEEN(address, 0x6000, 0x7FFF))
         {
            if (_cartContext.timerPresent && !_cartContext.Latch && (value & 0x01) == 0x01)
            {
               _cartContext._RTC.LatchClock();
               _cartContext.NeedSave = true;
            }
            _cartContext.Latch = (value & 0x01) == 0x01;
         }

         else if (Common.BETWEEN(address, 0xA000, 0xBFFF))
         {
            if (!_cartContext.RamEnabled)
               return;

            // Write to RAM Bank
            if (_cartContext.RamBank != null && _cartContext.RamBankValue < 4)
            {
               _cartContext.RamBank[address - 0xA000] = value;
               if (_cartContext.Battery)
               {
                  _cartContext.NeedSave = true;
               }
            }

            // RTC register
            else if (_cartContext.timerPresent &&
               _cartContext._RTC != null &&
               Common.BETWEEN(_cartContext.RamBankValue, 0x08, 0x0C))
            {
               switch (_cartContext.RamBankValue)
               {
                  case 0x08:
                     // Seconds
                     _cartContext._RTC.RealSeconds = value;
                     break;
                  case 0x09:
                     // Minutes
                     _cartContext._RTC.RealMinutes = value;
                     break;
                  case 0x0A:
                     // Hours
                     _cartContext._RTC.RealHours = value;
                     break;
                  case 0x0B:
                     // Lower 8 bits of Day Counter
                     _cartContext._RTC.RealDayLower = value;
                     break;
                  case 0x0C:
                     // Upper Day Counter, Half, Carry
                     _cartContext._RTC.RealDayUpper = value;
                     break;
               }
            }
         }
      }

      return;
   }
}
