using MyBox;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using UnityEngine;
using System.Linq;
using System.IO;
using Sirenix.OdinInspector;

public class EngineRoomBuilder : MonoBehaviour
{
    [SerializeField, MustBeAssigned]
    private ProjectParser parser;

    [Button]
    private void BuildLayout()
    {
        Dictionary<ISymbol, HashSet<string>> referencedSymbols = parser.ScanProject();
        // Dictionary to store the rooms
        var rooms = new Dictionary<string, Room>();

        // Iterate through the referencedSymbols dictionary
        foreach (var entry in referencedSymbols)
        {
            var symbol = entry.Key;
            var references = entry.Value;

            // Check if the symbol is a NamedType (i.e., a class)
            if (symbol.Kind == SymbolKind.NamedType)
            {
                var className = symbol.ToString();

                // Create a room for the class and add it to the rooms dictionary
                var room = new Room(className);
                rooms.Add(className, room);
            }
        }

        // Iterate through the referencedSymbols dictionary again to create machines and connections
        foreach (var entry in referencedSymbols)
        {
            var symbol = entry.Key;
            var references = entry.Value;

            // Check if the symbol is a method, field, or property
            if (symbol.Kind != SymbolKind.Method && symbol.Kind != SymbolKind.Field &&
                symbol.Kind != SymbolKind.Property)
            {
                continue;
            }

            var machineName = symbol.ToString();

            // Get the containing class name
            var containingClassName = symbol.ContainingType.ToString();

            // Create a machine for the method, field, or property and add it to the corresponding room
            var machine = new Machine(machineName);
            rooms[containingClassName].Machines.Add(machine);

            // Iterate through the references and create connections
            foreach (var reference in references)
            {
                // Extract the target class and method/constructor names
                var targetInfo = reference.Split('.');
                var targetClassName = string.Join(".", targetInfo.Take(targetInfo.Length - 1));
                var targetMachineName = targetInfo.Last();

                // Create a connection between the machines and add it to the source machine's connections list
                var targetMachine = rooms[targetClassName].Machines.FirstOrDefault(m => m.Name == targetMachineName);
                if (targetMachine != null)
                {
                    var connection = new Connection(machine, targetMachine);
                    machine.Connections.Add(connection);
                }
            }
        }

        WriteRoomsToFile(rooms, "rooms");
    }

    private void WriteRoomsToFile(Dictionary<string, Room> rooms, string fileName)
    {
        string path = Path.Combine(Application.dataPath, fileName, ".txt");
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var streamWriter = new StreamWriter(fileStream);

        foreach (var roomEntry in rooms)
        {
            var room = roomEntry.Value;
            streamWriter.WriteLine($"Room: {room.Name}");
            streamWriter.WriteLine("Machines:");

            foreach (var machine in room.Machines)
            {
                streamWriter.WriteLine($"\t- {machine.Name}");
                streamWriter.WriteLine("\tConnections:");

                foreach (var connection in machine.Connections)
                {
                    streamWriter.WriteLine($"\t\t- {connection.Source.Name} -> {connection.Target.Name}");
                }
            }

            streamWriter.WriteLine();
        }
    }
}
