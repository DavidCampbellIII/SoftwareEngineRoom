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
        Dictionary<ISymbol, HashSet<string>> referencedSymbols = parser.ScanProject(true);
        Dictionary<string, Room> rooms = new Dictionary<string, Room>();

        //Add all rooms on first pass
        foreach (ISymbol symbol in referencedSymbols.Keys)
        {
            if (symbol.Kind == SymbolKind.NamedType)
            {
                string className = symbol.ToString();
                Debug.Log($"Class: {className}");

                Room room = new Room(className);
                rooms.Add(className, room);
            }
        }

        //Add all machines in second pass
        foreach (ISymbol symbol in referencedSymbols.Keys)
        {
            if (symbol.Kind != SymbolKind.Method && symbol.Kind != SymbolKind.Field &&
                symbol.Kind != SymbolKind.Property)
            {
                continue;
            }

            string machineName = symbol.ToString();

            // Get the containing class name
            string containingClassName = symbol.ContainingType.ToString();

            // Create a machine for the method, field, or property and add it to the corresponding room
            Machine machine = new Machine(machineName);
            rooms[containingClassName].Machines.Add(machine);

            Debug.Log($"Adding Machine: {machineName} in {containingClassName}");
        }

        //Add connections for each machine in final pass
        foreach (var entry in referencedSymbols)
        {
            var symbol = entry.Key;
            var references = entry.Value;

            if (symbol.Kind != SymbolKind.Method && symbol.Kind != SymbolKind.Field &&
                symbol.Kind != SymbolKind.Property)
            {
                continue;
            }

            string machineName = symbol.ToString();

            // Get the containing class name
            string containingClassName = symbol.ContainingType.ToString();
            Machine machine = rooms[containingClassName].Machines.Find(m => m.ID == machineName);

            // Iterate through the references and create connections
            foreach (string reference in references)
            {
                Debug.Log($"{machineName} reference: {reference}");

                // Extract the target class and method/constructor names
                string[] targetInfo = reference.Split('.');
                string targetClassName = string.Join(".", targetInfo.Take(targetInfo.Length - 1));
                string targetMachineName = reference;

                //Debug.Log($"Target class: {targetClassName}");
                //Debug.Log($"Target machine: {targetMachineName}");

                Machine targetMachine = rooms[targetClassName].Machines.FirstOrDefault(m => m.ID == targetMachineName);
                if (targetMachine != null)
                {
                    machine.inputs.Add(targetMachine);
                    targetMachine.outputs.Add(machine);
                }
                else
                {
                    Debug.LogWarning($"Could not find machine {targetMachineName} in class {targetClassName}");
                }
            }
        }

        WriteRoomsToFile(rooms, "rooms");
    }

    private void WriteRoomsToFile(Dictionary<string, Room> rooms, string fileName)
    {
        string path = Path.Combine(Application.dataPath, fileName + ".txt");
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var streamWriter = new StreamWriter(fileStream);

        foreach (Room room in rooms.Values)
        {
            streamWriter.WriteLine($"Room: {room.Name}");
            WriteIndentedLine(streamWriter, 1, "Machines:");

            foreach (Machine machine in room.Machines)
            {
                WriteIndentedLine(streamWriter, 2, $"- {machine.ID}");
                WriteIndentedLine(streamWriter, 3, "Inputs:");

                foreach (Machine input in machine.inputs)
                {
                    WriteIndentedLine(streamWriter, 4, $"---> {input.ID}");
                }

                WriteIndentedLine(streamWriter, 3, "Outputs:");

                foreach (Machine output in machine.outputs)
                {
                    WriteIndentedLine(streamWriter, 4, $"<--- {output.ID}");
                }
            }

            streamWriter.WriteLine("-----------------------------------------------------------");
        }
    }

    private static void WriteIndentedLine(StreamWriter streamWriter, int indentLevel, string text)
    {
        string indentation = string.Concat(Enumerable.Repeat("\t", indentLevel));
        streamWriter.WriteLine($"{indentation}{text}");
    }
}
