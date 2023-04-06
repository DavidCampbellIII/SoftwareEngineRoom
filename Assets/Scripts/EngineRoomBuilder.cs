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
    #region Nested Structures

    private struct MachineInstance
    {
        public Machine machine { get; }
        public Transform transform { get; }
        
        public MachineInstance(Machine machine, Transform transform)
        {
            this.machine = machine;
            this.transform = transform;
        }
    }

    #endregion
    [SerializeField, MustBeAssigned]
    private ProjectParser parser;
    [SerializeField, MustBeAssigned]
    private Transform roomPrefab;
    [SerializeField, MustBeAssigned]
    private Transform machinePrefab;
    [SerializeField]
    private float roomPadding = 1.0f;
    [SerializeField]
    private float machineSpacing = 0.5f;
    [SerializeField]
    private float minMachineWidth = 1.0f;
    [SerializeField]
    private float minMachineHeight = 1.0f;
    [SerializeField, MustBeAssigned]
    private Material connectionMaterial;
    [SerializeField, MustBeAssigned]
    private Gradient connectionGradient;

    [Button, ShowIf("@UnityEngine.Application.isPlaying")]
    private void Generate()
    {
        List<Room> rooms = BuildLayout();
        //WriteRoomsToFile(rooms, "rooms");
        BuildRooms(rooms);
    }

    private List<Room> BuildLayout()
    {
        Dictionary<ISymbol, HashSet<string>> referencedSymbols = parser.ScanProject(true);
        Dictionary<string, Room> rooms = new Dictionary<string, Room>();

        //Add all rooms on first pass
        foreach (ISymbol symbol in referencedSymbols.Keys)
        {
            if (symbol.Kind == SymbolKind.NamedType)
            {
                string className = symbol.ToString();
                //Debug.Log($"Class: {className}");

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

            //Debug.Log($"Adding Machine: {machineName} in {containingClassName}");
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
                //Debug.Log($"{machineName} reference: {reference}");

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

        return rooms.Values.ToList();
    }

    private void BuildRooms(List<Room> rooms)
    {
        Dictionary<string, MachineInstance> machineInstances = new Dictionary<string, MachineInstance>();
        
        float xOffset = 0f;
        float prevRoomScale = 0f;

        foreach (Room room in rooms)
        {
            // Instantiate and position the room
            Transform roomInstance = Instantiate(roomPrefab);
            roomInstance.name = room.Name;

            ScaleRoom(room.Machines.Count, roomInstance);
            xOffset += roomInstance.localScale.x + prevRoomScale / 2f + roomPadding;
            roomInstance.position = new Vector3(xOffset, 0, 0);
            prevRoomScale = roomInstance.localScale.x;

            List<Transform> machineTransforms = new List<Transform>();

            // Instantiate machines and place them along the walls
            foreach (Machine machine in room.Machines)
            {
                Transform machineTransform = Instantiate(machinePrefab);
                machineTransform.parent = roomInstance;
                machineTransform.name = machine.Name;
                machineInstances.Add(machine.ID, new MachineInstance(machine, machineTransform));
                machineTransforms.Add(machineTransform);
            }
            PositionMachines(machineTransforms);
        }
        
        // Connect machines
        foreach (MachineInstance machineInstance in machineInstances.Values)
        {
            Machine machine = machineInstance.machine;

            foreach (Machine inputMachine in machine.inputs)
            {
                if (machineInstances.TryGetValue(inputMachine.ID, out MachineInstance inputMachineInstance))
                {
                    ConnectMachines(inputMachineInstance.transform, machineInstance.transform);
                }
            }

            foreach (Machine outputMachine in machine.outputs)
            {
                if (machineInstances.TryGetValue(outputMachine.ID, out MachineInstance outputMachineInstance))
                {
                    ConnectMachines(machineInstance.transform, outputMachineInstance.transform);
                }
            }
        }
    }

    private void ScaleRoom(int numMachines, Transform roomInstance)
    {
        int machinesPerWall = Mathf.CeilToInt(numMachines / 2.0f);

        // Calculate the total width of all machines, including spacing
        float totalWidth = (minMachineWidth + machineSpacing) * machinesPerWall - machineSpacing;

        // Scale the room to fit all machines
        Vector3 roomScale = roomInstance.localScale;
        roomScale.x = Mathf.Max(roomScale.x, totalWidth);
        roomInstance.localScale = roomScale;
    }

    private void PositionMachines(List<Transform> machineInstances)
    {
        int machinesPerWall = Mathf.CeilToInt(machineInstances.Count / 2.0f);

        // Position machines along the walls
        float xPos = -0.5f;
        for (int i = 0; i < machineInstances.Count; i++)
        {
            Transform machineInstance = machineInstances[i];
            Vector3 position;

            // Scale the machine to its minimum size
            Vector3 machineScale = new Vector3(minMachineWidth, minMachineHeight, minMachineWidth / 3f);
            machineInstance.SetLossyScale(machineScale);

            Vector3 localScale = machineInstance.localScale;
            float halfZScale = localScale.z / 2;

            if (i == 0 || i == machinesPerWall)
            {
                xPos = -0.5f + localScale.x / 2f;
            }

            if (i < machinesPerWall) // Front wall
            {
                position = new Vector3(xPos, localScale.y / 2, 0.5f - halfZScale);
            }
            else // Back wall
            {
                position = new Vector3(xPos, localScale.y / 2, -0.5f + halfZScale);
            }

            machineInstance.localPosition = position;
            xPos += localScale.x + machineSpacing * localScale.x;
        }
    }

    private void ConnectMachines(Transform fromMachine, Transform toMachine)
    {
        Vector3 fromPoint = fromMachine.position;
        Vector3 toPoint = toMachine.position;

        Vector3 fromScale = fromMachine.localScale;
        Vector3 toScale = toMachine.localScale;

        fromPoint.z += fromScale.z / 2;
        toPoint.z -= toScale.z / 2;

        fromPoint.y += Random.Range(-fromScale.y / 2f, fromScale.y / 2f);
        toPoint.y += Random.Range(-toScale.y / 2f, toScale.y / 2f);

        fromPoint.x += Random.Range(-fromScale.x / 2f, fromScale.x / 2f);
        toPoint.x += Random.Range(-toScale.x / 2f, toScale.x / 2f);

        LineRenderer lineRenderer = new GameObject("Connection").AddComponent<LineRenderer>();
        lineRenderer.material = connectionMaterial;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        lineRenderer.colorGradient = connectionGradient;

        lineRenderer.SetPosition(0, fromPoint);
        lineRenderer.SetPosition(1, toPoint);

        lineRenderer.transform.parent = fromMachine.parent;
    }

    private void WriteRoomsToFile(List<Room> rooms, string fileName)
    {
        string path = Path.Combine(Application.dataPath, fileName + ".txt");
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var streamWriter = new StreamWriter(fileStream);

        foreach (Room room in rooms)
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
