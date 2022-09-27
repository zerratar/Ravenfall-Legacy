using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestAStarAgent : MonoBehaviour
{
    [SerializeField] private RichAI richAi;
    [SerializeField] private Transform target;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!richAi || !target)
            return;

        //Pathfinding.Drawing.Draw.SolidBox(new Unity.Mathematics.float3(0, 0, 0), new Unity.Mathematics.float3(1, 1, 1));

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            richAi.isStopped = !richAi.isStopped;
            if (richAi.isStopped)
            {
                richAi.destination = transform.position;
                richAi.canMove = false;
            }
            else
            {
                richAi.canMove = true;
            }
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            richAi.Teleport(new Vector3(0, 0, 0));
        }
        else
        {
            richAi.destination = target.position;
        }
    }
}
