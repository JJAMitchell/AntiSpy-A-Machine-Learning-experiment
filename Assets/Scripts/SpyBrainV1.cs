using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using System.IO;

/*
 * Created by Jordan Mitchell for CI301
 */

[RequireComponent(typeof(NavMeshAgent))]

public class Replay
{
    public List<double> states;
    public double reward;

    public Replay(double sx, double sz, double gx, double gz, double gxv, double gzv, double r)
    {
        //All the inputs and rewards into states so that it can learn through reinforcement learning
        states = new List<double>();
        states.Add(sx);
        states.Add(sz);
        states.Add(gx);
        states.Add(gz);
        states.Add(gxv);
        states.Add(gzv);
        reward = r;
    }
}

public class SpyBrainV1 : MonoBehaviour
{
    public GameObject guard;
    NavMeshAgent guardAgent;

    //Refrence to all its eyes for raycasting
    public GameObject forwardEye;
    public GameObject backwardsEye;
    public GameObject rightEye;
    public GameObject leftEye;

    ANN ann;

    float discount = 0.99f;

    //Adds a list to store the inputs and reward to later be used for reinforcement learning
    float reward = 0.0f;
    List<Replay> replayMemory = new List<Replay>();
    int mCapacity = 10000;

    //Floats to handle completly random instructions so the ai can "Explore" its options
    float exploreRate = 100.0f;
    float maxExploreRate = 100.0f;
    float minExploreRate = 0.01f;
    float exploreDecay = 0.001f;

    int caughtCount = 0;

    //Refrence to a specfic output
    int lastQIndex = 4;

    Vector3 startPos;
    Vector3 currentPos;
    Vector3 guardStartPos;

    //Vector points for moving
    Vector3 forward, backwards, right, left;

    NavMeshAgent spyAgent;
    Renderer spyRender;

    //The range of the raycasts
    public float visableDistance = 10.0f;

    float distance;

    float timer = 0;
    float bestTime = 0;
    float timeCheck;

    bool wallHit = false;

    // String to show what the AI is doing
    string instruction = "Nothing";

    //Booleans to handle saving, loading, random values, training and making it fast
    public bool load = false;
    public bool save = false;
    public bool randomValues = false;
    public bool train = true;
    public bool fast = false;

    // Start is called before the first frame update
    void Start()
    {
        //Sets up the ANN with how many inputs, outputs, hidden layers and neurons it has.
        ann = new ANN(6, 4, 1, 12, 0.3f);
        spyAgent = GetComponent<NavMeshAgent>();
        spyRender = GetComponent<Renderer>();
        startPos = this.transform.position;
        guardAgent = guard.GetComponent<NavMeshAgent>();
        guardStartPos = guard.transform.position;

        distance = Vector3.Distance(guard.transform.position, this.transform.position);
        currentPos = startPos;

        if(fast)
            Time.timeScale = 5.0f;

        timeCheck = timer + 1;

        if(load)
            LoadWeightsFromFile();
    }

    //Makes a GUI to display all the current stats
    GUIStyle guiStyle = new GUIStyle();
    void OnGUI()
    {
        guiStyle.fontSize = 25;
        guiStyle.normal.textColor = Color.white;
        GUI.BeginGroup(new Rect(10, 10, 600, 150));
        GUI.Box(new Rect(0, 0, 150, 140), "Stats", guiStyle);
        GUI.Label(new Rect(10, 25, 500, 30), "Caught: " + caughtCount, guiStyle);

        //If random values are on, add the explore rate to the GUI, else leave it out
        if (randomValues)
        {
            GUI.Label(new Rect(10, 50, 500, 30), "Explore Rate: " + exploreRate, guiStyle);
            GUI.Label(new Rect(10, 75, 500, 30), "This Time: " + timer, guiStyle);
            GUI.Label(new Rect(10, 100, 500, 30), "Best Time: " + bestTime, guiStyle);
        }
        else
        {
            GUI.Label(new Rect(10, 50, 500, 30), "This Time: " + timer, guiStyle);
            GUI.Label(new Rect(10, 75, 500, 30), "Best Time: " + bestTime, guiStyle);
        }

        GUI.Label(new Rect(10, 125, 500, 30), "AI is: " + instruction, guiStyle);

        GUI.EndGroup();
    }

    void SaveWeightsToFile()
    {
        string path = Application.dataPath + "/spyWeights.txt";
        //If file path already exists, delete and make a new one to ensure new set of weights are saved
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        StreamWriter wf = File.CreateText(path);
        wf.WriteLine(ann.PrintWeights());
        wf.Close();
    }

    void LoadWeightsFromFile()
    {
        string path = Application.dataPath + "/spyWeights.txt";
        StreamReader wf = File.OpenText(path);

        if (File.Exists(path))
        {
            string line = wf.ReadLine();
            ann.LoadWeights(line);
        }
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        //Debug rays to show where the hitpoints land.
        Debug.DrawRay(forwardEye.transform.position, forwardEye.transform.right * visableDistance, Color.red);
        Debug.DrawRay(backwardsEye.transform.position, backwardsEye.transform.right * visableDistance, Color.red);
        Debug.DrawRay(rightEye.transform.position, rightEye.transform.right * visableDistance, Color.green);
        Debug.DrawRay(leftEye.transform.position, leftEye.transform.right * visableDistance, Color.green);

        //Creates raycast to get a point in the world space that the AI can walk too.
        RaycastHit hit;
        if(Physics.Raycast(forwardEye.transform.position,forwardEye.transform.right,out hit,visableDistance))
        {
            if (hit.collider.gameObject.tag == "Wall")
            {
                wallHit = true;
                forward = hit.point;
            }
            else
            {
                forward = hit.point;
            }
        }
        if (Physics.Raycast(backwardsEye.transform.position, backwardsEye.transform.right, out hit, visableDistance))
        {
            if (hit.collider.gameObject.tag == "Wall")
            {
                wallHit = true;
                backwards = hit.point;
            }
            else
            {
                backwards = hit.point;
            }
        }
        if (Physics.Raycast(rightEye.transform.position, rightEye.transform.right, out hit, visableDistance))
        {
            if (hit.collider.gameObject.tag == "Wall")
            {
                wallHit = true;
                right = hit.point;
            }
            else
            {
                right = hit.point;
            }
        }
        if (Physics.Raycast(leftEye.transform.position, leftEye.transform.right, out hit, visableDistance))
        {
            if (hit.collider.gameObject.tag == "Wall")
            {
                wallHit = true;
                left = hit.point;
            }
            else
            {
                left = hit.point;
            }
        }


        timer += Time.deltaTime;
        List<double> states = new List<double>();
        List<double> qs = new List<double>();

        //Add all the inputs into the states list
        states.Add(this.transform.position.x);
        states.Add(this.transform.position.z);
        states.Add(guard.transform.position.x);
        states.Add(guard.transform.position.z);
        states.Add(guardAgent.velocity.x);
        states.Add(guardAgent.velocity.z);

        //Handles outputs
        qs = SoftMax(ann.CalcOutput(states));
        double maxQ = qs.Max();
        int maxQIndex = qs.ToList().IndexOf(maxQ);

        //Sets explore rate
        exploreRate = Mathf.Clamp(exploreRate - exploreDecay, minExploreRate, maxExploreRate);

        //Randomly "Explore" outputs
        if (randomValues)
        {
            if (Random.Range(0, 100) < exploreRate)
            maxQIndex = Random.Range(0, 4);
        }

        if (maxQIndex == 0)
        {
            //Move forward
            spyAgent.SetDestination(forward);
            instruction = "Moving forward";
                
        }
        else if (maxQIndex == 1)
        {
            //Move backwards
            spyAgent.SetDestination(backwards);
            instruction = "Moving backwards";
        }
        else if (maxQIndex == 2)
        {
            //Move right
            spyAgent.SetDestination(right);
            instruction = "Moving right";
        }
        else if (maxQIndex == 3)
        {
            //Move left
            spyAgent.SetDestination(left);
            instruction = "Moving left";
        }

        //Checks if the AI is running into a wall
        if (wallHit)
        {
            reward = -1.0f;
            spyRender.material.SetColor("_Color", Color.white);
            wallHit = false;
        }
        else
        {
            reward = 0.1f;
            spyRender.material.SetColor("_Color", Color.blue);
        }

        //Checks if the AI has been caught
        if (guard.GetComponent<GuardBrainV1>().caught)
        {
            reward = -2.0f;
            spyRender.material.SetColor("_Color", Color.white);
        }            
        else
        {
            reward = 0.1f;
        }

        //Every 1 seconds, does several checks to assign rewards to
        if(timeCheck == timer)
        {
            float checkDistance;
            Vector3 position;

            checkDistance = Vector3.Distance(guard.transform.position, this.transform.position);
            position = this.transform.position;

            //Checks to see if its current distance between the AI and the guard is lower or greater
            if(checkDistance <= distance)
            {
                reward = -1.0f;
                spyRender.material.SetColor("_Color", Color.white);
            }
            else if (checkDistance > distance)
            {
                reward = 0.1f;
                spyRender.material.SetColor("_Color", Color.blue);
            }

            //Checks to see if the AI has stayed in the current position
            if((position - currentPos).magnitude<0.1f)
            {
                reward = -1.0f;
                spyRender.material.SetColor("_Color", Color.white);
            }

            //Checks if the AI has called the same output for more then one second.
            if(lastQIndex == maxQIndex)
            {
                reward = -1.0f;
                spyRender.material.SetColor("_Color", Color.white);
            }

            //Sets values for next frames check
            distance = checkDistance;
            currentPos = position;
            lastQIndex = maxQIndex;
            timeCheck = timer + 1;
        }
            
        //Gets all the states for the replay class
        Replay lastMemory = new Replay(this.transform.position.x,
                                       this.transform.position.z,
                                       guard.transform.position.x,
                                       guard.transform.position.z,
                                       guardAgent.velocity.x, guardAgent.velocity.z,
                                       reward);

        //If the replay has stored a certain amount, then remove the first replay from the list.
        if (replayMemory.Count > mCapacity)
            replayMemory.RemoveAt(0);

        replayMemory.Add(lastMemory);


        //When the AI is caught, train another generation while taking into account the reward for an action.
        if (guard.GetComponent<GuardBrainV1>().caught)
        {
            //Back propagate through the replay list
            for (int i = replayMemory.Count - 1; i >= 0; i--)
            {
                List<double> toutputsOld = new List<double>();
                List<double> toutputsNew = new List<double>();
                toutputsOld = SoftMax(ann.CalcOutput(replayMemory[i].states));

                double maxQOld = toutputsOld.Max();
                int action = toutputsOld.ToList().IndexOf(maxQOld);

                double feedback;
                //If its the last memory in the list or if the reward is less then negative 1
                if (i == replayMemory.Count - 1 || replayMemory[i].reward <= -1)
                    feedback = replayMemory[i].reward;
                else
                {
                    toutputsNew = SoftMax(ann.CalcOutput(replayMemory[i + 1].states));
                    maxQ = toutputsNew.Max();
                    feedback = (replayMemory[i].reward + discount * maxQ);
                }

                toutputsOld[action] = feedback;
                if (train)
                    ann.Train(replayMemory[i].states, toutputsOld);
                else
                    ann.CalcOutput(replayMemory[i].states, toutputsOld);
            }

            Reset();
        }
    }

    //Sets everything back for another generation.
    public void Reset()
    {
        if (timer > bestTime)
            bestTime = timer;
        timer = 0;
        timeCheck = timer + 2;
        guard.GetComponent<GuardBrainV1>().caught = false;
        this.transform.position = startPos;
        spyAgent.SetDestination(startPos);
        guardAgent.SetDestination(guardStartPos);
        guardAgent.Warp(guardStartPos);
        replayMemory.Clear();
        caughtCount++;
    }

    List<double> SoftMax(List<double> values)
    {
        double max = values.Max();

        float scale = 0.0f;
        for (int i = 0; i < values.Count; ++i)
            scale += Mathf.Exp((float)(values[i] - max));

        List<double> result = new List<double>();
        for (int i = 0; i < values.Count; ++i)
            result.Add(Mathf.Exp((float)(values[i] - max)) / scale);

        return result;
    }

    void OnApplicationQuit()
    {
        if(save)
            SaveWeightsToFile();
    }
}
