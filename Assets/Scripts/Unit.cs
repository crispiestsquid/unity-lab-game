using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour {

    public int tileX;
    public int tileY;
    public TileMap map;
    public bool isBusy = false;
    public List<Job> heldJobs;

    int moveSpeed = 2;
    float remainingMovement = 2;

    public List<int[]> queuedTasks;
    public List<Node> currentPath = null;

    public string dropOffAt = "";

    private void Start()
    {
        heldJobs = new List<Job>();
        queuedTasks = new List<int[]>();
    }

    void Update()
    {
        // find out if current path is null
        if (currentPath == null)
        {
            // if so do we have any queued tasks to perform
            if(queuedTasks.Count > 0)
            {
                // we have more to do, so set the current path to the next available queued task
                int taskX = queuedTasks[0][0];
                int taskY = queuedTasks[0][1];
                map.GeneratePathTo(taskX, taskY);
                queuedTasks.RemoveAt(0);
            }
        }
        // Draw our debug line showing the pathfinding!
        // NOTE: This won't appear in the actual game view.
        if (currentPath != null)
        {
            isBusy = true;

            int currNode = 0;

            while(currNode < currentPath.Count -1)
            {
                Vector3 start = map.TileCoordToWorldCoord(currentPath[currNode].x, currentPath[currNode].y) +
                    new Vector3(0, 0, -.5f);
                Vector3 end = map.TileCoordToWorldCoord(currentPath[currNode + 1].x, currentPath[currNode + 1].y) +
                    new Vector3(0, 0, -.5f); ;

                Debug.DrawLine(start, end, Color.red);

                currNode++;
            }
        }

        // Have we moved our visible piece close enough to the target tile that we can
        // advance to the next step in our pathfinding?
        if (Vector3.Distance(transform.position, map.TileCoordToWorldCoord(tileX, tileY)) < 0.1f)
            AdvancePathing();

        // Smoothly animate towards the correct map tile.
        transform.position = Vector3.Lerp(transform.position, map.TileCoordToWorldCoord(tileX, tileY), 5f * Time.deltaTime);
    }

    // Advances our pathfinding progress by one tile.
    public void AdvancePathing()
    {
        if (currentPath == null)
            return;

        if (remainingMovement <= 0)
            return;

        // Teleport us to our correct "current" position, in case we
        // haven't finished the animation yet.
        transform.position = map.TileCoordToWorldCoord(tileX, tileY);

        // Get cost from current tile to next tile
        remainingMovement -= map.CostToEnterTile(currentPath[0].x, currentPath[0].y, currentPath[1].x, currentPath[1].y);

        // Move us to the next tile in the sequence
        tileX = currentPath[1].x;
        tileY = currentPath[1].y;

        // Remove the old "current" tile from the pathfinding list
        currentPath.RemoveAt(0);

        if (currentPath.Count == 1)
        {
            // We only have one tile left in the path, and that tile MUST be our ultimate
            // destination -- and we are standing on it!
            // So let's just clear our pathfinding info.
            currentPath = null;
            isBusy = false;
        }

        remainingMovement = moveSpeed;
    }
}