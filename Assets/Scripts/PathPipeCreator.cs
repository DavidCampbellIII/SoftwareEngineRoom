using UnityEngine;
using System.Collections.Generic;

public class PathPipeCreator : MonoBehaviour
{
    [SerializeField]
    private float pipeRadius = 0.5f;
    [SerializeField]
    private int pipeSides = 6;
    [SerializeField]
    private Material pipeMaterial;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = pipeMaterial;
    }

    public void CreatePipe(List<Vector3> waypoints)
    {
        Mesh pipeMesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            AddPipeSegment(vertices, triangles, waypoints[i], waypoints[i + 1], pipeRadius, pipeSides);
        }

        pipeMesh.vertices = vertices.ToArray();
        pipeMesh.triangles = triangles.ToArray();
        pipeMesh.RecalculateNormals();

        meshFilter.mesh = pipeMesh;
    }

    private void AddPipeSegment(List<Vector3> vertices, List<int> triangles, Vector3 start, Vector3 end, float radius, int sides)
    {
        int startVertexIndex = vertices.Count;

        // Create ring vertices
        Vector3 segmentDirection = (end - start).normalized;
        Vector3 cross = (segmentDirection != Vector3.up) ? Vector3.up : Vector3.right;
        Vector3 normal = Vector3.Cross(segmentDirection, cross).normalized;

        for (int i = 0; i <= sides; i++)
        {
            float angle = i * 2 * Mathf.PI / sides;
            Vector3 pointOffset = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, segmentDirection) * normal * radius;
            vertices.Add(start + pointOffset);
            vertices.Add(end + pointOffset);
        }

        // Create triangles
        for (int i = 0; i < sides; i++)
        {
            int rootIndex = startVertexIndex + i * 2;
            int nextIndex = startVertexIndex + ((i + 1) % sides) * 2;

            // Triangle 1
            triangles.Add(rootIndex);
            triangles.Add(nextIndex);
            triangles.Add(rootIndex + 1);

            // Triangle 2
            triangles.Add(nextIndex);
            triangles.Add(nextIndex + 1);
            triangles.Add(rootIndex + 1);
        }
    }
}

