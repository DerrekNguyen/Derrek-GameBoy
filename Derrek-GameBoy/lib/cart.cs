using System;
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
   public string Filename { get; set; } = string.Empty;
   public UInt32 RomSize { get; set; }
   public Byte[] RomData { get; set; } = Array.Empty<Byte>();
   public RomHeader? Header { get; set; }
}

public static class Cart
{
   private static CartContext _cartContext = new CartContext();

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

      // Verify the checksum
      UInt16 x = 0;
      for (UInt16 i = 0x0134; i <= 0x014C; ++i)
      {
         x = (UInt16)(x - _cartContext.RomData[i] - 1);
      }
      bool passed = (x & 0xFF) == header.Checksum;
      Console.WriteLine($"\t Checksum : {header.Checksum:X2} ({(passed ? "PASSED" : "FAILED")})");

      return true;
   }

   public static byte CartRead(UInt16 address)
   {
      //for now just ROM ONLY type supported...

      return _cartContext.RomData[address];
   }
   public static void CartWrite(UInt16 address, byte value) 
   {
      //for now, ROM ONLY...

      Console.WriteLine($"Cart Write: {address:X4}");
      //Common.NO_IMPL();
   }
}
