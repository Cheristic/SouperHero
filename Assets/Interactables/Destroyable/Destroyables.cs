using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroyables : MonoBehaviour
{
    public static event Action<Vector3> Destroyed;
    public bool randDrop = false;
    [SerializeField] private Collectable singleCollectable;
    public List<Collectable> multipleCollectables;
    [SerializeField] private float oddsForSomething = .5f;
    public bool DestroyedOnDamage = true;
    [SerializeField] DestructableParticleCreator destroyedParticles;
    [SerializeField] int hitsToBreak = 1;
    [SerializeField] float spawnCollectableOffsetY = -1;
    [SerializeField] float bounceAmount = 0;
    [SerializeField] float groundOffsetY = 0;
    [SerializeField] SpiceSource spiceSource;
    [SerializeField] Vector2Int spiceToDrop;

    int forageableIndex = -1;
    public void TrackSpawn(int index, List<Collectable> newList)
    {
        forageableIndex = index;
        if (randDrop) multipleCollectables = newList;
    }

    public void RemoveDestroyable(Vector2 hitDir = default)
    {
        if (!DestroyedOnDamage) return;

        hitsToBreak--;
        destroyedParticles?.Hit();

        if (hitsToBreak > 0) return;

        Destroy(this.gameObject);

        if (!randDrop)
        {
            if (singleCollectable != null)
            {
                GameObject gameObj = Instantiate(singleCollectable.gameObject, new Vector2(this.transform.position.x, this.transform.position.y), Quaternion.identity);
                gameObj.GetComponent<Collectable>().Spawn(this.transform.position, Vector2.up, bounceAmount, 2, spawnCollectableOffsetY, groundOffsetY);
                Destroyed?.Invoke(transform.position);
            }
        } else
        {
            if (multipleCollectables != null)
            {
                if (UnityEngine.Random.Range(0f, 1f) < oddsForSomething)
                {
                    int randomIndex = UnityEngine.Random.Range(0, multipleCollectables.Count);
                    GameObject gameObj = Instantiate(multipleCollectables[randomIndex].gameObject, new Vector2(this.transform.position.x, this.transform.position.y), Quaternion.identity);
                    gameObj.GetComponent<Collectable>().Spawn(this.transform.position, Vector2.up, bounceAmount, 2, spawnCollectableOffsetY, groundOffsetY);
                }
                Destroyed?.Invoke(transform.position);
            }
        }

        if (forageableIndex >= 0) RunStateManager.Singleton.TrackBrokenDestroyable(forageableIndex);


        if (spiceSource != null)
        {
            float angle = Mathf.Rad2Deg * Mathf.Atan2(hitDir.y, hitDir.x);
            SpiceSource source = Instantiate(spiceSource, transform.position, Quaternion.identity);
            source.Emit(UnityEngine.Random.Range(spiceToDrop.x, spiceToDrop.y), angle);
        }
       

        destroyedParticles?.Break();
    }

    public void ManualDestroy()
    {
        if (forageableIndex >= 0) RunStateManager.Singleton.TrackBrokenDestroyable(forageableIndex);
    }
}
