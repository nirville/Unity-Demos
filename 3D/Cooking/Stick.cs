using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class Stick : MonoBehaviour
{
    public static Stick Instance;


    [Space(20)]
    [Header("Stick Variables")]
    public Transform tipPoint;
    public Transform endPoint;
    public Transform fruitsParent, forkPoint;
    //public LayerMask layerM;

    public ParticleSystem pulpFX, smokeFX;

    internal List<GameObject> pokedFruits = new List<GameObject>();
    internal bool isFruitPoked;
    internal bool isMaxItemCollected, isSwiping;
    internal bool isRotating;
    internal float stickRotateTimer = 0;


    internal bool isSmokyGrilled, isCooked;
    internal Collider stickCol;
    internal Rigidbody stickRb;

    internal float barbequedTime;
    int foodForked = 0;

    Vector3 stickInitPos, stickForkInitPos;
    float cooldownTimer = 0;
    Ray stickRay;
    RaycastHit hitInfo;
    int fruitCount = 0;
    GameObject lastFruit;
    Color lastFoodColor;
    internal float grillTimer, currMaxGrillTimer;
    public float sliderSpeed, grillDuration;
    public ParticleSystem grillSmoke_FX;

    float pokingDist;
    Vector3 pokePoint;

    [Space(20)]
    [Header("Stick Rotations")]
    public float rotSensitivity;
    private Vector3 mouseRef;
    private Vector3 mouseOffset;
    private Vector3 stickRot;



    private void Awake()
    {
        Instance = this;    
    }

    void Start()
    {
        HapticFeedback.SetVibrationOn(true);
        stickInitPos = transform.position;
        stickForkInitPos = forkPoint.position;
        //GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //sphere.transform.position = stickForkInitPos;
        //sphere.transform.localScale = Vector3.one * 0.01f;
        //sphere.transform.SetParent(fruitsParent);
        StartCoroutine(StickLoop());
        stickCol = GetComponent<Collider>();
        stickRb = GetComponent<Rigidbody>();

        pokePoint = stickInitPos;
        pokePoint.z = 0.015f;
        pokingDist = Vector3.Distance(transform.position, pokePoint);

    }

    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }



    IEnumerator StickLoop()
    {
        while(true)
        {

            if (Input.GetMouseButtonDown(0) && cooldownTimer > 0.1f && !isSwiping && GamePlay.Instance.isPokeArea && !IsPointerOverUIObject())
            {
                stickCol.enabled = true;
                UI.Instance.tutPoke.SetActive(false);
                cooldownTimer = 0;
                float currPokeDist = Vector3.Distance(transform.position, pokePoint);
                float pokeDuration = 0.18f * (currPokeDist) / (pokingDist);
                LeanTween.move(gameObject, pokePoint, pokeDuration).setEaseInOutQuart().setOnComplete(() => ResetStickToInitPos());
            }
            cooldownTimer += Time.deltaTime;

            if(GamePlay.Instance.isBarbequeArea)
            {
                // StartCoroutine(StartFryingTime());

                if (!isCooked)
                {
                    float normalizedGrillTimer= 0;

                    if (isRotating && mouseRef != Input.mousePosition)
                    {
                        mouseOffset = (Input.mousePosition - mouseRef);
                        stickRot.y = -(mouseOffset.x + mouseOffset.y) * rotSensitivity;
                        transform.Rotate(stickRot);
                        mouseRef = Input.mousePosition;
                        stickRotateTimer += Time.deltaTime;
                        grillTimer += Time.deltaTime;
                       // print(stickRot);
                    }
                    else if(currMaxGrillTimer / grillDuration < 0.85f)
                    {
                        grillTimer -= Time.deltaTime * sliderSpeed;
                        if (grillTimer < 0)
                        {
                            grillTimer = 0;
                        }
                    }

                    normalizedGrillTimer = grillTimer / grillDuration;
                    if (grillTimer > currMaxGrillTimer)
                    {
                        currMaxGrillTimer = grillTimer;
                        float normalizedMaxGrillTimer = currMaxGrillTimer/grillDuration;
                        if(normalizedMaxGrillTimer > 0.85f)
                        {
                            if(UI.Instance.barbequeTex.text != "Over Cooked!")
                            {
                                ParticleSystem.MainModule m = grillSmoke_FX.main;
                                ParticleSystem.EmissionModule e = grillSmoke_FX.emission;
                                e.rateOverTime = 9;
                                //m.startSpeed = 0.7f;
                                m.startColor = GamePlay.Instance.overCookedSmokeColor;
                                UI.Instance.barbequeTex.text = "Over Cooked!";
                                HapticFeedback.Vibrate(UIFeedbackType.ImpactLight);

                            }
                        }
                        else if (normalizedMaxGrillTimer > 0.6f)
                        {
                            if (UI.Instance.barbequeTex.text != "Cooked!")
                            {
                                ParticleSystem.MainModule m = grillSmoke_FX.main;
                                //m.startSpeed = 0.55f;
                                ParticleSystem.EmissionModule e = grillSmoke_FX.emission;
                                e.rateOverTime = 8;
                                m.startColor = Color.white;
                                UI.Instance.barbequeTex.text = "Cooked!";
                                HapticFeedback.Vibrate(UIFeedbackType.ImpactLight);

                            }
                        }
                        else if (normalizedMaxGrillTimer > 0.4f)
                        {
                            if (UI.Instance.barbequeTex.text != "Smoked!")
                            {
                                ParticleSystem.MainModule m = grillSmoke_FX.main;
                                //m.startSpeed = 0.4f;
                                ParticleSystem.EmissionModule e = grillSmoke_FX.emission;
                                e.rateOverTime = 7;
                                m.startColor = Color.white;
                                UI.Instance.barbequeTex.text = "Smoked!";
                                HapticFeedback.Vibrate(UIFeedbackType.ImpactLight);

                            }
                        }

                        Food[] food = fruitsParent.GetComponentsInChildren<Food>();
                        foreach (Food f in food)
                        {
                            f.FoodProcess(grillTimer / grillDuration);
                        }
                    }
                    UI.Instance.SetGrillSlider(grillTimer / grillDuration);

                    if (Input.GetMouseButtonDown(0))
                    {
                        UI.Instance.tutBarb.SetActive(false);

                        isRotating = true;
                        mouseRef = Input.mousePosition;

                        StartCoroutine(CallHaptics(true));
                    }
                    if (Input.GetMouseButtonUp(0))
                    {
                        isRotating = false;
                        StartCoroutine(CallHaptics(false));
                    }

                }
            }

            if(GamePlay.Instance.isPlatingArea)
            {
                if(fruitsParent.childCount <= 0)
                {
                    GamePlay.Instance.isPlatingArea = false;
                    GamePlay.Instance.subLevelConfett_FX.Play();
                    LeanTween.delayedCall(0.2f, () => GamePlay.Instance.CompleteOrderInPlating());
                    HapticFeedback.Vibrate(UIFeedbackType.ImpactLight);
                    GamePlay.Instance.confetti_FX.SetActive(true);
                }
            }


            if (foodForked >= GamePlay.Instance.maxOrderItems && !isMaxItemCollected)
            {
                GamePlay.Instance.subLevelConfett_FX.Play();
                //print("Order Complete");
                isMaxItemCollected = true;
                GamePlay.Instance.isPokeArea = false;
                HapticFeedback.Vibrate(UIFeedbackType.ImpactLight);

                GamePlay.Instance.CompareOrder();

                if(GamePlay.Instance.totalMatches.Equals(GamePlay.Instance.maxOrderItems))
                {
                    UI.Instance.AnimateStatusTex("awesome!", Color.magenta);
                }
                else
                {
                    UI.Instance.AnimateStatusTex("good!", Color.green);
                }

                Collider[] cols = fruitsParent.GetComponentsInChildren<Collider>();
                foreach (Collider c in cols)
                {
                    c.enabled = false;
                }
                LeanTween.delayedCall(1.2f, () =>
                 {
                     ParticleSystem.MainModule m = grillSmoke_FX.main;
                     m.startSpeed = 0.3f;
                     m.startColor = Color.white;
                     GamePlay.Instance.MoveToBarbequeStation();
                 });
                //GamePlay.Instance.MoveToBarbequeStation();
                GamePlay.Instance.DisableAllSwiperExcept(GamePlay.Instance.barbequeSwiper);
            }

            //if (barbequedTime > 0.5f && barbequedTime < 1 && !isSmokyGrilled)
            //{
            //    isSmokyGrilled = true;

            //    UI.Instance.barbequeTex.text = "Smoked!";

            //    Food[] food = fruitsParent.GetComponentsInChildren<Food>();
            //    foreach(Food f in food)
            //    {
            //        f.FoodProcess(FoodStage.SMOKYGRILLED);
            //    }
            //    //smokeFX.Play();
            //}

            //if(barbequedTime >= 1 && !isCooked)
            //{
            //    isCooked = true;
            //    isSmokyGrilled = true;
            //    UI.Instance.barbequeTex.text = "Cooked!";

            //    //change all slicetex
            //    Food[] food = fruitsParent.GetComponentsInChildren<Food>();
            //    foreach (Food f in food)
            //    {
            //        f.FoodProcess(FoodStage.COOKED);
            //    }
            //    GamePlay.Instance.subLevelConfett_FX.Play();

            //    //smokeFX.Stop();


            //    Collider[] cols = fruitsParent.GetComponentsInChildren<Collider>();
            //    foreach (Collider c in cols)
            //    {
            //        c.enabled = false;
            //    }

            //    LeanTween.delayedCall(1f,()=> GamePlay.Instance.MoveToPlatingStation());
            //    //ciomeote barnbequye
            //}
            if (grillTimer / grillDuration >= 0.6f && grillTimer/grillDuration < 0.85f && Input.GetMouseButtonUp(0) && !isCooked)
            {
                isCooked = true;
                isSmokyGrilled = true;
                UI.Instance.barbequeTex.text = "Cooked!";
                UI.Instance.AnimateStatusTex("awesome!", Color.magenta);

                GamePlay.Instance.totStarScored++;
               GamePlay.Instance.subLevelConfett_FX.Play();
                HapticFeedback.Vibrate(UIFeedbackType.ImpactLight);

                Collider[] cols = fruitsParent.GetComponentsInChildren<Collider>();
                foreach (Collider c in cols)
                {
                    c.enabled = false;
                }

                LeanTween.delayedCall(3f, () => GamePlay.Instance.MoveToPlatingStation());
                //ciomeote barnbequye
            }

            if (((grillTimer / grillDuration >= 0.85f && grillTimer / grillDuration <= 1f && Input.GetMouseButtonUp(0)) ||
                (grillTimer >= grillDuration)) && !isCooked)
            {
                isCooked = true;
                isSmokyGrilled = true;
                UI.Instance.barbequeTex.text = "Over Cooked!";
                UI.Instance.AnimateStatusTex("over cooked!", Color.red);
                //GamePlay.Instance.subLevelConfett_FX.Play();

                Collider[] cols = fruitsParent.GetComponentsInChildren<Collider>();
                foreach (Collider c in cols)
                {
                    c.enabled = false;
                }

                LeanTween.delayedCall(1f, () => GamePlay.Instance.MoveToPlatingStation());
                //ciomeote barnbequye
            }

            barbequedTime = stickRotateTimer / 10;
            //print(barbequedTime);
            yield return null;
        }
    }

    IEnumerator CallHaptics(bool hap)
    {
        while (hap)
        {
            HapticFeedback.Vibrate(UIFeedbackType.ImpactLight);
            yield return new WaitForSeconds(0.156f);
        }
    }


    void ResetStickToInitPos()
    {
        stickCol.enabled = false;

        if(isObstacle)
        {
            isObstacle = false;
            LeanTween.move(gameObject, stickInitPos, 0.08f).setOnComplete(() => Shake());
        }
        else
        {
            LeanTween.move(gameObject, stickInitPos, 0.3f).setEaseSpring();//.setOnComplete(()=> OnResetStickFunc());
        }

    }

    [EasyButtons.Button]
    internal void BreakStick()
    {
        Collider[] cols = fruitsParent.GetComponentsInChildren<Collider>();

        if(cols.Length != 0)
        {
            foreach (Collider c in cols)
            {
                c.enabled = true;
                c.attachedRigidbody.mass = 10;
                c.attachedRigidbody.drag = 0;
                c.attachedRigidbody.angularDrag = 0;
                c.attachedRigidbody.isKinematic = false;
                c.attachedRigidbody.useGravity = true;
            }
        }


        Collider[] stickPieces = GamePlay.Instance.sticks[GamePlay.Instance.currEnvIndex].GetComponentsInChildren<Collider>();
        foreach(Collider cx in stickPieces)
        {
            cx.enabled = true;
            cx.isTrigger = false;
            cx.attachedRigidbody.isKinematic = false;
            cx.attachedRigidbody.useGravity = true;
            cx.attachedRigidbody.AddExplosionForce(0.5f, GamePlay.Instance.sticks[GamePlay.Instance.currEnvIndex].transform.position, 1f, 3f, ForceMode.VelocityChange);
        }

    }


    void Shake()
    {
        LeanTween.move(gameObject, transform, 0.5f).setEaseShake();
    }
    
    [EasyButtons.Button]
    void OnResetStickFunc()
    {
        stickCol.enabled = true;
        if(isFruitPoked)
        {
            MoveFruitsBackForth();
        }
    }

    IEnumerator PokingFlag()
    {
        isFruitPoked = true;
        yield return new WaitForSeconds(0.8f);
        isFruitPoked = false;
    }

    //internal IEnumerator ShowPokedUI()
    //{

    //}
   




    bool isObstacle;
    string fruitId;
    private void OnTriggerEnter(Collider other)
    {
        //print("triggered" + other.name);

        if (other.gameObject.layer == LayerMask.NameToLayer("Food"))
        {
            fruitId = other.gameObject.tag;
            foodForked++;

            if (GamePlay.Instance != null)
            {
                GamePlay.Instance.recievedFoodOrder.Add(fruitId);
            }

            if(GamePlay.Instance.currLevel > 5)
            {
                PlateRotater.Instance.RandomMovement();
            }

            if(other.transform.childCount > 0)
            {
                other.transform.GetChild(0).gameObject.SetActive(false);
            }


            isFruitPoked = true;
            StartCoroutine(PokingFlag());
            other.enabled = false;
            lastFoodColor = other.GetComponentInChildren<Food>().foodColor;

            var main = pulpFX.main;
            main.startColor = lastFoodColor;
            lastFruit = other.gameObject;
            pulpFX.Play();

            UI.Instance.StartCoroutine(UI.Instance.SetRandomComment());

            other.gameObject.layer = LayerMask.NameToLayer("StickFood");
            other.enabled = true;
            other.attachedRigidbody.useGravity = false;
            other.attachedRigidbody.isKinematic = true;
            other.transform.SetParent(fruitsParent);
            other.transform.position = forkPoint.position;
            other.transform.eulerAngles = new Vector3(other.transform.eulerAngles.x, 0, other.transform.eulerAngles.z); 
            //other.transform.SetPositionAndRotation(forkPoint.position, Quaternion.identity);
            pokedFruits.Add(other.gameObject);
            MoveFruitsBackForth();
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            LeanTween.cancelAll(gameObject);
            UI.Instance.AnimateStatusTex("oops!", Color.red);
            //print("hit Obstacle");
            isObstacle = true;
            //LeanTween.move(gameObject, stickInitPos, 0.2f).setEaseShake();
            BreakStick();
            //UI.Instance.gamePanel.SetActive(false);
            LeanTween.delayedCall(1.2f, () => UI.Instance.Retry());

            SDK_Initialiser.LevelFailEvent(GamePlay.Instance.currLevel, 0);
        }
    }


    [EasyButtons.Button]
    void MoveFruitsBackForth()
    {
        if(fruitsParent.childCount != 0)
        {
            LeanTween.moveLocal(pokedFruits[pokedFruits.Count - 1].gameObject, pokedFruits[pokedFruits.Count - 1].transform.localPosition, 0.5f).setEaseSpring();
            foreach (GameObject go in pokedFruits)
            {
                //go.transform.localPosition = new Vector3(go.transform.localPosition.x, go.transform.localPosition.y, go.transform.localPosition.z + (-0.06f));
                go.transform.localPosition -= Vector3.up * 0.045f;

            }
        }

    }

    
    //public void LeftMoveStick()
    //{
    //    if((transform.eulerAngles.x < 90))
    //    {
    //        isRotating = true;
    //        LeanTween.rotateX(gameObject, transform.eulerAngles.x + 15f, 0.6f).setOnComplete(() => isRotating = false);
    //    }
    //    StartCoroutine(StartFryingTime());
    //}

    //public void RightMoveStick()
    //{
    //    //if ((transform.eulerAngles.x < -90))
    //    //{
    //        isRotating = true;
    //        //LeanTween.rotateX(gameObject, transform.eulerAngles.x - 15f, 0.6f).setOnComplete(() => isRotating = false);
    //    //}
    //    //StartCoroutine(StartFryingTime());
    //}


    public IEnumerator StartFryingTime()
    {
        while(GamePlay.Instance.isBarbequeArea)
        {

            if(isRotating)
            {
                stickRotateTimer += Time.deltaTime;
//                print("Frying timer " + stickRotateTimer.ToString("F1"));
                yield return null;
            }
            yield return null;
        }

    }
}
