using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : SingletonBehavior<ParticleManager>
{
    public readonly Dictionary<GameObject, List<GameObject>> particleDic = new();

    public GameObject collideParticlePrefab;
    public GameObject directExitParticlePrefab;
    public ParticleSystem followingStonePrefab;

    public void RegisterParticle(GameObject particle)
    {
        if(particleDic.ContainsKey(particle)) return;

        List<GameObject> newList = new();
        
        GameObject go = Instantiate(particle, Vector3.zero, Quaternion.identity);
        go.SetActive(false);
        newList.Add(go);

        particleDic.Add(particle, newList);
    }

    public void DeregisterParticle(GameObject particle)
    {
        if(!particleDic.ContainsKey(particle)) return;

        particleDic.Remove(particle);
    }

    public IEnumerator PlayParticle(GameObject particle, Vector3 position)
    {
        GameObject particleToPlay = TryGetFromPool(particle);

        if(particleToPlay == null) yield break;

        particleToPlay.transform.position = position;        
        particleToPlay.SetActive(true);
        
        ParticleSystem ps = particleToPlay.GetComponent<ParticleSystem>();
        ps.Play();
        yield return new WaitForSeconds(ps.main.duration + ps.main.startLifetimeMultiplier);
        
        particleToPlay.SetActive(false);
    }

    public IEnumerator PlayParticle(GameObject particle, Vector3 position, float startSpeedMult, float rateMult)
    {
        GameObject particleToPlay = TryGetFromPool(particle);

        if(particleToPlay == null) yield break;

        particleToPlay.transform.position = position;
        particleToPlay.SetActive(true);

        ParticleSystem ps = particleToPlay.GetComponent<ParticleSystem>();
        var em = ps.emission;
        var main = ps.main;
        float originalRate = em.rateOverTimeMultiplier;
        float originalSpeed = main.startSpeedMultiplier;
        em.rateOverTimeMultiplier = em.rateOverTimeMultiplier * rateMult;
        main.startSpeedMultiplier = main.startSpeedMultiplier * startSpeedMult;

        ps.Play();
        yield return new WaitForSeconds(ps.main.duration + ps.main.startLifetimeMultiplier);
        
        em.rateOverTimeMultiplier = originalRate;
        main.startSpeedMultiplier = originalSpeed;

        particleToPlay.SetActive(false);
    }

    public IEnumerator PlayParticle(GameObject particle, Vector3 position, Vector3 rotation)
    {
        GameObject particleToPlay = TryGetFromPool(particle);

        if(particleToPlay == null) yield break;

        particleToPlay.transform.position = position;
        particleToPlay.transform.rotation = Quaternion.Euler(0f, Vector3.SignedAngle(Vector3.forward, rotation, Vector3.up), 0f);
        particleToPlay.SetActive(true); 
        
        ParticleSystem ps = particleToPlay.GetComponent<ParticleSystem>();
        ps.Play();
        yield return new WaitForSeconds(ps.main.duration + ps.main.startLifetimeMultiplier);
        
        particleToPlay.SetActive(false);
    }

    private GameObject TryGetFromPool(GameObject particle)
    {
        foreach(var ps in particleDic[particle])
        {
            if(!ps.activeInHierarchy) return ps;
        }

        GameObject go = Instantiate(particle, Vector3.zero, Quaternion.identity);
        go.SetActive(false);
        particleDic[particle].Add(go);

        return go;
    }
}