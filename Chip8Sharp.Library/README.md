# Chip8Sharp
A [CHIP-8](https://en.wikipedia.org/wiki/CHIP-8) interpreter/emulator library for C#, built with dotnet 8.0. No dependencies!

## Getting Started
Include Chip8Sharp.Library in your project with whichever means are most familiar to you. 

## Usage
Create an instance of CHIP-8 with `Chip8.CreateWithFont()`.  
Feed in the ROM data by copying the bytes into `Chip8.RAM`, starting at `Chip8.PC_START_ADDRESS`.  
Run `Chip8.Step()` at around 500Hz, and run `Chip8.UpdateDelayTimers()` at 60Hz, and the system is running!  
Read `Chip8.Display` to view the display; it is an array of boolean values, as the display is monochrome in CHIP-8.

