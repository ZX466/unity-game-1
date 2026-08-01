using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;

public class DoorOpen : MonoBehaviour
{
    [SerializeField]
    private PlayableDirector PlayableDirector;

    [SerializeField]
    private float WaitTimeInSeconds;
    
    private float StayTime;
    private Collider[] Triggers;

    private void Awake()
    {
        StayTime = 0;
        Triggers = GetComponentsInChildren<Collider>().Where(c => c.isTrigger).ToArray();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            StayTime += Time.deltaTime;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            StayTime = 0;
        }
    }

    private void Update()
    {
        if (StayTime > WaitTimeInSeconds)
        {
            if (PlayableDirector != null)
            {
                StayTime = float.MinValue;
                foreach (var trigger in Triggers)
                {
                    if (trigger.isTrigger)
                    {
                        trigger.enabled = false;
                    }
                }
                PlayableDirector.stopped += pd =>
                {
                    gameObject.SetActive(false);
                };
                PlayableDirector.Play();
            }
        }
    }
}