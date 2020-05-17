using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * Created by Jordan Mitchell for CI301
 */

[RequireComponent(typeof(NavMeshAgent))]
public class GuardBrainV1 : MonoBehaviour
{
    public GameObject spy;

    Vector3 destination;

    NavMeshAgent agent;

    public bool caught = false;
    public bool player = false;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        destination = agent.destination;
    }

    // Update is called once per frame
    void Update()
    {
        //If the player is controling the guard, sets the controls. Else it will automaticaly go for the spy.
        if (player)
        {
            if (Input.GetMouseButton(0))
            {
                SetDestinationToMousePos();
            }
        }
        else
        {
            if (Vector3.Distance(this.transform.position, spy.transform.position) != 0)
            {
                destination = spy.transform.position;
                agent.destination = destination;
            }
        }
    }

    void SetDestinationToMousePos()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            agent.SetDestination(hit.point);
        }
    }

    
    void OnCollisionEnter(Collision col)
    {
        if(col.gameObject.tag == "Spy")
        {
            caught = true;
        }
    }
}
