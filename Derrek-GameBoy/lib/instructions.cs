using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public enum AddrMode
{
   AM_IMP,
   AM_R_D16,
   AM_R_R,
   AM_MR_R,
   AM_R,
   AM_R_D8,
   AM_R_MR,
   AM_R_HLI,
   AM_R_HLD,
   AM_HLI_R,
   AM_HLD_R,
   AM_R_A8,
   AM_A8_R,
   AM_HL_SPR,
   AM_D16,
   AM_D8,
   AM_D16_R,
   AM_MR_D8,
   AM_MR,
   AM_A16_R,
   AM_R_A16,
}

public enum RegType
{
   RT_NONE,
   RT_A,
   RT_F,
   RT_B,
   RT_C,
   RT_D,
   RT_E,
   RT_H,
   RT_L,
   RT_AF,
   RT_BC,
   RT_DE,
   RT_HL,
   RT_SP,
   RT_PC,
}

public enum InType
{
   IN_NONE,
   IN_NOP,
   IN_LD,
   IN_INC,
   IN_DEC,
   IN_RLCA,
   IN_ADD,
   IN_RRCA,
   IN_STOP,
   IN_RLA,
   IN_JR,
   IN_RRA,
   IN_DAA,
   IN_CPL,
   IN_SCF,
   IN_CCF,
   IN_HALT,
   IN_ADC,
   IN_SUB,
   IN_SBC,
   IN_AND,
   IN_XOR,
   IN_OR,
   IN_CP,
   IN_POP,
   IN_JP,
   IN_PUSH,
   IN_RET,
   IN_CB,
   IN_CALL,
   IN_RETI,
   IN_LDH,
   IN_JPHL,
   IN_DI,
   IN_EI,
   IN_RST,
   IN_ERR,

   //CB instructions...
   IN_RLC,
   IN_RRC,
   IN_RL,
   IN_RR,
   IN_SLA,
   IN_SRA,
   IN_SWAP,
   IN_SRL,
   IN_BIT,
   IN_RES,
   IN_SET,
}

public struct CPU_Registers
{
   public byte a;
   public byte f;
   public byte b;
   public byte c;
   public byte d;
   public byte e;
   public byte h;
   public byte l;
   public UInt16 pc;
   public UInt16 sp;
}

public enum CondType
{
   CT_NONE, CT_NZ, CT_Z, CT_NC, CT_C
}

public static class InstLookUp
{
   public static readonly string[] table = new string[]
   {
      "<NONE>",
      "NOP",
      "LD",
      "INC",
      "DEC",
      "RLCA",
      "ADD",
      "RRCA",
      "STOP",
      "RLA",
      "JR",
      "RRA",
      "DAA",
      "CPL",
      "SCF",
      "CCF",
      "HALT",
      "ADC",
      "SUB",
      "SBC",
      "AND",
      "XOR",
      "OR",
      "CP",
      "POP",
      "JP",
      "PUSH",
      "RET",
      "CB",
      "CALL",
      "RETI",
      "LDH",
      "JPHL",
      "DI",
      "EI",
      "RST",
      "IN_ERR",
      "IN_RLC",
      "IN_RRC",
      "IN_RL",
      "IN_RR",
      "IN_SLA",
      "IN_SRA",
      "IN_SWAP",
      "IN_SRL",
      "IN_BIT",
      "IN_RES",
      "IN_SET"
   };

   public static string InstName(InType t)
   {
      int index = (int)t;

      if (index < 0 || index > table.Length)
      {
         return "<INVALID>";
      }

      return table[index];
   }
}

public class Instruction
{
   public InType type;
   public AddrMode mode;
   public RegType reg1;
   public RegType reg2;
   public CondType cond;
   public byte param;

   public Instruction(
      InType inType = InType.IN_NONE, 
      AddrMode addrMode = AddrMode.AM_IMP, 
      RegType regType1 = RegType.RT_NONE, 
      RegType regType2 = RegType.RT_NONE)
   {
      type = inType;
      mode = addrMode;
      reg1 = regType1;
      reg2 = regType2;
   }
}

public static class Instructions
{
   public static Instruction[] _instructions = new Instruction[0x100];

   static Instructions()
   {
      _instructions[0x00] = new Instruction(InType.IN_NOP);
      _instructions[0x01] = new Instruction(InType.IN_LD, AddrMode.AM_R_D16, RegType.RT_BC);
      _instructions[0x02] = new Instruction(InType.IN_LD, AddrMode.AM_MR_R, RegType.RT_BC, RegType.RT_A);

      _instructions[0x05] = new Instruction(InType.IN_DEC, AddrMode.AM_R, RegType.RT_B);
      _instructions[0x06] = new Instruction(InType.IN_LD, AddrMode.AM_R_D8, RegType.RT_B);

      _instructions[0x08] = new Instruction(InType.IN_LD, AddrMode.AM_A16_R, RegType.RT_NONE, RegType.RT_SP);

      _instructions[0x0A] = new Instruction(InType.IN_LD, AddrMode.AM_R_MR, RegType.RT_A, RegType.RT_BC);

      _instructions[0x0E] = new Instruction(InType.IN_LD, AddrMode.AM_R_D8, RegType.RT_C);

      // 0x1*
      _instructions[0x11] = new Instruction(InType.IN_LD, AddrMode.AM_R_D16, RegType.RT_DE);
      _instructions[0x12] = new Instruction(InType.IN_LD, AddrMode.AM_MR_R, RegType.RT_DE, RegType.RT_A);

      _instructions[0x15] = new Instruction(InType.IN_DEC, AddrMode.AM_R, RegType.RT_D);
      _instructions[0x16] = new Instruction(InType.IN_LD, AddrMode.AM_R_D8, RegType.RT_D);

      _instructions[0x1A] = new Instruction(InType.IN_LD, AddrMode.AM_R_MR, RegType.RT_A, RegType.RT_DE);

      _instructions[0x1E] = new Instruction(InType.IN_LD, AddrMode.AM_R_D8, RegType.RT_E);

      // 0x2*
      _instructions[0x21] = new Instruction(InType.IN_LD, AddrMode.AM_R_D16, RegType.RT_HL);
      _instructions[0x22] = new Instruction(InType.IN_LD, AddrMode.AM_HLI_R, RegType.RT_HL, RegType.RT_A);

      _instructions[0x25] = new Instruction(InType.IN_DEC, AddrMode.AM_R, RegType.RT_H);
      _instructions[0x26] = new Instruction(InType.IN_LD, AddrMode.AM_R_D8, RegType.RT_H);

      _instructions[0x2A] = new Instruction(InType.IN_LD, AddrMode.AM_R_HLI, RegType.RT_A, RegType.RT_HL);

      _instructions[0x2E] = new Instruction(InType.IN_LD, AddrMode.AM_R_D8, RegType.RT_L);

      // 0x3*
      _instructions[0x31] = new Instruction(InType.IN_LD, AddrMode.AM_R_D16, RegType.RT_SP);
      _instructions[0x32] = new Instruction(InType.IN_LD, AddrMode.AM_HLD_R, RegType.RT_HL, RegType.RT_A);

      _instructions[0x35] = new Instruction(InType.IN_DEC, AddrMode.AM_R, RegType.RT_HL);
      _instructions[0x36] = new Instruction(InType.IN_LD, AddrMode.AM_MR_D8, RegType.RT_HL);

      _instructions[0x3A] = new Instruction(InType.IN_LD, AddrMode.AM_R_HLD, RegType.RT_A, RegType.RT_HL);

      _instructions[0x3E] = new Instruction(InType.IN_LD, AddrMode.AM_R_D8, RegType.RT_A);

      // 0x4*
      _instructions[0x40] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_B, RegType.RT_B);
      _instructions[0x41] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_B, RegType.RT_C);
      _instructions[0x42] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_B, RegType.RT_D);
      _instructions[0x43] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_B, RegType.RT_E);
      _instructions[0x44] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_B, RegType.RT_H);
      _instructions[0x45] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_B, RegType.RT_L);
      _instructions[0x46] = new Instruction(InType.IN_LD, AddrMode.AM_R_MR, RegType.RT_B, RegType.RT_HL);
      _instructions[0x47] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_B, RegType.RT_A);
      _instructions[0x48] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_C, RegType.RT_B);
      _instructions[0x49] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_C, RegType.RT_C);
      _instructions[0x4A] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_C, RegType.RT_D);
      _instructions[0x4B] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_C, RegType.RT_E);
      _instructions[0x4C] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_C, RegType.RT_H);
      _instructions[0x4D] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_C, RegType.RT_L);
      _instructions[0x4E] = new Instruction(InType.IN_LD, AddrMode.AM_R_MR, RegType.RT_C, RegType.RT_HL);
      _instructions[0x4F] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_C, RegType.RT_A);

      // 0x5*
      _instructions[0x50] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_D, RegType.RT_B);
      _instructions[0x51] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_D, RegType.RT_C);
      _instructions[0x52] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_D, RegType.RT_D);
      _instructions[0x53] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_D, RegType.RT_E);
      _instructions[0x54] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_D, RegType.RT_H);
      _instructions[0x55] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_D, RegType.RT_L);
      _instructions[0x56] = new Instruction(InType.IN_LD, AddrMode.AM_R_MR, RegType.RT_D, RegType.RT_HL);
      _instructions[0x57] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_D, RegType.RT_A);
      _instructions[0x58] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_E, RegType.RT_B);
      _instructions[0x59] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_E, RegType.RT_C);
      _instructions[0x5A] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_E, RegType.RT_D);
      _instructions[0x5B] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_E, RegType.RT_E);
      _instructions[0x5C] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_E, RegType.RT_H);
      _instructions[0x5D] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_E, RegType.RT_L);
      _instructions[0x5E] = new Instruction(InType.IN_LD, AddrMode.AM_R_MR, RegType.RT_E, RegType.RT_HL);
      _instructions[0x5F] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_E, RegType.RT_A);

      // 0x6*
      _instructions[0x60] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_H, RegType.RT_B);
      _instructions[0x61] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_H, RegType.RT_C);
      _instructions[0x62] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_H, RegType.RT_D);
      _instructions[0x63] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_H, RegType.RT_E);
      _instructions[0x64] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_H, RegType.RT_H);
      _instructions[0x65] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_H, RegType.RT_L);
      _instructions[0x66] = new Instruction(InType.IN_LD, AddrMode.AM_R_MR, RegType.RT_H, RegType.RT_HL);
      _instructions[0x67] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_H, RegType.RT_A);
      _instructions[0x68] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_L, RegType.RT_B);
      _instructions[0x69] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_L, RegType.RT_C);
      _instructions[0x6A] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_L, RegType.RT_D);
      _instructions[0x6B] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_L, RegType.RT_E);
      _instructions[0x6C] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_L, RegType.RT_H);
      _instructions[0x6D] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_L, RegType.RT_L);
      _instructions[0x6E] = new Instruction(InType.IN_LD, AddrMode.AM_R_MR, RegType.RT_L, RegType.RT_HL);
      _instructions[0x6F] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_L, RegType.RT_A);

      // 0x7*
      _instructions[0x70] = new Instruction(InType.IN_LD, AddrMode.AM_MR_R, RegType.RT_HL, RegType.RT_B);
      _instructions[0x71] = new Instruction(InType.IN_LD, AddrMode.AM_MR_R, RegType.RT_HL, RegType.RT_C);
      _instructions[0x72] = new Instruction(InType.IN_LD, AddrMode.AM_MR_R, RegType.RT_HL, RegType.RT_D);
      _instructions[0x73] = new Instruction(InType.IN_LD, AddrMode.AM_MR_R, RegType.RT_HL, RegType.RT_E);
      _instructions[0x74] = new Instruction(InType.IN_LD, AddrMode.AM_MR_R, RegType.RT_HL, RegType.RT_H);
      _instructions[0x75] = new Instruction(InType.IN_LD, AddrMode.AM_MR_R, RegType.RT_HL, RegType.RT_L);
      _instructions[0x76] = new Instruction(InType.IN_HALT);
      _instructions[0x77] = new Instruction(InType.IN_LD, AddrMode.AM_MR_R, RegType.RT_HL, RegType.RT_A);
      _instructions[0x78] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_A, RegType.RT_B);
      _instructions[0x79] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_A, RegType.RT_C);
      _instructions[0x7A] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_A, RegType.RT_D);
      _instructions[0x7B] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_A, RegType.RT_E);
      _instructions[0x7C] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_A, RegType.RT_H);
      _instructions[0x7D] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_A, RegType.RT_L);
      _instructions[0x7E] = new Instruction(InType.IN_LD, AddrMode.AM_R_MR, RegType.RT_A, RegType.RT_HL);
      _instructions[0x7F] = new Instruction(InType.IN_LD, AddrMode.AM_R_R, RegType.RT_A, RegType.RT_A);

      // 0xA0
      _instructions[0xAF] = new Instruction(InType.IN_XOR, AddrMode.AM_R, RegType.RT_A);

      // 0xC0
      _instructions[0xC1] = new Instruction(InType.IN_POP, AddrMode.AM_R, RegType.RT_BC);

      _instructions[0xC3] = new Instruction(InType.IN_JP, AddrMode.AM_D16);

      _instructions[0xC5] = new Instruction(InType.IN_PUSH, AddrMode.AM_R, RegType.RT_BC);

      // 0xD0
      _instructions[0xD1] = new Instruction(InType.IN_POP, AddrMode.AM_R, RegType.RT_DE);

      _instructions[0xD5] = new Instruction(InType.IN_PUSH, AddrMode.AM_R, RegType.RT_DE);

      // 0xE0
      _instructions[0xE0] = new Instruction(InType.IN_LDH, AddrMode.AM_A8_R, RegType.RT_NONE, RegType.RT_A);
      _instructions[0xE1] = new Instruction(InType.IN_POP, AddrMode.AM_R, RegType.RT_HL);
      _instructions[0xE2] = new Instruction(InType.IN_LD, AddrMode.AM_MR_R, RegType.RT_C, RegType.RT_A);

      _instructions[0xE5] = new Instruction(InType.IN_PUSH, AddrMode.AM_R, RegType.RT_HL);

      _instructions[0xEA] = new Instruction(InType.IN_LD, AddrMode.AM_A16_R, RegType.RT_NONE, RegType.RT_A);

      // 0xF0
      _instructions[0xF0] = new Instruction(InType.IN_LDH, AddrMode.AM_R_A8, RegType.RT_A);
      _instructions[0xF1] = new Instruction(InType.IN_POP, AddrMode.AM_R, RegType.RT_AF);
      _instructions[0xF2] = new Instruction(InType.IN_LD, AddrMode.AM_R_MR, RegType.RT_A, RegType.RT_C);
      _instructions[0xF3] = new Instruction(InType.IN_DI);

      _instructions[0xF5] = new Instruction(InType.IN_PUSH, AddrMode.AM_R, RegType.RT_AF);

      _instructions[0xFA] = new Instruction(InType.IN_LD, AddrMode.AM_R_A16, RegType.RT_A, RegType.RT_NONE);
   }

   public static Instruction Instruction_By_Opcode(byte opcode)
   {
      // no need for pointer since returning a class in C# returns by reference.
      // only need it for structs
      if (_instructions[opcode] is not null)
      {
         return _instructions[opcode];
      }
      return new Instruction();
   }
}