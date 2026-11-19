using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.ParticleSystem;
using System.Linq;

public class SpiceSource : MonoBehaviour
{
    [SerializeField] ParticleSystem parts;
    [SerializeField] float collectBufferTime;
    [SerializeField] float timeTilIncreaseSpeed;
    [SerializeField] float speedIncrease;
    [SerializeField] float transitionToSuckTime;
    [SerializeField] float collectRange;

    List<ParticleCollisionEvent> collisionEvents;
    bool inBuffer = true;

    public void Emit(int count, float rot)
    {
        transform.localEulerAngles = new Vector3(0, 0, rot);
        parts.Emit(count);
        collisionEvents = new();
        parts.externalForces.AddInfluence(PlayerEntityManager.Singleton.GetComponentInChildren<ParticleSystemForceField>());
        StartCoroutine(Wait());

        IEnumerator Wait()
        {
            yield return new WaitForSeconds(collectBufferTime);
            inBuffer = false;
            yield return new WaitForSeconds(timeTilIncreaseSpeed);

            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[parts.main.maxParticles];
            int numParticlesAlive = parts.GetParticles(particles);

            Vector3[] ogVels = particles.Select(x => x.velocity).ToArray();

            float time = 0;
            while (time < transitionToSuckTime)
            {
                numParticlesAlive = parts.GetParticles(particles);
                for (int j = 0; j < numParticlesAlive; j++)
                {
                    particles[j].velocity = Vector3.Lerp(ogVels[j], 
                        ((Vector3)PlayerEntityManager.Singleton.GetPlayerPosition() - particles[j].position).normalized * speedIncrease,
                        time / transitionToSuckTime);
                }
                parts.SetParticles(particles, numParticlesAlive);
                yield return new WaitForFixedUpdate();
                time += Time.deltaTime;
            }

            while(true)
            {
                numParticlesAlive = parts.GetParticles(particles);
                if (numParticlesAlive <= 0) break;
                for (int j = 0; j < numParticlesAlive; j++)
                {
                    particles[j].velocity = ((Vector3)PlayerEntityManager.Singleton.GetPlayerPosition() - particles[j].position).normalized * speedIncrease;
                }
                parts.SetParticles(particles, numParticlesAlive);
                yield return new WaitForFixedUpdate();
            }
            Destroy(gameObject);
        }
    }

    void OnParticleCollision(GameObject other)
    {
        Debug.Log("collide");
        if (inBuffer || !other.CompareTag("Player")) return;

        int numCollisionEvents = parts.GetCollisionEvents(other, collisionEvents);

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[parts.main.maxParticles];
        int numParticlesAlive = parts.GetParticles(particles);

        for (int i = 0; i < numCollisionEvents; i++)
        {
            Vector3 collisionPos = collisionEvents[i].intersection;

            // Iterate through alive particles to find the one that collided
            for (int j = 0; j < numParticlesAlive; j++)
            {
                // Check distance to determine if this particle caused the collision
                if (Vector3.Distance(particles[j].position, collisionPos) < collectRange)
                {
                    // Disable the particle by setting remaining lifetime to 0 or negative
                    particles[j].remainingLifetime = 0f;
                    break; // Stop checking once the specific particle is found/removed
                }
            }
        }

        // Apply changes back to the particle system
        parts.SetParticles(particles, numParticlesAlive);
    }
}
