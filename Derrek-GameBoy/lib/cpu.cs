using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;

public class CPUContext
{
   public CPU_Registers regs;

   // Current fetch...
   public UInt16 fetchedData;
   public UInt16 memDest;
   public byte curOpcode;

   public Instruction CurrInst;

   public bool destIsMem;
   public bool halted;
   public bool stepping;

   public bool intMasterEnabled;
   public bool enablingIme;

   public byte ieRegister;
   public byte intFlags;
}

public static class CPU
{
   public static CPUContext _context = new();

   public static bool CPU_FLAG_Z => Common.BIT(_context.regs.f, 7);
   public static bool CPU_FLAG_C => Common.BIT(_context.regs.f, 4);
   public static bool CPU_FLAG_N => Common.BIT(_context.regs.f, 6);
   public static bool CPU_FLAG_H => Common.BIT(_context.regs.f, 5);

   public static void CPU_Init()
   {
      _context.regs.pc = 0x100;
      _context.regs.a = 0x01;
   }

   public static void Fetch_Instruction()
   {
      _context.curOpcode = Bus.BusRead(_context.regs.pc++);
      _context.CurrInst = Instructions.Instruction_By_Opcode(_context.curOpcode);
   }

   public static void Execute()
   {
      IN_PROC proc = CPUProc.InstGetProcessor(_context.CurrInst.type);

      if ( proc == null )
      {
         Common.NO_IMPL();
      }

      proc(_context);
   }

   public static bool CPU_Step()
   {
      if (!_context.halted)
      {
         UInt16 pc = _context.regs.pc;

         Fetch_Instruction();
         CPUFetch.Fetch_Data(_context);

         string flags = string.Format("{0}{1}{2}{3}",
             (_context.regs.f & (1 << 7)) != 0 ? 'Z' : '-',
             (_context.regs.f & (1 << 6)) != 0 ? 'N' : '-',
             (_context.regs.f & (1 << 5)) != 0 ? 'H' : '-',
             (_context.regs.f & (1 << 4)) != 0 ? 'C' : '-'
         );

         Console.WriteLine($"{Emulator.GetContext().Ticks:X8} - " +
            $"{pc:X4}: {InstLookUp.InstName(_context.CurrInst.type), 7} " +
            $"({_context.curOpcode:X2} {Bus.BusRead((ushort)(pc + 1)):X2} {Bus.BusRead((ushort)(pc + 2)):X2}) " +
            $"A: {_context.regs.a:X2} F: {flags}" +
            $" BC: {_context.regs.b:X2}{_context.regs.c:X2} " +
            $"DE: {_context.regs.d:X2}{_context.regs.e:X2} HL: {_context.regs.h:X2}{_context.regs.l:X2}");

         if (_context.CurrInst == null)
         {
            Console.WriteLine($"Unknown Instruction: {_context.curOpcode:X2}");
            Environment.Exit(-7);
            return false;
         }

         Execute();
      } else
      {
         Emulator.EmuCycle(1);

         if (_context.intFlags != 0)
         {
            _context.halted = false;
         }
      }

      if (_context.intMasterEnabled)
      {

      }

      return true;
   }
}