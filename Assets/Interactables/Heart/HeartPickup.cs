using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartPickup : Destroyables
{
    [Header("IGNORE ABOVE")]
    [SerializeField] float defaultHitAngle = 1.5f;
    [SerializeField] float percentageLerpAngle = .3f;
    [SerializeField] float startVelocity;
    [SerializeField] float dragCoefficient;
    [SerializeField] float spawnOffsetFromGround;
    [SerializeField] float gravity;
    [SerializeField] float swingAmount;
    [SerializeField] float swingFrequency;
    [SerializeField] float influenceOfHittingDownOnGroundOffset = .5f;
    [SerializeField] FlavorIngredient.InflictionFlavor HealthInfliction;

    public void Spawn(Vector2 hitDir)
    {
        StartCoroutine(ShootAndFall());
        IEnumerator ShootAndFall()
        {

            Vector2 targetDir = hitDir.x > 0 ? new Vector2(1, defaultHitAngle).normalized : new Vector2(-1, defaultHitAngle).normalized;
            Vector2 trueHitDir = (percentageLerpAngle * hitDir + targetDir).normalized;
            float spawnY = transform.position.y;
            float groundY = spawnY - spawnOffsetFromGround;
            if (hitDir.y < 0) groundY += hitDir.y * influenceOfHittingDownOnGroundOffset;
            Vector2 vel = trueHitDir * startVelocity;
            while (transform.position.y > groundY)
            {
                transform.position = new Vector3(transform.position.x + vel.x, transform.position.y + vel.y, transform.position.z);
                vel += dragCoefficient * Mathf.Pow(vel.magnitude, 2) * -vel.normalized;
                float swing = vel.y > 0 ? 0 : Mathf.Sin(swingFrequency * Time.time) * -vel.y * swingAmount;
                vel = new Vector2(vel.x + swing, vel.y - gravity);
                yield return new WaitForFixedUpdate();
            }
        }
    }

    public override void RemoveDestroyable(GameObject source, Vector2 hitDir = default)
    {
        if (!source.CompareTag("Player")) return;
        Collect();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        Collect();
    }

    void Collect()
    {
        var inflictions = new List<FinishedSoup.SoupInflictionStat>();
        FinishedSoup.SoupInflictionStat spoonInfliction = new FinishedSoup.SoupInflictionStat(HealthInfliction.inflictionType);
        spoonInfliction.Add(HealthInfliction.amount);
        inflictions.Add(spoonInfliction);
        PlayerEntityManager.Singleton.ApplyInfliction(inflictions, transform);
        Destroy(gameObject);
    }
}
