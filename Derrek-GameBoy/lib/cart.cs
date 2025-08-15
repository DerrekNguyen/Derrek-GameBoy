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
}

public static class Cart
{
   private static CartContext _cartContext = new CartContext();

   public static bool CartNeedSave()
   {
      return _cartContext.NeedSave;
   }

   public static bool CartMBC1()
   {
      if (_cartContext.Header is RomHeader header)
      {
         return Common.BETWEEN(header.Type, 1, 3);
      }
      return false;
   }

   public static bool CartBattery()
   {
      if (_cartContext.Header is RomHeader header)
      {
         return header.Type == 3;
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

      return true;
   }

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
            fs.Read(_cartContext.RamBank, 0, 0x2000);
         }
      }
      catch (Exception e)
      {
         Console.WriteLine($"FAILED TO OPEN: {fn}");
         Console.WriteLine(e.Message);
         return;
      }
   }

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
            fs.Write(_cartContext.RamBank, 0, 0x2000);
         }
      } catch (Exception e)
      {
         Console.WriteLine($"FAILED TO OPEN: {fn}");
         Console.WriteLine(e.Message);
         return;
      }
   }

   public static byte CartRead(ushort address)
   {
      if (!CartMBC1() || address < 0x4000)
      {
         return _cartContext.RomData[address];
      }

      if ((address & 0xE000) == 0xA000)
      {
         if (!_cartContext.RamEnabled || _cartContext.RamBank == null)
            return 0xFF;

         return _cartContext.RamBank[address - 0xA000];
      }

      return _cartContext.RomData[address - 0x4000 + _cartContext.RomBankXOffset];
   }


   public static void CartWrite(UInt16 address, byte value) 
   {
      if (!Cart.CartMBC1())
      {
         return;
      }

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
}
