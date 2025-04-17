using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;

#pragma warning disable CS8618
#pragma warning disable CS8600
#pragma warning disable CS8625

namespace PromethiumXS
{
    public enum PrimitiveType : byte
    {
        Triangle = 1,
        Square = 2,
        Polygon = 3
    }

    public enum DisplayCommandType
    {
        Primitive,
        Color,
        Vertex
    }

    public class DisplayCommand
    {
        public DisplayCommandType CommandType { get; }
        public object Data { get; }
        public float? Z { get; } // Optional Z-coordinate for 3D vertices.

        public DisplayCommand(DisplayCommandType commandType, object data, float? z = null)
        {
            CommandType = commandType;
            Data = data;
            Z = z;
        }
    }

    public class DisplayList
    {
        public string Name { get; }
        public List<DisplayCommand> Commands { get; } = new();

        public DisplayList(string name)
        {
            Name = name;
        }
    }

    public class DisplayListManager
    {
        private readonly Dictionary<string, DisplayList> _displayLists;
        private DisplayList _currentList;

        public DisplayListManager()
        {
            _displayLists = new Dictionary<string, DisplayList>(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<string> GetModelNamesFromDictionary()
        {
            return _displayLists.Keys;
        }

        public bool TryGetDisplayList(string modelName, out DisplayList displayList)
        {
            return _displayLists.TryGetValue(modelName, out displayList);
        }

        /// <summary>
        /// Begins a new display list with the given model name.
        /// </summary>
        public void StartDisplayList(string modelName)
        {
            if (_currentList != null)
            {
                throw new InvalidOperationException("Finish the current display list with EndDisplayList() before starting a new one.");
            }
            if (_displayLists.ContainsKey(modelName))
            {
                throw new InvalidOperationException($"A display list with the name '{modelName}' already exists.");
            }
            _currentList = new DisplayList(modelName);
        }

        /// <summary>
        /// Sets the primitive type of the current display list.
        /// </summary>
        public void AddPrimitive(PrimitiveType type)
        {
            EnsureDisplayListActive();
            _currentList.Commands.Add(new DisplayCommand(DisplayCommandType.Primitive, type));
        }

        /// <summary>
        /// Adds a color command to the current display list.
        /// Expects a hex string (e.g. "FF0000" for red).
        /// </summary>
        public void AddColor(string colorHex)
        {
            EnsureDisplayListActive();
            // Convert hex string (assumed RRGGBB) to a Color.
            if (colorHex.Length != 6 ||
                !int.TryParse(colorHex, NumberStyles.HexNumber, null, out int argb))
            {
                throw new ArgumentException($"Invalid color hex: {colorHex}");
            }
            Color color = Color.FromArgb((argb >> 16) & 0xFF, (argb >> 8) & 0xFF, argb & 0xFF);
            _currentList.Commands.Add(new DisplayCommand(DisplayCommandType.Color, color));
        }

        /// <summary>
        /// Adds a vertex command to the current display list.
        /// </summary>
        public void AddVertex(float x, float y, float z)
        {
            EnsureDisplayListActive();
            _currentList.Commands.Add(new DisplayCommand(DisplayCommandType.Vertex, new PointF(x, y), z));
        }

        /// <summary>
        /// Ends the current display list and saves it.
        /// </summary>
        public void EndDisplayList()
        {
            EnsureDisplayListActive();
            _displayLists[_currentList.Name] = _currentList;
            _currentList = null;
        }

        /// <summary>
        /// Calls (renders) the given display list.
        /// </summary>
        public void CallDisplayList(string modelName, float x = 0, float y = 0, float z = 0)
        {
            if (!_displayLists.TryGetValue(modelName, out DisplayList list))
            {
                throw new KeyNotFoundException($"Display list '{modelName}' not found.");
            }

            Console.WriteLine($"Calling display list '{modelName}' at position ({x}, {y}, {z}).");

            foreach (var cmd in list.Commands)
            {
                switch (cmd.CommandType)
                {
                    case DisplayCommandType.Primitive:
                        Console.WriteLine($"  Primitive: {(PrimitiveType)cmd.Data}");
                        break;
                    case DisplayCommandType.Color:
                        Console.WriteLine($"  Color: {((Color)cmd.Data).Name}");
                        break;
                    case DisplayCommandType.Vertex:
                        PointF point = (PointF)cmd.Data;
                        Console.WriteLine($"  Vertex: ({point.X:F2}, {point.Y:F2})");
                        break;
                }
            }
            // Integration with the render pipeline would occur here.
        }

        private void EnsureDisplayListActive()
        {
            if (_currentList == null)
            {
                throw new InvalidOperationException("No display list is currently being constructed. Call StartDisplayList first.");
            }
        }

        /// <summary>
        /// Serializes the display list with the given model name into a byte array using the 2D format.
        /// Header format:
        ///   [nameLength (1 byte)][modelName (ASCII)]
        /// Each command is then encoded as follows:
        ///   - Primitive: [commandType (1 byte)][primitiveType (1 byte)]
        ///   - Color: [commandType (1 byte)][R (1 byte)][G (1 byte)][B (1 byte)]
        ///   - Vertex: [commandType (1 byte)][x (4 bytes float)][y (4 bytes float)][z (4 bytes float)]
        /// </summary>
        public byte[] SerializeDisplayList(string modelName)
        {
            if (!_displayLists.TryGetValue(modelName, out DisplayList list))
            {
                throw new KeyNotFoundException($"Display list '{modelName}' not found.");
            }
            if (modelName.Length > 255)
            {
                throw new ArgumentException($"Model name '{modelName}' exceeds the maximum length of 255 characters.");
            }

            // Count the types of commands.
            byte vertexCount = (byte)list.Commands.Count(cmd => cmd.CommandType == DisplayCommandType.Vertex);
            byte colorCount = (byte)list.Commands.Count(cmd => cmd.CommandType == DisplayCommandType.Color);
            byte primitiveCount = (byte)list.Commands.Count(cmd => cmd.CommandType == DisplayCommandType.Primitive);

            List<byte> bytes = new List<byte>();

            // Header:
            // Magic byte to mark the beginning of a display list.
            const byte DisplayListMagic = 0xD2;
            bytes.Add(DisplayListMagic);

            // Model name header: [nameLength (1 byte)][modelName (ASCII)]
            byte nameLength = (byte)modelName.Length;
            bytes.Add(nameLength);
            bytes.AddRange(Encoding.ASCII.GetBytes(modelName));

            // Counts header: [vertexCount][colorCount][primitiveCount]
            bytes.Add(vertexCount);
            bytes.Add(colorCount);
            bytes.Add(primitiveCount);

            // Now encode each display command.
            foreach (var cmd in list.Commands)
            {
                // Write the command type.
                bytes.Add((byte)cmd.CommandType);
                switch (cmd.CommandType)
                {
                    case DisplayCommandType.Primitive:
                        if (cmd.Data is not PrimitiveType primitiveType)
                        {
                            throw new InvalidOperationException($"Invalid data type for Primitive command: {cmd.Data.GetType()}");
                        }
                        bytes.Add((byte)primitiveType);
                        break;
                    case DisplayCommandType.Color:
                        if (cmd.Data is not Color color)
                        {
                            throw new InvalidOperationException($"Invalid data type for Color command: {cmd.Data.GetType()}");
                        }
                        bytes.Add(color.R);
                        bytes.Add(color.G);
                        bytes.Add(color.B);
                        break;
                    case DisplayCommandType.Vertex:
                        if (cmd.Data is not PointF point)
                        {
                            throw new InvalidOperationException($"Invalid data type for Vertex command: {cmd.Data.GetType()}");
                        }
                        float zValue = cmd.Z ?? 0f;
                        bytes.AddRange(BitConverter.GetBytes(point.X));
                        bytes.AddRange(BitConverter.GetBytes(point.Y));
                        bytes.AddRange(BitConverter.GetBytes(zValue));
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown display command type: {cmd.CommandType}");
                }
            }

            Console.WriteLine($"[SerializeDisplayList] Serialized data for '{modelName}': {BitConverter.ToString(bytes.ToArray())}");
            return bytes.ToArray();
        }

        /// <summary>
        /// Serializes the display list with the given model name into a byte array using the 3D format.
        /// Header format (total 16 bytes):
        ///   Byte 0: Primitive type (1 byte)
        ///   Bytes 1-4: Vertex count (int32)
        ///   Bytes 5-8: Color count (int32)
        ///   Bytes 9-12: Index count (int32)
        ///   Bytes 13-15: Reserved (zeros)
        /// Then follows:
        ///   - Vertex data block: each vertex is 12 bytes (x, y, z as floats)
        ///   - Color data block: each color is 4 bytes (R, G, B, A)
        ///   - Index data block: (currently unused, index count is 0)
        /// </summary>
        public byte[] SerializeDisplayListAs3D(string modelName)
        {
            if (!_displayLists.TryGetValue(modelName, out DisplayList list))
            {
                throw new KeyNotFoundException($"Display list '{modelName}' not found.");
            }

            // Determine the primitive type.
            // If there is a Primitive command, take its type; otherwise default to Triangle (1).
            byte primitiveType = 1;
            var primitiveCmd = list.Commands.FirstOrDefault(cmd => cmd.CommandType == DisplayCommandType.Primitive);
            if (primitiveCmd != null && primitiveCmd.Data is PrimitiveType pt)
            {
                primitiveType = (byte)pt;
            }

            // Count the number of vertex and color commands.
            int vertexCount = list.Commands.Count(cmd => cmd.CommandType == DisplayCommandType.Vertex);
            int colorCount = list.Commands.Count(cmd => cmd.CommandType == DisplayCommandType.Color);
            int indexCount = 0; // Currently, we do not pack any indices.

            List<byte> bytes = new List<byte>();

            // Build the 16-byte header.
            // Byte 0: primitiveType.
            bytes.Add(primitiveType);
            // Bytes 1-4: vertexCount.
            bytes.AddRange(BitConverter.GetBytes(vertexCount));
            // Bytes 5-8: colorCount.
            bytes.AddRange(BitConverter.GetBytes(colorCount));
            // Bytes 9-12: indexCount.
            bytes.AddRange(BitConverter.GetBytes(indexCount));
            // Pad the remaining bytes (13-15) with zeros.
            while (bytes.Count < 16)
            {
                bytes.Add(0);
            }

            // Pack vertex data: each vertex command gives 12 bytes (x, y, z as floats).
            foreach (var cmd in list.Commands)
            {
                if (cmd.CommandType == DisplayCommandType.Vertex)
                {
                    if (cmd.Data is PointF point)
                    {
                        float zValue = cmd.Z ?? 0f;
                        bytes.AddRange(BitConverter.GetBytes(point.X));
                        bytes.AddRange(BitConverter.GetBytes(point.Y));
                        bytes.AddRange(BitConverter.GetBytes(zValue));
                    }
                }
            }

            // Pack color data: each color command gives 4 bytes (R, G, B, A).
            foreach (var cmd in list.Commands)
            {
                if (cmd.CommandType == DisplayCommandType.Color)
                {
                    if (cmd.Data is Color color)
                    {
                        bytes.Add(color.R);
                        bytes.Add(color.G);
                        bytes.Add(color.B);
                        bytes.Add(255); // Full opacity.
                    }
                }
            }

            // For now, no index data is appended.

            Console.WriteLine($"[SerializeDisplayListAs3D] Serialized data for '{modelName}': {BitConverter.ToString(bytes.ToArray())}");
            return bytes.ToArray();
        }

        /// <summary>
        /// Stores the serialized display list into the DPL memory domain starting at the specified address.
        /// </summary>
        /// <param name="modelName">Name of the display list to store.</param>
        /// <param name="memory">The memory system instance.</param>
        /// <param name="startAddress">Starting address in the DPL memory domain.</param>
        public void StoreDisplayListToDpl(string modelName, Memory memory, int startAddress)
        {
            byte[] data = SerializeDisplayList(modelName);

            Console.WriteLine($"[DisplayListManager] Storing display list '{modelName}' to DPL memory at address {startAddress:X8}");
            Console.WriteLine($"[DisplayListManager] Serialized data ({data.Length} bytes): {BitConverter.ToString(data)}");

            // Write the serialized data to the DPL memory domain without removing it from memory.
            for (int i = 0; i < data.Length; i++)
            {
                memory.Write(MemoryDomain.DPL, startAddress + i, data[i]);
            }

            Console.WriteLine($"[DisplayListManager] Display list '{modelName}' stored in DPL memory from address {startAddress:X8} to {startAddress + data.Length - 1:X8}");
        }
    }
}