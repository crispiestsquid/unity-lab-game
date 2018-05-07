using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Machine : MonoBehaviour {

    public int frontX;
    public int frontY;
    public int tileX;
    public int tileY;
    public string orientation = "RIGHT";
    public string type;
    public int waitTime = 1;
    public GameObject trayPrefab;

    List<Job> jobs = new List<Job>();
    Job currentJob;

    Dictionary<string, int> TYPES = new Dictionary<string, int>() { {"MARKER", 0}, {"TAPER", 1}, {"WAX", 2}, {"GENERATOR", 3}, {"FINER", 4}, {"POLISHER", 5}, {"DEBLOCK", 6},
                                                                    {"CLEAN", 7}, {"SP-200", 8}, {"AR", 9}, {"FIRST INSPECTION", 10}, {"BLOCKER", 11}, {"EDGER", 12}, {"FINAL INSPECTION", 13}};

    Dictionary<string, int> TIMES = new Dictionary<string, int>() { {"MARKER", 5}, {"TAPER", 5}, {"WAX", 8}, {"GENERATOR", 10}, {"FINER", 8}, {"POLISHER", 10}, {"DEBLOCK", 5},
                                                                    {"CLEAN", 8}, {"SP-200", 15}, {"AR", 5}, {"FIRST INSPECTION", 5}, {"BLOCKER", 5}, {"EDGER", 10}, {"FINAL INSPECTION", 5}};

    int timeRemaining;
    bool currentJobIsDone;
    bool calledWorker;

    int mapSizeX;
    int mapSizeY;

    Unit worker;

    int workerX;
    int workerY;

    ClickableTile ct;

    List<GameObject> trays;

    // Use this for initialization
    void Start ()
    {
        timeRemaining = TIMES[type];
        currentJobIsDone = false;
        calledWorker = false;
        trays = new List<GameObject>();

        if(type == "MARKER")
        {
            Job job1 = new Job();
            Job job2 = new Job();
            Job job3 = new Job();
            jobs.Add(job1);
            jobs.Add(job2);
            //jobs.Add(job3);
            Debug.Log("Starting with " + jobs.Count + " jobs");
            InstantiateTrays();
        }

        ct = GetComponent<ClickableTile>();
        worker = ct.map.selectedUnit.GetComponent<Unit>();

        tileX = ct.tileX;
        tileY = ct.tileY;

        mapSizeX = ct.map.mapSizeX;
        mapSizeY = ct.map.mapSizeY;

        SetOrientation();
    }

    private void Update()
    {
        // get worker's coordinates
        workerX = worker.tileX;
        workerY = worker.tileY;

        // check if there are jobs available, and none currently running
        if (jobs.Count > 0 && currentJob == null)
        {
            currentJob = jobs[0];
            jobs.RemoveAt(0);
            RunCurrentJob();
        }

        // check to see if worker is here to drop off job
        if (workerX == frontX && workerY == frontY  && type == worker.dropOffAt)
        {
            // the worker is here
            GetJobFromWorker();
        }

        // check to see if current job is finished and waiting for a worker
        if (currentJob != null && currentJobIsDone)
        {
            // worker should be en route so see if he is here yet
            if(workerX == frontX && workerY == frontY)
            {
                // the worker must be here so see if he has a job in hand already
                if(worker.heldJobs.Count > 0)
                {
                    // worker already has job, we will check and call worker back later
                    Debug.Log("worker already is holding a job");
                }
                else
                {
                    GiveWorkerJob();
                }
            }
        }
    }

    void InstantiateTrays()
    {
        foreach(GameObject t in trays)
        {
            Destroy(t);
        }

        trays.RemoveRange(0, trays.Count);

        int numJobs = jobs.Count;
        if(currentJob != null)
        {
            numJobs += 1;
        }

        Vector3 trayPos = new Vector3(transform.position.x, transform.position.y, transform.position.z - 1.54f);
        bool first = true;
        float z = trayPos.z;
        GameObject tray;

        while (numJobs > 0)
        {
            if (first)
            {
                tray = Instantiate(trayPrefab, trayPos, Quaternion.identity);
                trays.Add(tray);
                first = false;
            }
            else
            {
                z -= .058f;
                trayPos = new Vector3(trayPos.x, trayPos.y, z);
                tray = Instantiate(trayPrefab, trayPos, Quaternion.identity);
                trays.Add(tray);
            }

            numJobs--;
        }
    }

    void RemoveTray()
    {
        Destroy(trays[trays.Count - 1]);
        trays.RemoveAt(trays.Count - 1);
    }

    void SetOrientation()
    {
        // figure out orientation
        switch (orientation)
        {
            case "RIGHT":
                if (tileX + 1 <= mapSizeX)
                {
                    frontX = tileX + 1;
                    frontY = tileY;
                }
                    break;
            case "LEFT":
                if (tileX - 1 >= 0)
                {
                    frontX = tileX - 1;
                    frontY = tileY;
                }
                break;
            case "UP":
                if (tileY + 1 <= mapSizeY)
                {
                    frontX = tileX;
                    frontY = tileY + 1;
                }
                break;
            case "DOWN":
                if (tileY - 1 >= 0)
                {
                    frontX = tileX;
                    frontY = tileY - 1;
                }
                    break;
            default:
                break;
        }

    }

    void RunCurrentJob()
    {
        if(timeRemaining > 0)
        {
            StartCoroutine(DecrementTime());
        }
    }

    IEnumerator DecrementTime()
    {
        while (timeRemaining > 0)
        {
            timeRemaining--;
            yield return new WaitForSeconds(1);
        }

        currentJobIsDone = true;
        calledWorker = true;
        SetCharacterPathHere(frontX, frontY);
    }

    IEnumerator WaitTime()
    {
        while (waitTime > 0)
        {
            waitTime--;
            yield return new WaitForSeconds(1);
        }

        RemoveTray();

        SendWorkerToNext();
        calledWorker = false;
    }

    void GiveWorkerJob()
    {
        worker.heldJobs.Add(currentJob);
        currentJob = null;
        currentJobIsDone = false;
        timeRemaining = TIMES[type];

        // send worker to drop job at next machine
        // first wait for some time
        if(waitTime > 0)
        {
            StartCoroutine(WaitTime());
        }
    }

    void GetJobFromWorker()
    {
        if(worker.heldJobs.Count > 0)
        {
            jobs.Add(worker.heldJobs[0]);
            worker.heldJobs.RemoveAt(0);
            worker.dropOffAt = "";
            InstantiateTrays();
            Debug.Log(type + " now has " + jobs.Count + " jobs to run");
        }
    }

    void SendWorkerToNext()
    {
        waitTime = 1;
        // figure out what station is next
        // loop through the map's list of machines
        int currentMachine = TYPES[type];
        int nextMachine;
        string typeToCheck;
        foreach(Machine m in ct.map.machines)
        {
            typeToCheck = m.type; // grab the type of the machine we are at in the for loop
            nextMachine = (int)TYPES[typeToCheck]; // get the value of the machine from our dictionary of types

            // check to see if the value is equal to our current machine + 1
            if(nextMachine == currentMachine + 1)
            {
                // if this is true this machine must be the next in the sequence of machines logically
                // not just the next machine in the list
                // it is safe to send the worker to this station
                if(worker.dropOffAt == "")
                {
                    worker.dropOffAt = typeToCheck;
                }
                SetCharacterPathHere(m.frontX, m.frontY);
                break;
            }
        }
    }

    public void SetCharacterPathHere(int x, int y)
    {
        int[] targetCoordinates = new int[2];
        targetCoordinates[0] = x;
        targetCoordinates[1] = y;
        worker.queuedTasks.Add(targetCoordinates);
    }
}
