using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public enum Draw
//{
//    HUMAN,
//    HORSE,
//    JEEP,
//    TANK,
//    NONE
//};


public class DrawLine : MonoBehaviour
{

    public static DrawLine Instance;

    [Space(20)]
    [Header(" *__________________[Level-Prefabs]___________________*")]
    public Camera mainCamera;
    public LineRenderer drawLineRenderer;
    public float distCheck;
    public Transform playerSpawnPoint;
    public List<Vector3> worldLinePoints = new List<Vector3>();
    public GameObject[] playerPrefab;
    public GameObject[] horsesPrefab;
    public GameObject[] jeepPrefab;
    public GameObject[] tanksPrefab;
    public GameObject playerHorseForm, playerJeepForm, playerBossForm, playerTankForm;

    [Space(20)]
    [Header(" *__________________[Draw Variables]___________________*")]
    //public Draw drawType;
    public float playerWidth;
    public List<GameObject> totalLineDraws = new List<GameObject>();
    public bool drawFlag = false;
    public GameObject drawArea;
    public Color redColor;
    public Color greenColor;
    public float maxInk, usedInk, inkRefillRate;

    private List<Vector3> linePoints = new List<Vector3>();
    private Vector3 hitPos;
    int randomPlayer;
    private float speed = 0.1f;

    int totalSpheres;
    List<Vector3> center = new List<Vector3>();
    List<float> radius = new List<float>();

    internal bool isSpecialPlayersTime;
    internal float fever = 0;

    bool canVibrate;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        isSpecialPlayersTime = false;
        AllUIManager.Instance.feverBar.fillAmount = 0;
        fever = 0;
        drawArea.GetComponent<MeshRenderer>();
        drawLineRenderer.positionCount = 0;


        HapticFeedback.SetVibrationOn(true);

        //if (GamePlayManager.Instance.enemy == EnemyType.Humans)
        //{
        //    playerWidth = humanWidth;
        //}
        //else if (GamePlayManager.Instance.enemy == EnemyType.Horses)
        //{
        //    playerWidth = horseWidth;
        //}
        //else if (GamePlayManager.Instance.enemy == EnemyType.Jeeps)
        //{
        //    playerWidth = jeepWidth;
        //}
        //else if(GamePlayManager.Instance.enemy == EnemyType.Tanks)
        //{
        //    playerWidth = tankWidth;
        //}
        //else if (GamePlayManager.Instance.enemy == EnemyType.Bosses)
        //{
        //    playerWidth = tankWidth;
        //}

    }

    void Update()
    {

        if (fever >= 1 && !isSpecialPlayersTime)
        {
            //empty bar
            StartCoroutine(SpecialPlayerSpawn());
        }


        if (GameDataManager.Instance.currLayer == "GamePlay" && !AllUIManager.Instance.isFinished)
        {
            if (Input.GetMouseButtonDown(0))
            {
                drawFlag = true;
                canVibrate = true;
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 5);
                if (Physics.Raycast(ray, out RaycastHit hitInfo, 100, LayerMask.GetMask("DrawArea")))
                {
                    hitPos = hitInfo.point;
                    linePoints.Add(hitPos);
                    drawLineRenderer.positionCount = linePoints.Count;
                    drawLineRenderer.SetPositions(linePoints.ToArray());
                    //  print("Hit");
                }
                else
                {
                    // print("No Hit");
                }
            }
            else if (Input.GetMouseButton(0))
            {

                drawFlag = true;

                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hitInfo, 100, LayerMask.GetMask("DrawArea")) &&
                     usedInk < maxInk)
                {
                    hitPos = hitInfo.point;
                    float currDistance = Vector3.Distance(hitPos, linePoints[linePoints.Count - 1]);
                    if (linePoints.Count == 0 || currDistance > distCheck)
                    {
                        Vector3 worldPoint = new Vector3(hitPos.x, hitPos.y -0.5f, hitPos.z);
                        Collider[] cols = Physics.OverlapSphere(worldPoint, playerWidth, LayerMask.GetMask("Player"));

                        if (cols.Length == 0 && !isSpecialPlayersTime)// && GamePlayManager.Instance.enemy == EnemyType.Humans)
                        {
                            randomPlayer = Random.Range(0, playerPrefab.Length);
                            //center.Add(Vector3.Lerp(worldLinePoints[i - 1], worldLinePoints[i], j / (float)numPlayers));
                            //radius.Add(playerWidth);

                            Instantiate(playerPrefab[randomPlayer], worldPoint, Quaternion.identity);

                            if (canVibrate)
                            {
                                canVibrate = false;
                                HapticFeedback.Vibrate(UIFeedbackType.ImpactLight);
                                LeanTween.delayedCall(gameObject, 0.15f, () => canVibrate = true);
                            }

                        }

                        linePoints.Add(hitPos);
                        if (linePoints.Count > 1)
                        {
                            //Debug.Log("1");
                            usedInk += currDistance;
                            AllUIManager.Instance.gp_ProgressBar.fillAmount = 1 - usedInk / maxInk;
                        }
                        drawLineRenderer.positionCount = linePoints.Count;
                        drawLineRenderer.SetPositions(linePoints.ToArray());
                    }
                    else
                    {
                        //  print("Too Close");
                    }

                    //   print("Hit");
                }
                //else
                //{
                //    // print("No Hit");
                //    FinishLine();
                //}
            }
            else if (Input.GetMouseButtonUp(0))
            {
                drawFlag = false;
                linePoints.Clear();
                drawLineRenderer.positionCount = 0;
                //StartCoroutine(FinishLine());

                if(GameDataManager.Instance.GetPlayerLevel() == 1)
                {
                    AllUIManager.Instance.tutScreen.SetActive(false);
                }

            }
            else if (!Input.GetMouseButton(0))
            {
                if (usedInk > 0)
                {
                    //Debug.Log("2");
                    usedInk -= inkRefillRate * Time.deltaTime;
                }
                else
                {
                    //Debug.Log("3");
                    usedInk = 0;
                }
                AllUIManager.Instance.gp_ProgressBar.fillAmount = 1 - usedInk / maxInk;
            }
        }
    }

    IEnumerator FinishLine()
    {
        if (linePoints.Count > 1)
        {
            worldLinePoints = new List<Vector3>(linePoints);

            //    Debug.Log("before : " + worldLinePoints.Count);

            for (int i = 1; i < worldLinePoints.Count; i++)
            {
                //      Debug.Log(Vector3.Distance(worldLinePoints[i], worldLinePoints[i - 1]));
                if (Vector3.Distance(worldLinePoints[i], worldLinePoints[i - 1]) < 0.3f)
                {
                    worldLinePoints.RemoveAt(i);
                    i--;
                }
            }
            //      Debug.Log("after : " + worldLinePoints.Count);

            float minZ = 9999f;

            foreach (Vector3 v in worldLinePoints)
            {
                if (v.z < minZ)
                {
                    minZ = v.z;
                }
            }
            float addZ = playerSpawnPoint.position.z - minZ;
            float playerHeightOffset = -0.5f;

            for (int i = 0; i < worldLinePoints.Count; i++)
            {
                worldLinePoints[i] += new Vector3(0, playerHeightOffset, addZ);
            }

            // to store lines num of players
            GameObject g = new GameObject();

            totalLineDraws.Add(g);

            g.name = "line " + totalLineDraws.Count;

            for (int i = 1; i < worldLinePoints.Count; i++)
            {
                Vector3 dir = (worldLinePoints[i] - worldLinePoints[i - 1]).normalized;

                float dist = Vector3.Distance(worldLinePoints[i - 1], worldLinePoints[i]);

                int numPlayers = Mathf.RoundToInt(dist / playerWidth);

                //         print(numPlayers + " = " + dist + "/" + playerWidth);

                if (numPlayers == 0)
                {
                    numPlayers = 1;
                }
                for (int j = 0; j < numPlayers; j++)
                {
                    //GameObject ball = Instantiate(playerPrefab, Vector3.Lerp( worldLinePoints[i - 1],worldLinePoints[i], j/(float)numPlayers), Quaternion.identity);
                    //Debug.DrawLine(ball.transform.position - new Vector3(playerWidth / 2,0.3f, 0), ball.transform.position - new Vector3(-playerWidth / 2, 0.3f, 0), Color.red, 10);
                    Vector3 point = Vector3.Lerp(worldLinePoints[i - 1], worldLinePoints[i], j / (float)numPlayers);
                    Collider[] cols = Physics.OverlapSphere(point, playerWidth, LayerMask.GetMask("Player"));

                    if (cols.Length == 0 && !isSpecialPlayersTime)// && GamePlayManager.Instance.enemy == EnemyType.Humans)
                    {
                        randomPlayer = Random.Range(0, playerPrefab.Length);
                        center.Add(Vector3.Lerp(worldLinePoints[i - 1], worldLinePoints[i], j / (float)numPlayers));
                        radius.Add(playerWidth);

                        Instantiate(playerPrefab[randomPlayer], point, Quaternion.identity).transform.SetParent(g.transform);
                    }

                    #region Test PlayerType                 

                    //else if (cols.Length == 0 && GamePlayManager.Instance.enemy == EnemyType.Horses)
                    //{
                    //    randomPlayer = Random.Range(0, horsesPrefab.Length);
                    //    center.Add(Vector3.Lerp(worldLinePoints[i - 1], worldLinePoints[i], j / (float)numPlayers));
                    //    radius.Add(playerWidth);

                    //    Instantiate(horsesPrefab[randomPlayer], point, Quaternion.identity).transform.SetParent(g.transform);
                    //    yield return null;
                    //}

                    //if (cols.Length == 0 && GamePlayManager.Instance.enemy == EnemyType.Jeeps)
                    //{
                    //    randomPlayer = Random.Range(0, jeepPrefab.Length);
                    //    center.Add(Vector3.Lerp(worldLinePoints[i - 1], worldLinePoints[i], j / (float)numPlayers));
                    //    radius.Add(playerWidth);

                    //    Instantiate(jeepPrefab[randomPlayer], point, Quaternion.identity).transform.SetParent(g.transform);
                    //    yield return null;
                    //}

                    //if (cols.Length == 0 && GamePlayManager.Instance.enemy == EnemyType.Tanks)
                    //{
                    //    randomPlayer = Random.Range(0, tanksPrefab.Length);
                    //    center.Add(Vector3.Lerp(worldLinePoints[i - 1], worldLinePoints[i], j / (float)numPlayers));
                    //    radius.Add(playerWidth);

                    //    Instantiate(tanksPrefab[randomPlayer], point, Quaternion.identity).transform.SetParent(g.transform);
                    //    yield return null;
                    //}
                    #endregion
                    else
                    {
                        //Debug.Log("overlap else");
                        for (int k = 0; k < cols.Length; k++)
                        {
                            Debug.Log(LayerMask.LayerToName(cols[k].gameObject.layer));
                        }
                        //  Instantiate(playerPrefabRed, worldLinePoints[i - 1] + dir * (j * playerWidth), Quaternion.identity);

                    }
                }
            }
            //Debug.Break();

            //Debug.DrawLine(worldLinePoints[0], worldLinePoints[1], Color.black, 10);
            //for (int i = 1; i < worldLinePoints.Count; i++)
            //{
            //    Debug.DrawLine(worldLinePoints[i - 1], worldLinePoints[i], Color.black, 10);
            //}

        }

        StartCoroutine(March(totalLineDraws[totalLineDraws.Count - 1].transform));

        linePoints.Clear();
        drawLineRenderer.positionCount = 0;
        yield return null;

    }

    IEnumerator March(Transform t)
    {
        while (true)
        {
            yield return null;
            t.Translate(Vector3.forward * Time.deltaTime * speed);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.blue;
        for (int i = 0; i < center.Count; i++)
        {
            Gizmos.DrawSphere(center[i], radius[i]);
        }
    }


    internal void DeploySpecialTroops()
    {
        if (GamePlayManager.Instance.enemy == EnemyType.Humans)
        {
            Instantiate(playerBossForm, playerSpawnPoint.position, Quaternion.identity);
        }
        if (GamePlayManager.Instance.enemy == EnemyType.Bosses)
        {
            Instantiate(playerBossForm, playerSpawnPoint.position, Quaternion.identity);
        }
        if (GamePlayManager.Instance.enemy == EnemyType.Horses)
        {
            Instantiate(playerHorseForm, playerSpawnPoint.position, Quaternion.identity);
        }
        else if (GamePlayManager.Instance.enemy == EnemyType.Jeeps)
        {
            print("Spawn Jeeps");
            Instantiate(playerJeepForm, playerSpawnPoint.position, Quaternion.identity);
        }
        else if (GamePlayManager.Instance.enemy == EnemyType.Tanks)
        {
            Instantiate(playerTankForm, playerSpawnPoint.position, Quaternion.identity);
        }
        fever = 0;
        AllUIManager.Instance.feverBar.fillAmount = 0;
        //LeanTween.cancel(gameObject);
        //StartCoroutine(ResetFeverBar());
        isSpecialPlayersTime = false;

    }


    IEnumerator SpecialPlayerSpawn()
    {
        isSpecialPlayersTime = true;
        yield return new WaitForSeconds(0.5f);
        AllUIManager.Instance.inkBar.SetActive(false);
        AllUIManager.Instance.deployBtn.SetActive(true);
    }

    IEnumerator ResetFeverBar()
    {
        fever = 0;
        AllUIManager.Instance.inkBar.SetActive(true);
        AllUIManager.Instance.deployBtn.SetActive(false);
        while (AllUIManager.Instance.feverBar.fillAmount >= 0)
        {
            yield return new WaitForSeconds(0.04f);
            AllUIManager.Instance.feverBar.fillAmount -= 0.1f;
        }
    }

}
