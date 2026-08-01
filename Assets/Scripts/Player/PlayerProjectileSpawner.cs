using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public class PlayerProjectileSpawner : MonoBehaviour {


	[Header("Input")]
	public KeyCode spawnKey = KeyCode.Mouse0;


	[Header("Spawner Settings")]
	public GameObject projectilePrefab;
	public Transform spawnPoint;

	public float spawnRate;
	private float timer;


	[Header("Particles")]
	public ParticleSystem spawnParticles;


	[Header("Audio")]
	public AudioSource spawnAudioSource;


	
	void Update()
	{
		timer += Time.deltaTime;
		var firePressed = false;
		
#if MOBILE_INPUT
		firePressed = CrossPlatformInputManager.GetButton("Fire1");
#else
		firePressed = Input.GetKey(spawnKey);
#endif
		
		if(firePressed && timer >= spawnRate)
		{
			SpawnProjectile();
		}

	}
	

	void SpawnProjectile()
	{
		timer = 0f;
		Instantiate(projectilePrefab, spawnPoint.position, spawnPoint.rotation);

		if(spawnParticles)
		{
			spawnParticles.Play();
		}

		if(spawnAudioSource)
		{
			spawnAudioSource.Play();
		}

	}

}
