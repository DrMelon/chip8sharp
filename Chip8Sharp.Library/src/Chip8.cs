namespace Chip8Sharp.Library;

using System.Collections.Generic;

public class Chip8
{
    /// <summary>
    /// Maximum bytes of RAM the CHIP-8 has.
    /// </summary>
    public const uint RAM_SIZE = 4096;
    
    /// <summary>
    /// Display width, in pixels.
    /// </summary>
    public const byte DISPLAY_WIDTH = 64;
    
    /// <summary>
    /// Display height, in pixels.
    /// </summary>
    public const byte DISPLAY_HEIGHT = 32;
    
    /// <summary>
    /// Number of registers of the CHIP-8. 0x00 - 0x0F.
    /// </summary>
    public const uint REGISTER_COUNT = 16;
    
    /// <summary>
    /// Location of Font data in CHIP-8. 
    /// </summary>
    public const UInt16 FONT_START_ADDRESS = 0x050;
    
    /// <summary>
    /// Location of Program Data in CHIP-8.
    /// </summary>
    public const UInt16 PC_START_ADDRESS = 0x200;
    
    /// <summary>
    /// CHIP-8's RAM.
    /// </summary>
    public byte[] RAM = new byte[RAM_SIZE];
    
    /// <summary>
    /// CHIP-8's Display - read this to provide graphical display. Monochrome.
    /// </summary>
    public bool[] Display = new bool[DISPLAY_WIDTH * DISPLAY_HEIGHT];
    
    /// <summary>
    /// CHIP-8's program counter.
    /// </summary>
    public UInt16 PC = PC_START_ADDRESS;
    
    /// <summary>
    /// CHIP-8's memory index register, often called I. 
    /// </summary>
    public UInt16 Index;
    
    /// <summary>
    /// CHIP-8's working stack.
    /// </summary>
    public Stack<UInt16> Stack = new();
    
    /// <summary>
    /// CHIP-8's delay timer - this is used to provide a simple timing clock and is updated at 60Hz by <see cref="UpdateDelayClocks"/>.
    /// </summary>
    public byte DelayTimer;
    
    /// <summary>
    /// CHIP-8's sound timer - when this is above zero, produce a beeping tone. It is updated at 60Hz by <see cref="UpdateDelayClocks"/>.
    /// </summary>
    public byte SoundTimer;
    
    /// <summary>
    /// CHIP-8's registers.
    /// </summary>
    public byte[] Registers = new byte[REGISTER_COUNT];
    
    /// <summary>
    /// CHIP-8's 16-key keyboard state. Each bit represents on/off of one of the 16 keys of the CHIP-8. See <see cref="SetKeyState"/> for an easier API.
    /// </summary>
    public UInt16 KeyboardState; 

    /// <summary>
    /// Setting this to true will change some of the instructions to behave like the CHIP-48 and SUPER-CHIP. Some ROMs may require this.
    /// </summary>
    public bool SuperChipQuirks = false; 

    /// <summary>
    /// Read-only CHIP-8 font data. 
    /// </summary>
    public readonly byte[] Font = new byte[]
    {
        0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
        0x20, 0x60, 0x20, 0x20, 0x70, // 1
        0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
        0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
        0x90, 0x90, 0xF0, 0x10, 0x10, // 4
        0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
        0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
        0xF0, 0x10, 0x20, 0x40, 0x40, // 7
        0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
        0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
        0xF0, 0x90, 0xF0, 0x90, 0x90, // A
        0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
        0xF0, 0x80, 0x80, 0x80, 0xF0, // C
        0xE0, 0x90, 0x90, 0x90, 0xE0, // D
        0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
        0xF0, 0x80, 0xF0, 0x80, 0x80 // F
    };

    /// <summary>
    /// Creates a CHIP-8 instance with its font data preloaded at <see cref="FONT_START_ADDRESS"/>.
    /// </summary>
    /// <returns>CHIP-8 instance</returns>
    public static Chip8 CreateWithFont()
    {
        var chip8 = new Chip8();
        chip8.Font.CopyTo(chip8.RAM, FONT_START_ADDRESS);

        return chip8;
    }

    /// <summary>
    /// Perform a single step of the fetch, decode, execute cycle. Should be run at roughly 500Hz.
    /// </summary>
    public void Step()
    {
        // Fetch
        byte instructionFirstByte = RAM[PC++];
        byte instructionSecondByte = RAM[PC++];
        
        // Decode
        UInt16 fullSizeInstruction = BitConverter.ToUInt16(new byte[]{instructionSecondByte, instructionFirstByte}, 0);
        byte firstNibble = (byte)(instructionFirstByte & 0b11110000);
        firstNibble = (byte)(firstNibble >> 4);
        byte secondNibble = (byte)(instructionFirstByte & 0b00001111);
        byte thirdNibble = (byte)(instructionSecondByte & 0b11110000);
        thirdNibble = (byte)(thirdNibble >> 4);
        byte fourthNibble = (byte)(instructionSecondByte & 0b00001111);

        UInt16 secondThirdFourth =  BitConverter.ToUInt16(new byte[]{instructionSecondByte, secondNibble}, 0);
        
        
        // Execute
        switch (firstNibble)
        {
            case 0x0: // 0x00E0 - CLS - Clear Screen
                if (thirdNibble == 0xE && fourthNibble == 0x0)
                    ClearDisplay();
                else if (thirdNibble == 0xE && fourthNibble == 0xE) // 0x00EE - RET - Return from subroutine
                    PC = Stack.Pop();
                else
                {
                    Console.WriteLine($"UNKNOWN INSTRUCTION {fullSizeInstruction:X}");
                }
                break;
            case 0x1: // 0x1NNN - JMP - Jump to NNN.
                PC = secondThirdFourth;
                break;
            case 0x2: // 0x2NNN - JSR - Execute subroutine at NNN
                Stack.Push(PC);
                PC = secondThirdFourth;
                break;
            case 0x3: // 0x3XNN - JEQ - if the value in VX is equal to NN, move PC + 2
                if (Registers[secondNibble] == instructionSecondByte)
                    PC += 2;
                break;
            case 0x4: // 0x3XNN - JNE - if the value in VX is NOT equal to NN, move PC + 2
                if (Registers[secondNibble] != instructionSecondByte)
                    PC += 2;
                break;
            case 0x5: // 0x5XY0 - CMP - if the values in X and Y are equal, move PC + 2
                if (Registers[secondNibble] == Registers[thirdNibble])
                    PC += 2;
                break;
            case 0x6: // 0x6XNN - STR - Set register VX to NN 
                Registers[secondNibble] = instructionSecondByte;
                break;
            case 0x7: // 0x7XNN - ADD - Add value NN to register VX (no carry)
                Registers[secondNibble] += instructionSecondByte;
                break;
            case 0x8: // ARITHMETIC & LOGIC
                switch (fourthNibble)
                {
                    case 0x0: // 0x8XY0 - SET - VX is set to value of VY
                        Registers[secondNibble] = Registers[thirdNibble];
                        break;
                    case 0x1: // 0x8XY1 - OR - VX is set to the value of VX Binary OR'ed with VY 
                        Registers[secondNibble] = (byte)(Registers[secondNibble] | Registers[thirdNibble]);
                        break;  
                    case 0x2: // 0x8XY2 - AND - VX is set to the value of VX & VY
                        Registers[secondNibble] = (byte)(Registers[secondNibble] & Registers[thirdNibble]);
                        break;
                    case 0x3: // 0x8XY3 - XOR - VX is set to VX XOR VY
                        Registers[secondNibble] = (byte)(Registers[secondNibble] ^ Registers[thirdNibble]);
                        break;
                    case 0x4: // 0x8XY4 - ADD with Carry - VX = VX + VY. Set VF to 1 if overflowed
                        if (Registers[secondNibble] + Registers[thirdNibble] > 255)
                            Registers[0xF] = 1;
                        Registers[secondNibble] = (byte)(Registers[secondNibble] + Registers[thirdNibble]);
                        break;
                    case 0x5: // 0x8XY5 - SUB with Carry - VX = VX - VY. VF is 1 - if underflow, VF is set to 0.
                        Registers[0xF] = 1;
                        if (Registers[secondNibble] < Registers[thirdNibble])
                            Registers[0xF] = 1;
                        Registers[secondNibble] = (byte)(Registers[secondNibble] - Registers[thirdNibble]);
                        break;
                    case 0x6: // 0x8XY6 - SHR - Copy VY to VX and Shift VX Right by 1. Keep leftover bit in VF. (NOTE: SUPER-CHIP AND CHIP-48 DO THIS DIFFERENTLY)
                        if(!SuperChipQuirks)
                            Registers[secondNibble] = Registers[thirdNibble];
                        Registers[0xF] = (byte)(Registers[secondNibble] & 0b00000001); // Get low bit into VF
                        Registers[secondNibble] = (byte)(Registers[secondNibble] >> 1);
                        break;
                    case 0x7: // 0xXY7 - SUB with Carry (reverse) - VX = VY - VX. Same VF logic as 0x8XY5.
                        Registers[0xF] = 1;
                        if (Registers[thirdNibble] < Registers[secondNibble])
                            Registers[0xF] = 1;
                        Registers[secondNibble] = (byte)(Registers[thirdNibble] - Registers[secondNibble]);
                        break;
                    case 0xE: // 0x8XYE - SHL - Copy VY to VX and Shift VX Left by 1. Keep leftover bit in VF. (NOTE: SUPER-CHIP AND CHIP-48 DO THIS DIFFERENTLY)
                        if(!SuperChipQuirks)
                            Registers[secondNibble] = Registers[thirdNibble];
                        Registers[0xF] = (byte)(Registers[secondNibble] & 0b10000000); // Get high bit into VF
                        Registers[secondNibble] = (byte)(Registers[secondNibble] << 1);
                        break;
                    default:
                        Console.WriteLine($"UNKNOWN INSTRUCTION {fullSizeInstruction:X}");
                        break;
                }
                break;
            case 0x9: // 0x5XY0 - NCMP - if the values in X and Y are NOT equal, move PC + 2
                if (Registers[secondNibble] != Registers[thirdNibble])
                    PC += 2;
                break;
            case 0xA: // 0xANNN - SETI - Set Index to NNN
                Index = secondThirdFourth;
                break;
            case 0xB: // 0xBNNN - JMPO - Jump with offset. Jump to NNN + V0 (SUPER-CHIP AND CHIP-48 DO THIS DIFFERENTLY
                if (!SuperChipQuirks)
                    PC = (UInt16)(secondThirdFourth + Registers[0]);
                else
                    PC = (UInt16)(secondThirdFourth + Registers[secondNibble]);
                break;
            case 0xC: // 0xCXNN - VX is set to (NN & a random number)
                Registers[secondNibble] = (byte)(instructionSecondByte & Random.Shared.Next());
                break;
            case 0xD: // 0xDXYN - Draw N pixel tall sprite at X coord in VX and Y coord in VY 
                var drawX = (byte)(Registers[secondNibble] % DISPLAY_WIDTH);
                var drawY = (byte)(Registers[thirdNibble] % DISPLAY_HEIGHT);
                var spriteHeight = fourthNibble;
                DrawSprite(drawX, drawY, spriteHeight);
                break;
            case 0xE: // Keypresses
                if (instructionSecondByte == 0x9E) // 0xEX9E - KEYDN - If key in VX is pressed, move PC + 2
                {
                    var key = (byte)(Registers[secondNibble] % 0xF); // modulo 0xF isn't quite standard but it's convenient
                    if (IsKeyDown(key))
                        PC += 2;
                }
                else if (instructionSecondByte == 0xA1) // 0xEXA1 - KEYUP - If key in VX is NOT pressed, move PC + 2
                {
                    var key = (byte)(Registers[secondNibble] % 0xF); // modulo 0xF isn't quite standard but it's convenient
                    if (!IsKeyDown(key))
                        PC += 2;
                }
                else
                {
                    Console.WriteLine($"UNKNOWN INSTRUCTION {fullSizeInstruction:X}");
                }
                break;
            case 0xF: // Devices
                if (instructionSecondByte == 0x07) // 0xFX07 - Set VX to the current value of the delay timer.
                    Registers[secondNibble] = DelayTimer;
                else if (instructionSecondByte == 0x15) // 0xFX15 - Set Delay Timer to VX 
                    DelayTimer = Registers[secondNibble];
                else if (instructionSecondByte == 0x18) // 0xFX18 - Set Sound Timer to VX
                    SoundTimer = Registers[secondNibble];
                else if (instructionSecondByte == 0x1E) // 0xFX1E - Add to VX to Index
                {
                    Index += Registers[secondNibble];
                    if (Index > 0x0FFF)
                    {
                        Index = (UInt16)(Index % 0x0FFF);
                        Registers[0xF] = 1; // Set VF if overflow address space and wrap.
                    }
                }
                else if (instructionSecondByte == 0x0A) // 0xFX0A - Wait for key. If no keys are pressed, set PC back by 2 to repeat the instruction. When key pressed, set VX.
                {
                    if (KeyboardState == 0)
                    {
                        PC -= 2;
                    }
                    else
                    {
                        for (byte i = 0; i < 0xF; i++)
                        {
                            if (IsKeyDown(i))
                            {
                                Registers[secondNibble] = i;
                                break;
                            }
                        }
                    }
                }
                else if (instructionSecondByte == 0x29) // 0xFX29 - FONT - Sets Index to the location of the font character specified in VX. So for VX = 0xA, would point to the A character.
                    Index = (UInt16)(FONT_START_ADDRESS + Registers[secondNibble]);
                else if (instructionSecondByte == 0x33) // 0xFX33 - BCD - Binary Coded Decimal conversion. Good for score display. Stores the three digits representing the value in VX in three bytes pointed at by Index.
                {
                    var vx = Registers[secondNibble];
                    var units = vx % 10;
                    vx /= 10;
                    var tens = vx % 10;
                    vx /= 10;
                    var hundreds = vx % 10;
                    RAM[Index] = (byte)hundreds;
                    RAM[Index + 1] = (byte)tens;
                    RAM[Index + 2] = (byte)units;
                }
                else if (instructionSecondByte == 0x55) // 0xFX55 - STOREMEM - Stores V0 through VX (inclusive) into ram.
                {
                    for (int i = 0; i <= secondNibble; i++)
                    {
                        RAM[Index + i] = Registers[i];
                    }
                }
                else if (instructionSecondByte == 0x65) // 0xFX65 - LOADMEM - Loads ram into V0 through VX (inclusive)
                {
                    for (int i = 0; i <= secondNibble; i++)
                    {
                        Registers[i] = RAM[Index + i];
                    }
                }
                else
                {
                    Console.WriteLine($"UNKNOWN INSTRUCTION {fullSizeInstruction:X}");
                }
                break;
            default:
                Console.WriteLine($"UNKNOWN INSTRUCTION {fullSizeInstruction:X}");
                break;
        }
    }

    /// <summary>
    /// Set a key of the CHIP-8's 16-key keyboard to the pressed or unpressed state.
    /// </summary>
    /// <param name="key">Which key to press, 0x0 - 0xF</param>
    /// <param name="state">Whether the key is currently pressed or not</param>
    public void SetKeyState(byte key, bool state)
    {
        if (state)
            KeyboardState = (UInt16)(KeyboardState | (1 << key));
        else 
            KeyboardState = (UInt16)(KeyboardState & ~(1 << key));
    }

    /// <summary>
    /// Query the key state of a given CHIP-8 key.
    /// </summary>
    /// <param name="key">Which key to chech, 0x0 - 0xF</param>
    /// <returns>Whether the key is currently pressed or not</returns>
    public bool IsKeyDown(byte key)
    {
        return (KeyboardState & (1 << key)) == 1;
    }
    
    /// <summary>
    /// Draws a sprite using the CHIP-8's special sprite drawing logic.
    /// </summary>
    private void DrawSprite(byte drawX, byte drawY, byte spriteHeight)
    {
        Registers[0x0F] = 0; // VF set to 0.
        
        int py = drawY;
        for (int row = 0; row < spriteHeight; row++)
        {
            
            int px = drawX;
            byte spriteData = RAM[Index + row];
            for (int pixel = 0; pixel < 8; pixel++)
            {
                var pixIndex = (py * DISPLAY_WIDTH) + px;
                var spritePixOn = (spriteData & (0x80 >> pixel)) != 0;

                if (spritePixOn)
                {
                    Display[pixIndex] = !Display[pixIndex]; // XOR toggle this pixel
                    if(Display[pixIndex] == false)
                        Registers[0x0F] = 1; // Set VF to 1 if pixel was just turned off.
                }

                px++;
                if (px >= DISPLAY_WIDTH)
                    break;
            }

            py++;
            if (py >= DISPLAY_HEIGHT)
                break;
        }
    }

    /// <summary>
    /// Must be called at 60Hz for accurate emulation of the CHIP-8, regardless
    /// of the actual update speed of the clock.
    ///
    /// The DelayTimer and the SoundTimer are kind of like a physical hardware clock and audio hardware.
    /// </summary>
    public void UpdateDelayClocks()
    {
        if (DelayTimer > 0)
            DelayTimer--;

        if (SoundTimer > 0)
            SoundTimer--;
    }

    /// <summary>
    /// Copy the internal state from one CHIP-8 instance into the current instance. For save-state support. 
    /// </summary>
    /// <param name="from"></param>
    public void CopyStateFrom(Chip8 from)
    {
        Index = from.Index;
        DelayTimer = from.DelayTimer;
        SoundTimer = from.SoundTimer;
        PC = from.PC;
        KeyboardState = from.KeyboardState;
        
        from.RAM.CopyTo(RAM, 0);
        from.Display.CopyTo(Display, 0);
        Stack = new Stack<ushort>(new Stack<ushort>(from.Stack));
        from.Registers.CopyTo(Registers, 0);
    }

    /// <summary>
    /// Clears the display of the CHIP-8. Equivalent to instruction 0x00E0.
    /// </summary>
    public void ClearDisplay()
    {
        for (int i = 0; i < DISPLAY_WIDTH * DISPLAY_HEIGHT; i++)
        {
            Display[i] = false;
        }
    }
    
}