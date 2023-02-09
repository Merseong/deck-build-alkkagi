using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : SingletonBehavior<ParticleManager>
{
    public readonly Dictionary<GameObject, List<GameObject>> particleDic = new();

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

        particleToPlay.transform.position = position;        
        particleToPlay.SetActive(true);

        if(particleToPlay == null) yield break;
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