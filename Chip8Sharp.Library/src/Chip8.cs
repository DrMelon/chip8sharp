namespace Chip8Sharp.Library;

using System.Collections.Generic;

public class Chip8
{
    public const uint RAM_SIZE = 4096;
    public const byte DISPLAY_WIDTH = 64;
    public const byte DISPLAY_HEIGHT = 32;
    public const uint REGISTER_COUNT = 16;
    public const UInt16 FONT_START_ADDRESS = 0x000;
    public const UInt16 PC_START_ADDRESS = 0x200;
    
    public byte[] RAM = new byte[RAM_SIZE];
    public bool[] Display = new bool[DISPLAY_WIDTH * DISPLAY_HEIGHT];
    public UInt16 PC = PC_START_ADDRESS;
    public UInt16 Index;
    public Stack<UInt16> Stack = new();
    public byte DelayTimer;
    public byte SoundTimer;
    public byte[] Registers = new byte[REGISTER_COUNT];
    
    public UInt16 KeyboardState; // Each bit represents on/off of one of the 16 keys of the CHIP-8.

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

    public static Chip8 Create()
    {
        var chip8 = new Chip8();
        chip8.Font.CopyTo(chip8.RAM, FONT_START_ADDRESS);

        return chip8;
    }

    /// <summary>
    /// Perform a single step of the fetch, decode, execute cycle.
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
            case 0x0: // 0x00E0 - Clear Screen
                if (thirdNibble == 0xE)
                    ClearDisplay();
                break;
            case 0x1: // 0x1NNN - Jump to NNN.
                PC = secondThirdFourth;
                break;
            case 0x2:
                break;
            case 0x3:
                break;
            case 0x4:
                break;
            case 0x5:
                break;
            case 0x6: // 0x6XNN - Set register VX to NN 
                Registers[secondNibble] = instructionSecondByte;
                break;
            case 0x7: // 0x7XNN - Add value NN to register VX
                Registers[secondNibble] += instructionSecondByte;
                break;
            case 0x8:
                break;
            case 0x9:
                break;
            case 0xA: // 0xANNN - Set Index to NNN
                Index = secondThirdFourth;
                break;
            case 0xB:
                break;
            case 0xC:
                break;
            case 0xD: // 0xDXYN - Draw N pixel tall sprite at X coord in VX and Y coord in VY 
                var drawX = (byte)(Registers[secondNibble] % DISPLAY_WIDTH);
                var drawY = (byte)(Registers[thirdNibble] % DISPLAY_HEIGHT);
                var spriteHeight = fourthNibble;
                DrawSprite(drawX, drawY, spriteHeight);
                break;
            case 0xE:
                break;
            case 0xF:
                break;
        }
    }

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

    public void ClearDisplay()
    {
        for (int i = 0; i < DISPLAY_WIDTH * DISPLAY_HEIGHT; i++)
        {
            Display[i] = false;
        }
    }
    
}