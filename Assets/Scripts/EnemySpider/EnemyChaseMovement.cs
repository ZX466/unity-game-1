using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyChaseMovement : MonoBehaviour {


	[Header("Chase Behaviour")]
	public Collider player;
	public NavMeshAgent agent;
	private bool playerInRange = false;
	private bool chasingPlayer = false;
	public Transform lineOfSightOrigin;
	private RaycastHit lineOfSightHit;

	[Header("Animation")]
	public Animator animator;


	public void PlayerIsNowInRange()
	{
		playerInRange = true;
	}

	public void PlayerIsNowOutOfRange()
	{
		playerInRange = false;

		if(chasingPlayer){
			StopChasingPlayer();
		}
	}

	void Update()
	{
		if(playerInRange && !chasingPlayer){
			
			Vector3 raycastDirectionTowardsPlayer = player.bounds.center - lineOfSightOrigin.position;

			Debug.DrawRay(lineOfSightOrigin.position, raycastDirectionTowardsPlayer * 100f, Color.red);

			if(Physics.Raycast(lineOfSightOrigin.position, raycastDirectionTowardsPlayer, out lineOfSightHit, 100f))
			{
				if(lineOfSightHit.collider.CompareTag("Player"))
				{
					BeginChasingPlayer();
				}
			}

		}

		if(chasingPlayer)
		{
			agent.SetDestination(player.bounds.center);
		}
	}

	public void BeginChasingPlayer()
	{
		chasingPlayer = true;
		SetNewAnimationState();
		agent.isStopped = false;
	}

	public void StopChasingPlayer()
	{
		chasingPlayer = false;
		SetNewAnimationState();
		agent.isStopped = true;
	}

	void SetNewAnimationState()
	{
		if(animator)
		{
			animator.SetBool("Chase Player", chasingPlayer);
		}

	}
	
}
