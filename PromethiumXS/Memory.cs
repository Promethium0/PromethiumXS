﻿using System;
using System.Collections.Generic;

namespace PromethiumXS
{
    /// <summary>
    /// Memory domains for PromethiumXS. Each domain is 4 MB in size, totaling 32 MB.
    /// </summary>
    public enum MemoryDomain
    {
        System,      // Main system RAM (used for code, data, etc.)
        Video,       // Video RAM (frame buffers, textures, etc.)
        Audio,       // Audio-specific memory for sound buffers and processing
        BIOS,        // BIOS or firmware storage (read-only in practice)
        Cartridge,   // Cartridge or expansion memory for game data
        IO,          // Input/Output memory mapped peripherals
        Cache,       // Cache memory for high-speed operations
        Scratch      // Scratchpad memory for temporary storage or calculations
    }

    /// <summary>
    /// Represents the complete memory system for PromethiumXS.
    /// Divides 32 MB into 8 domains of 4 MB each.
    /// </summary>
    public class Memory
    {
        // Total size for each domain: 4 MB (4 * 1024 * 1024 bytes).
        private const int DomainSize = 4 * 1024 * 1024;

        /// <summary>
        /// Gets the memory for each domain.
        /// </summary>
        public Dictionary<MemoryDomain, byte[]> Domains { get; private set; }

        /// <summary>
        /// Holds the size of the loaded program in the System domain.
        /// </summary>
        public int ProgramSize { get; set; } = 0;

        /// <summary>
        /// Initializes the memory system with 8 domains, each with 4 MB.
        /// </summary>
        public Memory()
        {
            Domains = new Dictionary<MemoryDomain, byte[]>
            {
                { MemoryDomain.System,    new byte[DomainSize] },
                { MemoryDomain.Video,     new byte[DomainSize] },
                { MemoryDomain.Audio,     new byte[DomainSize] },
                { MemoryDomain.BIOS,      new byte[DomainSize] },
                { MemoryDomain.Cartridge, new byte[DomainSize] },
                { MemoryDomain.IO,        new byte[DomainSize] },
                { MemoryDomain.Cache,     new byte[DomainSize] },
                { MemoryDomain.Scratch,   new byte[DomainSize] }
            };
        }

        /// <summary>
        /// Reads a byte from the specified memory domain at a given offset.
        /// </summary>
        public byte Read(MemoryDomain domain, int address)
        {
            if (!Domains.ContainsKey(domain))
                throw new ArgumentException("Invalid memory domain");

            if (address < 0 || address >= DomainSize)
                throw new ArgumentOutOfRangeException(nameof(address), "Address is out of range.");

            return Domains[domain][address];
        }

        /// <summary>
        /// Writes a byte to the specified memory domain at a given offset.
        /// </summary>
        public void Write(MemoryDomain domain, int address, byte value)
        {
            if (!Domains.ContainsKey(domain))
                throw new ArgumentException("Invalid memory domain");

            if (address < 0 || address >= DomainSize)
                throw new ArgumentOutOfRangeException(nameof(address), "Address is out of range.");

            Domains[domain][address] = value;
        }

        /// <summary>
        /// Resets all memory domains by clearing their contents.
        /// </summary>
        public void Reset()
        {
            foreach (var domain in Domains.Keys)
            {
                Array.Clear(Domains[domain], 0, DomainSize);
            }
            ProgramSize = 0;
        }

        /// <summary>
        /// Dumps the content of a given memory domain for debugging.
        /// Displays 16 bytes per line in hexadecimal format.
        /// </summary>
        public void DumpDomain(MemoryDomain domain)
        {
            if (!Domains.ContainsKey(domain))
                throw new ArgumentException("Invalid memory domain");

            Console.WriteLine($"Memory Dump for {domain} Domain:");
            byte[] data = Domains[domain];
            for (int i = 0; i < data.Length; i += 16)
            {
                int count = Math.Min(16, data.Length - i);
                string hex = BitConverter.ToString(data, i, count).Replace("-", " ");
                Console.WriteLine($"{i:X8}: {hex}");
            }
        }
    }
}
