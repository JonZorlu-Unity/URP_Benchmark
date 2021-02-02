using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HamCorGames.Benchmark
{
    public class ObjectCreator : MonoBehaviour
    {
        [SerializeField] private GameObject[] prefabsToSpawn = null;

        [SerializeField] private int totalObjectsToSpawn = 100;

        private void Start()
        {
            StartCoroutine(SpawnPrefabs());
        }

        private IEnumerator SpawnPrefabs()
        {
            for (int i = 0; i < totalObjectsToSpawn/5; i+=2)
            {
                for (int j = 0; j < totalObjectsToSpawn/5; j+=2)
                {
                    int randomIndex = Random.Range(0, prefabsToSpawn.Length);

                    Instantiate(prefabsToSpawn[randomIndex], new Vector3(i, j, 0), Quaternion.identity);
                    
                    yield return null;
                }
            }

        }



        


    }
}


