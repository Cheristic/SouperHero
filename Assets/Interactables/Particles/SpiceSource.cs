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

        StartCoroutine(Wait());

        IEnumerator Wait()
        {
            yield return new WaitForSeconds(collectBufferTime);
            parts.externalForces.AddInfluence(PlayerEntityManager.Singleton.GetComponentInChildren<ParticleSystemForceField>());
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
        if (inBuffer || !other.CompareTag("Player")) return;

        int numCollisionEvents = parts.GetCollisionEvents(other, collisionEvents);

        for (int i = 0; i < numCollisionEvents; i++)
        {
            BasketUI.Singleton.IncrementSpiceCount();
        }
    }
}
