using UnityEngine;
using Photon.Pun;
using System.IO;


public class EnemySpawner : MonoBehaviourPunCallbacks
{
    //public GameObject enemyPrefab;
    //float spawnInterval = 100f;
    GameObject enemyObject;
    PhotonView PV;

    private float spawnTime;

    public int enemyCount;

    public static EnemySpawner instance;

    private void Awake()
    {
        if (instance)
        {
            Destroy(gameObject);
            return;
        }
        //DontDestroyOnLoad(gameObject);
        instance = this;
        PV= GetComponent<PhotonView>();
        
    }

    //public override void OnLobbyStatisticsUpdate()
    //{
    //    string countPlayersOnline;
    //    countPlayersOnline = PhotonNetwork.countOfPlayers.ToString() + " Players Online";
    //}
    private void Start()
    {
        //spawnTime = Time.time + spawnInterval;
        SpawnEnemy();
        //if (PhotonNetwork.CountOfPlayers == 2)
        //{
        //    SpawnEnemy();
        //}
        Debug.Log(enemyCount.ToString());
    }
    private void Update()
    {
        //Debug.Log(enemyCount.ToString());
    }
    //private void Update()
    //{


    //    //if (PhotonNetwork.IsMasterClient && Time.time >= spawnTime)
    //    //{
    //    //    SpawnEnemy();
    //    //    spawnTime = Time.time + spawnInterval;
    //    //}
    //}

    private void SpawnEnemy()
    {
        enemyCount++;
        Transform spawn = SpawnManager.instance.GetRandomenemySpawn();
        Vector3 spawnPosition = spawn.position + new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
        enemyObject = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player 3"), spawnPosition, Quaternion.identity, 0, new object[] {PV.ViewID} );
    }

    //public void Die()
    //{
    //    PhotonNetwork.Destroy(enemyObject);

    //}
}
