using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PathPipeCreator))]
public class PathPipeTest : MonoBehaviour
{
    private PathPipeCreator pathPipeCreator;

    private void Start()
    {
        //make sure to ignore the first transform, which is the parent object
        Transform[] waypointObjects = GetComponentsInChildren<Transform>()[1..];

        // Make sure there's at least two waypoints to create a pipe
        if (waypointObjects.Length < 2)
        {
            Debug.LogError("At least two waypoints are required to create a pipe.");
            return;
        }

        // Initialize the PathPipeCreator
        pathPipeCreator = gameObject.GetComponent<PathPipeCreator>();

        List<Vector3> waypoints = new List<Vector3>();
        foreach (Transform waypoint in waypointObjects)
        {
            waypoints.Add(waypoint.position);
        }

        // Create the pipe
        pathPipeCreator.CreatePipe(waypoints);
    }
}
