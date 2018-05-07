using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TileMap : MonoBehaviour {

    public GameObject selectedUnit;

    public TileType[] tileTypes;

    int[,] tiles;
    Node[,] graph;

    public int mapSizeX = 10;
    public int mapSizeY = 10;

    public List<Machine> machines = new List<Machine>();

    enum OBJECTS
    {
        floor, marker, taper, wax, generator, finer, polisher, deblock, clean, sp200, ar,
        firstInspection, blocker, edger, finalInspection
    };

    void Start()
    {
        // set up the selected unit's variables
        selectedUnit.GetComponent<Unit>().tileX = (int)selectedUnit.transform.position.x;
        selectedUnit.GetComponent<Unit>().tileY = (int)selectedUnit.transform.position.y;
        selectedUnit.GetComponent<Unit>().map = this;

        GenerateMapData();
        GeneratePathfindingGraph();
        GenerateMapVisual();
    }

    void GenerateMapData()
    {
        // allocate our map tiles
        tiles = new int[mapSizeX, mapSizeY];

        int x, y;

        // initialize our map tiles to be floor
        for (x = 0; x < mapSizeX; x++)
        {
            for (y = 0; y < mapSizeY; y++)
            {
                tiles[x, y] = (int)OBJECTS.floor;
            }
        }

        tiles[1, 2] = (int)OBJECTS.marker;
        tiles[5, 4] = (int)OBJECTS.taper;
        tiles[7, 2] = (int)OBJECTS.wax;
        tiles[7, 6] = (int)OBJECTS.generator;
        tiles[6, 7] = (int)OBJECTS.finer;
        tiles[5, 8] = (int)OBJECTS.polisher;
        tiles[1, 7] = (int)OBJECTS.deblock;
        tiles[7, 8] = (int)OBJECTS.clean;
        tiles[3, 2] = (int)OBJECTS.sp200;
        tiles[1, 4] = (int)OBJECTS.ar;
        tiles[2, 9] = (int)OBJECTS.firstInspection;
        tiles[2, 0] = (int)OBJECTS.blocker;
        tiles[5, 0] = (int)OBJECTS.edger;
        tiles[6, 4] = (int)OBJECTS.finalInspection;

    }

    public float CostToEnterTile(int sourceX, int sourceY, int targetX, int targetY)
    {
        TileType tt = tileTypes[tiles[targetX, targetY]];

        if(UnitCanEnterTile(targetX, targetY) == false)
        {
            return Mathf.Infinity;
        }

        float cost = tt.movementCost;

        if(sourceX != targetX && sourceY != targetY)
        {
            // we are moving diagonally! fudge the cost for tie breaking
            // purely a cosmetic thing!
            cost += 0.001f;
        }

        return cost;
    }

    void GeneratePathfindingGraph()
    {
        // initialize the array
        graph = new Node[mapSizeX, mapSizeY];

        // initialize a Node for each spot in the array
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                graph[x, y] = new Node();
                graph[x, y].x = x;
                graph[x, y].y = y;
            }
        }

        // now that all the nodes exist, calculate their neighbors
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                // this is the four way version:

/*              // try left
                if (x > 0)
                {
                    graph[x, y].neighbors.Add(graph[x - 1, y]);
                }

                try right
                if (x < mapSizeX - 1)
                {
                    graph[x, y].neighbors.Add(graph[x + 1, y]);
                }

                //try down
                if (y > 0)
                {
                    graph[x, y].neighbors.Add(graph[x, y - 1]);
                }

                // try up
                if (y < mapSizeY - 1)
                {
                    graph[x, y].neighbors.Add(graph[x, y + 1]);
                }
*/
                
                // this is the 8 way version(allows diagonal movement)
                // try left
                if (x > 0)
                {
                    graph[x, y].neighbors.Add(graph[x - 1, y]);
                    if (y > 0)
                    {
                        graph[x, y].neighbors.Add(graph[x - 1, y - 1]);
                    }

                    if (y < mapSizeY - 1)
                    {
                        graph[x, y].neighbors.Add(graph[x - 1, y + 1]);
                    }
                }

                // try right
                if (x < mapSizeX - 1)
                {
                    graph[x, y].neighbors.Add(graph[x + 1, y]);
                    if (y > 0)
                    {
                        graph[x, y].neighbors.Add(graph[x + 1, y - 1]);
                    }

                    if (y < mapSizeY - 1)
                    {
                        graph[x, y].neighbors.Add(graph[x + 1, y + 1]);
                    }
                }

                // try straight up and down
                if (y > 0)
                {
                    graph[x, y].neighbors.Add(graph[x, y - 1]);
                }

                if (y < mapSizeY - 1)
                {
                    graph[x, y].neighbors.Add(graph[x, y + 1]);
                }

                // this also works with 6 way hexes and n way variable areas
            }
        }
    }

    void GenerateMapVisual()
    {

        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                TileType tt = tileTypes[tiles[x, y]];

                GameObject go = Instantiate(tt.tileVisualPrefab, new Vector3(x, y, 0), Quaternion.identity);

                ClickableTile ct = go.GetComponent<ClickableTile>();
                ct.tileX = x;
                ct.tileY = y;
                ct.map = this;

                // if tileType is a machine type, store it in my machines list
                if (tt.name == "machine")
                {
                    Machine machine = go.GetComponent<Machine>();

                    machines.Add(machine);
                }
            }
        }
    }

    public Vector3 TileCoordToWorldCoord(int x, int y)
    {
        return new Vector3(x, y, 0);
    }

    public bool UnitCanEnterTile(int x, int y)
    {
        // we could test the units walk/hover/fly type against various terrain flags
        // to see if they are allowed to enter the tile


        return tileTypes[tiles[x, y]].isWalkable;
    }

    public void GeneratePathTo(int x, int y)
    {
        // clear out old path
        selectedUnit.GetComponent<Unit>().currentPath = null;

        if(UnitCanEnterTile(x, y) == false)
        {
            // we probably clicked on a mountain or something, so just quit out
            return;
        }

        Dictionary<Node, float> dist = new Dictionary<Node, float>();
        Dictionary<Node, Node> prev = new Dictionary<Node, Node>();

        // set up our list of nodes we havent checked yet
        List<Node> unvisited = new List<Node>();

        Node source = graph[
                            selectedUnit.GetComponent<Unit>().tileX,
                            selectedUnit.GetComponent<Unit>().tileY
                            ];

        Node target = graph[
                            x,
                            y
                            ];

        dist[source] = 0;
        prev[source] = null;

        // initialize everything to have infinity distance since we dont know any better right now
        foreach(Node v in graph)
        {
            if(v != source)
            {
                dist[v] = Mathf.Infinity;
                prev[v] = null;
            }

            unvisited.Add(v);
        }

        while(unvisited.Count > 0)
        {
            // u is going to be the unvisited node with the smallest distance
            Node u = null;

            foreach(Node possibleU in unvisited)
            {
                if(u == null || dist[possibleU] < dist[u])
                {
                    u = possibleU;
                }
            }

            if(u == target)
            {
                break;
            }

            unvisited.Remove(u);

            foreach(Node v in u.neighbors)
            {
               // float alt = dist[u] + u.DistanceTo(v);
                float alt = dist[u] + CostToEnterTile(u.x, u.y, v.x, v.y);
                if (alt < dist[v])
                {
                    dist[v] = alt;
                    prev[v] = u;
                }
            }
        }

        // if we get here then either we found the shortest route to our target, or their is no route at all

        if(prev[target] == null)
        {
            // no route between our target and source

            return;
        }

        List<Node> currentPath = new List<Node>();

        Node curr = target;

        // step through the prev chain and add it to our path
        while(curr != null)
        {
            currentPath.Add(curr);
            curr = prev[curr];
        }

        // right now currentPath describes a route from our target to our source
        // so we need to invert it

        currentPath.Reverse();

        selectedUnit.GetComponent<Unit>().currentPath = currentPath;
    }
}
