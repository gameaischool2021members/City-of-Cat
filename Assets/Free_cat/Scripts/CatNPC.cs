using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CatNPC : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    
    new public Renderer renderer;
    private Material material;

    public float maxIdleTime = 5f;
    public float maxWanderDistance = 10f;

    private bool isIdling = false;

    // Start is called before the first frame update
    void Start()
    {
        material = renderer.material;
        material.color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);

        ChooseIdleOrWander();
    }

    private void ChooseIdleOrWander()
    {
        float random = UnityEngine.Random.Range(0f, 1f);
        if (random < 0.5f)
            Wander();
        else
            StartCoroutine(Idle());
    }

    private void Wander()
    {
        Vector3 randDirection = Random.insideUnitSphere * maxWanderDistance;
        randDirection += transform.position;

        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randDirection, out navHit, maxWanderDistance, NavMesh.AllAreas))
        {
            animator.SetBool("walk", true);
            agent.SetDestination(navHit.position);
        }
    }

    IEnumerator Idle()
    {
        isIdling = true;
        animator.SetBool("walk", false);
        yield return new WaitForSeconds(Random.Range(1f, maxIdleTime));
        isIdling = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isIdling)
            return;
        
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            ChooseIdleOrWander();
        }
    }
}
