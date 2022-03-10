using MEC;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Painter : MonoBehaviour
{
    internal static Painter Instance;
    public Image canColorImage, canSprayImage;
    public ParticleSystem ps;
    public SpriteRenderer mainSR;
    public Texture2D tex;
    public Texture2D fillTex, curPeelTex;
    public Sprite[] paintDecals;
    Color[,] paintDecalColors;
    public float minDecalSize, maxDecalSize;
    public float paintDelay;
    int texHeight, texWidth, fillTexHeight, fillTexWidth;
    public Color drawColor;
    public float maxGap;
    public Transform paintBrush;
    Vector3 prePos, offset;
    Color[,] texColors;
    List<Color[,]> finalColors;
    bool isDrawing;
    bool canVibrate = true;
    public Color peelColor;
    internal CreativeInfo curCreative;
    internal int curSubCreativeIndex;
    List<SpriteRenderer> subCreativeSRs;
    List<SpriteRenderer> subGlitterSRs;
    public GameObject subCreativeSrPrefab;
    public GameObject peelSpriteRendererPrefab;
    public GameObject pasteSpritePrefab;
    public GameObject glitterSpritePrefab;
    public PoolerScript srPooler;
    public int numPixelsColored, numPixelsColoredInGrid;

    public bool canSpray, canFill;

    public int totSubCreatives;

    public GameObject kompleteSprayCanUI;
    public ParticleSystem KompleteSprayCan_FX;
    public Image kompleteSprayImg;

    public Vector3 clickOffset;

    Vector3 kompSprayCanUIInitPos;
    Vector2 lastCellPoint, PrevCellPoint;

    public Transform previewHolder;
    public Texture2D tintOverlayTex;

    [Space(20)]
    [Header("DRAW CANVAS")]
    public GameObject sprayCanvas;
    public GameObject previewHolderParent;


    //UI Stuff
    [Space(20)]
    [Header("BRUSH")]
    public GameObject patternCanvas;
    public GameObject[] patternBtn;
    public Texture2D[] patterns;
    public Texture2D currGlitTex;
    public bool isGlitterSpray;
    public Material glitMat;


    bool isLevelLoaded;
    internal static bool isStencilLoaded;

    private void Awake()
    {
        LeanTween.init(2000);
        StartCoroutine(PainterLoop());
        //if (!Application.isEditor)
        //{
        //    Application.targetFrameRate = 30;
        //}
        //else
        //{
        //    Application.targetFrameRate = 30;
        //}
        Instance = this;
        fillTexWidth = paintDecals[0].texture.width;
        fillTexHeight = paintDecals[0].texture.height;
        subCreativeSRs = new List<SpriteRenderer>();
        subGlitterSRs = new List<SpriteRenderer>();
        lastCellPoint = Vector2.zero;

        if(PlayerDataManager.Instance.isPlayerVIP())
        {
            UnlockAllGlitPatterns();
        }

        UI.Instance.paint = this;

        SetGameIdle();
        StartCoroutine(LoadStencil());
    }

    IEnumerator PainterLoop()
    {
        while(true)
        {
            if (Application.isEditor)
            {
                #region Editor
                if (Input.GetMouseButtonDown(0) && !IsPointerOverUIObject() && canSpray)
                {
                    AudioManager.Instance.PlaySprayCanSFX(true);
                    paintBrush.gameObject.SetActive(true);
                    kompleteSprayCanUI.SetActive(false);
                    prePos = Input.mousePosition;
                    offset = paintBrush.position - prePos;
                    isDrawing = true;
                }
                else if (Input.GetMouseButton(0) && isDrawing && canSpray)
                {
                    paintBrush.position = Input.mousePosition + clickOffset;
                    //print(paintBrush.position);
                    if (canVibrate)
                    {
                        canVibrate = false;
                        HapticFeedback.Vibrate(UIFeedbackType.ImpactLight);
                        LeanTween.delayedCall(gameObject, 0.15f, () => canVibrate = true);
                    }
                    CalculateDrawPoint(Input.mousePosition);

                    prePos = Input.mousePosition;

                    //print("Colored : " + numPixelsColored);
                    //UI.Instance.currSubCreatImg.fillAmount = (float)(numPixelsColored )/(curCreative.subCreatives[curSubCreativeIndex].pixelCount);
                    UI.Instance.newProgUiImg.fillAmount = (float)(numPixelsColored) / (curCreative.subCreatives[curSubCreativeIndex].pixelCount);
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    AudioManager.Instance.PlaySprayCanSFX(false);

                    isDrawing = false;
                    if (ps.isPlaying)
                    {
                        ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                        canSprayImage.gameObject.SetActive(false);
                    }

                    cellX = cellY = 0;
                }
                #endregion
            }

            if (Application.isMobilePlatform)
            {
                #region iOS/Android

                Debug.Log("Handheld Device Detected!");

                if (Input.touchCount > 0)
                {
                    if (Input.GetTouch(0).phase == TouchPhase.Began && !IsPointerOverUIObject() && canSpray)
                    {
                        AudioManager.Instance.PlaySprayCanSFX(true);
                        paintBrush.gameObject.SetActive(true);
                        kompleteSprayCanUI.SetActive(false);
                        prePos = Input.mousePosition;
                        offset = paintBrush.position - prePos;
                        isDrawing = true;
                    }
                    else if (Input.GetTouch(0).phase == TouchPhase.Moved && isDrawing && canSpray)
                    {
                        paintBrush.position = Input.mousePosition + clickOffset;
                        //print(paintBrush.position);
                        if (canVibrate)
                        {
                            canVibrate = false;
                            HapticFeedback.Vibrate(UIFeedbackType.ImpactLight);
                            LeanTween.delayedCall(gameObject, 0.15f, () => canVibrate = true);
                        }
                        CalculateDrawPoint(Input.mousePosition);

                        prePos = Input.mousePosition;

                        //print("Colored : " + numPixelsColored);
                        //UI.Instance.currSubCreatImg.fillAmount = (float)(numPixelsColored )/(curCreative.subCreatives[curSubCreativeIndex].pixelCount);
                        UI.Instance.newProgUiImg.fillAmount = (float)(numPixelsColored) / (curCreative.subCreatives[curSubCreativeIndex].pixelCount);
                    }
                    else if (Input.GetTouch(0).phase == TouchPhase.Ended)
                    {
                        AudioManager.Instance.PlaySprayCanSFX(false);

                        isDrawing = false;
                        if (ps.isPlaying)
                        {
                            ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                            canSprayImage.gameObject.SetActive(false);
                        }

                        cellX = cellY = 0;
                    }
                }
                #endregion
            }
            yield return null;
        }
    }
    public void EnableGlitterOn()
    {
        isGlitterSpray = true;
        mainSR.color = curCreative.subCreatives[curSubCreativeIndex].color;
        AudioManager.Instance.PlayUIClickSFX();
    }

    public void DisableGlitterOff()
    {
        isGlitterSpray = false;
        mainSR.color = Color.white;
        AudioManager.Instance.PlayUIClickSFX();

        foreach(GameObject bt in patternBtn)
        {
            bt.transform.GetChild(0).gameObject.SetActive(true);
            bt.transform.GetChild(1).gameObject.SetActive(false);
        }
    }

    public void SetUserSelectedGlitPattern(int num)
    {
        currGlitTex = patterns[num];
        glitMat.SetTexture("_GlitterTex", currGlitTex);
        patternBtn[num].transform.GetChild(0).gameObject.SetActive(false);
        patternBtn[num].transform.GetChild(1).gameObject.SetActive(true);
        EnableGlitterOn();
    }

    public void UnlockUserSelectedGlitPattern(int num)
    {
        patternBtn[num].transform.GetChild(0).gameObject.SetActive(true); //unselected
        patternBtn[num].transform.GetChild(1).gameObject.SetActive(false); //selected
        patternBtn[num].transform.GetChild(2).gameObject.SetActive(false); //ad
        patternBtn[num].transform.GetChild(3).gameObject.SetActive(false); //iap

        SetUserSelectedGlitPattern(num);
    }

    public void RequestToUnlockPatternWithID(string reqID)
    {
        AppLovinMaxSDKManager.Instance.userRewardIdPassed = reqID;
        GameEvents.AdVideoAvailableEvent(
            "rewarded",
            "OnPatternUnlock",
            AppLovinMaxSDKManager.Instance.isRewardedAdCurrentlyReady(),
            AppLovinMaxSDKManager.Instance.CheckInternetReachability()
            );
        AppLovinMaxSDKManager.Instance.ShowRewardedAd();

    }

    public void UnlockAllGlitPatterns()
    {
        foreach (GameObject b in patternBtn)
        {
            b.GetComponent<Transform>().GetChild(0).gameObject.SetActive(true);
            b.GetComponent<Transform>().GetChild(1).gameObject.SetActive(false);
            b.GetComponent<Transform>().GetChild(2).gameObject.SetActive(false);
            b.GetComponent<Transform>().GetChild(3).gameObject.SetActive(false);
        }
    }

    public void CallVIPShop()
    {
        UI.Instance.OnVIPButtonPress();
    }

    void SetTexAsTransparent()
    {
        Color transparentPixel = new Color(1, 1, 1, 0);
        for (int y = 0; y < texHeight; ++y)
        {
            for (int x = 0; x < texWidth; ++x)
            {
                tex.SetPixel(x, y, transparentPixel);
                texColors[x, y] = transparentPixel;
            }
        }
        tex.Apply();
    }

    IEnumerator LoadStencil()
    {
        yield return new WaitUntil(() =>
            isStencilLoaded == true);
        SetGameReady();
    }

    [EasyButtons.Button]
    internal void SetGameReady()
    {
        if(!isLevelLoaded)
        {
            patternCanvas.SetActive(true);
            UI.Instance.vipModeIndicator.SetActive(false);
            //print("GAME READY");
            isLevelLoaded = true;
            sprayCanvas.SetActive(true);
            previewHolderParent.SetActive(true);
            //StartCoroutine(AutoPaste(0, 1));
            LevelManager.Instance.SetLevel();

            //kompleteSprayCanUI.SetActive(true);

        }
    }

    internal void SetGameIdle()
    {
        sprayCanvas.SetActive(false);
        previewHolderParent.SetActive(false);
    }
    internal void SetCreative(CreativeInfo creative)
    {
        curCreative = creative;
        int w = 384, h = 384;
        texColors = new Color[w, h];
        finalColors = new List<Color[,]>();
        tex = new Texture2D(w, h, TextureFormat.ARGB32, false);
        texHeight = w;
        texWidth = h;
        SetTexAsTransparent();
        mainSR.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 75);
        Timing.RunCoroutine(ApplyTexture());
        paintDecalColors = new Color[fillTexWidth, fillTexHeight];
        for (int y = 0; y < fillTexHeight; ++y)
        {
            for (int x = 0; x < fillTexWidth; ++x)
            {
                paintDecalColors[x, y] = paintDecals[0].texture.GetPixel(x, y);
            }
        }

        totSubCreatives = curCreative.subCreatives.Length;

        for (int i = 0; i < totSubCreatives; i++)
        {
            Color[,] subCreativeColors = new Color[texWidth, texHeight];
            finalColors.Add(subCreativeColors);
            int x, y;
            Color[] temp = curCreative.subCreatives[i].texture.GetPixels();
            for (y = 0; y < texHeight; ++y)
            {
                for (x = 0; x < texWidth; ++x)
                {
                    subCreativeColors[x, y] = temp[x + y * texWidth];
                }
            }
        }
        curSubCreativeIndex = -1;
        StartCoroutine(AutoPaste(0, 1));
    }

    void SetSubCreative()
    {
        curSubCreativeIndex++;
        numPixelsColored = 0;
        curCreative.subCreatives[curSubCreativeIndex].SetPeel(Instantiate(peelSpriteRendererPrefab).GetComponentInChildren<PagePeeler>(), peelColor);
        curPeelTex = curCreative.subCreatives[curSubCreativeIndex].pagePeeler.frontPagePainteArea.sprite.texture;
        StartCoroutine(CalculateEveryGridPixels());

        if ((curSubCreativeIndex + 1) >= totSubCreatives)
        {
            //nextPeelTex = null;
        }
        else
        {
            //nextPeelTex = curCreative.subCreatives[curSubCreativeIndex + 1].texture;
            CreateNextPreview();
        }

        UI.Instance.SetCreativesItemFunc();

        SetColor(curCreative.subCreatives[curSubCreativeIndex].color);

        //Color[,] subCreativeColors = new Color[texWidth, texHeight];
        //finalColors.Add(subCreativeColors);
        //int x, y;
        //Color transparentPixel = new Color(1, 1, 1, 0);
        //for (y = 0; y < texHeight; ++y)
        //{
        //    for (x = 0; x < texWidth; ++x)
        //    {
        //        subCreativeColors[x, y] = curCreative.subCreatives[curSubCreativeIndex].texture.GetPixel(x,y);
        //    }
        //}
        canVibrate = true;
        canSpray = true;

        paintBrush.gameObject.SetActive(false);
        ps.gameObject.SetActive(true);

        kompSprayCanUIInitPos = kompleteSprayCanUI.transform.position;
        paintBrush.position = kompSprayCanUIInitPos;
        //kompleteSprayCanUI.SetActive(false);
        KompleteSprayCan_FX.Stop();

        if (isGlitterSpray)
        {
            mainSR.color = curCreative.subCreatives[curSubCreativeIndex].color;
        }
        else
        {
            mainSR.color = Color.white;
        }
        //print("Pixel Count : " + curCreative.subCreatives[curSubCreativeIndex].pixelCount);
    }

    internal void SetColor(Color color)
    {
        drawColor = color;
        ParticleSystem.MainModule m = ps.main;
        m.startColor = new Color(color.r, color.g, color.b, m.startColor.color.a);
        canColorImage.color = color;
        canSprayImage.color = color;
    }
    
    void CalculateDrawPoint(Vector3 mousePosition)
    {
        float dist = Vector3.Distance(prePos, mousePosition);
        if (dist < maxGap)
        {
            DrawAt(paintBrush.position);
        }
        else
        {
            Vector3 dir = (prePos - mousePosition).normalized;
            float numPoints = Mathf.CeilToInt(dist / maxGap);
            float delta = dist / numPoints;
            //print(numPoints);
            for (int i = 0; i < numPoints; i++)
            {
                DrawAt(paintBrush.position + (i * delta * dir));
            }
        }
    }

    IEnumerator<float> ApplyTexture()
    {
        yield return Timing.WaitForSeconds(1.05f);
        while (true)
        {
            tex.Apply();
            curPeelTex.Apply();
            yield return Timing.WaitForSeconds(paintDelay);
        }
    }
    void DrawAt(Vector3 point)
    {
        Ray ray = Camera.main.ScreenPointToRay(point);
        if (Physics.Raycast(ray, out RaycastHit hit, 40, LayerMask.GetMask("Texture")))
        {
            ps.transform.position = hit.point;

            SpriteRenderer sr = srPooler.GetActivePooledObject().GetComponent<SpriteRenderer>();
            sr.transform.position = hit.point;
            sr.transform.localScale = Vector3.one * 1f;
            sr.sprite = paintDecals[Random.Range(0, paintDecals.Length)];
            sr.color = curCreative.subCreatives[curSubCreativeIndex].color;
            if (isGlitterSpray)
            {
                sr.gameObject.SetActive(false);
            }
            else
            {
                sr.gameObject.SetActive(true);
            }

            Vector2Int pixelHit = new Vector2Int
            {
                x = (int)((Remap(hit.point.x, -2.56f, 2.56f, 0f, 1f) * texWidth)),
                y = (int)((Remap(hit.point.y, -2.56f, 2.56f, 0f, 1f) * texHeight))
            };

            //if (canFill)
            //{
                ApplyColorAt(pixelHit.x, pixelHit.y, sr);
            //}

            //LeanTween.scale(sr.gameObject, Vector3.one /** Random.Range(minDecalSize, maxDecalSize)*/, 0.3f).setOnComplete(() =>
            //{
            //});

            Debug.DrawRay(ray.origin, ray.direction * 40, Color.green);
            if (!ps.isPlaying)
            {
                ps.Play();
                canSprayImage.gameObject.SetActive(true);
            }
        }
        else
        {
            if (ps.isPlaying)
            {
                ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                canSprayImage.gameObject.SetActive(false);
            }
            Debug.DrawRay(ray.origin, ray.direction * 40, Color.red);
        }
    }

    //int gridSize = 64; // 8 * 8
    //int gridFilled = 0; //makezero on reload
    int cellSize = 48;
    int cellX = 0, cellY = 0;
    //int cellPixelCount = 0;

    void ApplyColorAt(int x, int y, SpriteRenderer sr)
    {
        //print(x + " , " + y);

        if (numPixelsColored >= curCreative.subCreatives[curSubCreativeIndex].pixelCount)
        {
            sr.gameObject.SetActive(false);
            return;
        }
        fillTex = sr.sprite.texture;
        for (int i = x - fillTexWidth / 2, i2 = 0; i < x + fillTexWidth / 2; i++, i2++)
        {
            for (int j = y - fillTexHeight / 2, j2 = 0; j < y + fillTexHeight / 2; j++, j2++)
            {
                if (i >= 0 && i < texWidth && j >= 0 && j < texHeight)
                {
                    Color color = fillTex.GetPixel(i2, j2);
                    if (color.a > 0.1f)
                    {
                        Color peelPixel = curPeelTex.GetPixel(i, j);
                        Color glit2Pixel = currGlitTex.GetPixel(i, j);
                        if (peelPixel.a < 0.01f)
                        {
                            if (!texColors[i, j].Compare(finalColors[curSubCreativeIndex][i, j]))
                            {
                                if (isGlitterSpray)
                                {
                                    tex.SetPixel(i, j, glit2Pixel);
                                    //finalColors[curSubCreativeIndex][i, j].a = 0.5f;
                                }
                                else
                                {
                                    tex.SetPixel(i, j, finalColors[curSubCreativeIndex][i, j]);
                                }

                                texColors[i, j] = finalColors[curSubCreativeIndex][i, j];
                                numPixelsColored++;

                                for (int b = 1; b <= x; b++)
                                {
                                    if (b % cellSize == 0) { cellX = b; }
                                }

                                for (int a = 1; a <= y; a++)
                                {
                                    if (a % cellSize == 0) { cellY = a; }
                                }

                                PrevCellPoint = new Vector2(cellX, cellY);
                                //print(cellX + " , " + cellY);

                                if (PrevCellPoint != lastCellPoint)
                                {
                                    //print("Grid Length : " + grid.Count);
                                    if(grid.Count >= 64)
                                    {
                                        int peelCellPixels = ReturnPreCalculatedCellPixels(cellX, cellY);
                                        //print(cellX + " , " + cellY + "pix -----> " + peelCellPixels);

                                        int reqPixToAutoFill = (int)(peelCellPixels * 0.60f);

                                        if (NumPixColoredInCell(cellX, cellY) >= reqPixToAutoFill)
                                        {
                                            SetGridPixels(cellX, cellY);
                                            lastCellPoint = PrevCellPoint;
                                        }
                                    }

                                }

                                if (numPixelsColored >= curCreative.subCreatives[curSubCreativeIndex].pixelCount)
                                {
                                    canSpray = false;

                                    for (int col = 0; col < texWidth; col++)
                                    {
                                        for (int row = 0; row < texHeight; row++)
                                        {
                                            peelPixel = curPeelTex.GetPixel(col, row);
                                            if (peelPixel.a < 0.01f)
                                            {
                                                if (!texColors[col, row].Compare(finalColors[curSubCreativeIndex][col, row]))
                                                {
                                                    tex.SetPixel(col, row, finalColors[curSubCreativeIndex][col, row]);
                                                    texColors[col, row] = finalColors[curSubCreativeIndex][col, row];
                                                    //Debug.Log("<color=red></color>" + numPixelsColored + "<color=green> / </color>" + curCreative.subCreatives[curSubCreativeIndex].pixelCount);
                                                }
                                            }
                                        }
                                    }
                                    tex.Apply();
                                    SubCreativeComplete();
                                    sr.gameObject.SetActive(false);
                                    LeanTween.cancel(gameObject);
                                    return;
                                }
                            }
                        }
                        else
                        {
                            if (isGlitterSpray)
                            {
                                curPeelTex.SetPixel(i, j, glit2Pixel);
                            }
                            else
                            {
                                curPeelTex.SetPixel(i, j, drawColor);
                            }
                        }
                    }
                }
            }
        }
        //LeanTween.delayedCall(sr.gameObject, 1, () => sr.gameObject.SetActive(false)); //OUC
    }

    [EasyButtons.Button]
    void DrawTextureOnGrid()
    {
        //SetGridPixels(Random.Range(0, 384), Random.Range(0, 384));
        StartCoroutine(DrawWholeTex());
    }

    IEnumerator DrawWholeTex()
    {
        int x = 0, y = 0;

        while (y != 336 && x != 336)
        {
            SetGridPixels(x, y);
            if (x == 336)
            {
                x = 0;
                y += 48;
            }
            x += 48;
            yield return new WaitForSeconds(1);
        }
    }

    void SetGridPixels(int pixelX, int pixelY)
    {
        for (int col = pixelY; col < pixelY + cellSize; col++)
        {
            for (int row = pixelX; row < pixelX + cellSize; row++)
            {
                Color glit2Pixel = currGlitTex.GetPixel(row, col);
                //Color peelPixel = curPeelTex.GetPixel(row, col);
                if (curPeelTex.GetPixel(row, col).a < 0.01f)
                {
                    if (!texColors[row, col].Compare(finalColors[curSubCreativeIndex][row, col]))
                    {
                        if (isGlitterSpray)
                        {
                            tex.SetPixel(row, col, glit2Pixel);
                        }
                        else
                        {
                            tex.SetPixel(row, col, finalColors[curSubCreativeIndex][row, col]);
                        }

                        texColors[row, col] = finalColors[curSubCreativeIndex][row, col];
                        numPixelsColored++;
                        //print("Color Filled");
                    }
                    //cellPixelCount++;
                    //print("Transparent as pixel is a part of cutout!");
                }
            }
        }
        tex.Apply();
    }

    [EasyButtons.Button]
    void CalulatePix()
    {
        //GetCellPixelsInPeelTexture(0, 0);
        Debug.Log(GetCellPixelsInDrawTexture(192, 192) + " / " + GetCellPixelsInPeelTexture(192, 192));
        //SetGridPixels(192, 48);
    }

    [EasyButtons.Button]
    void CalculateAllPixelsInSubcreative()
    {
        Debug.Log("No of pixels in the area is " + ReturnPreCalculatedCellPixels(192, 192));
    }

    //int totPixelsInGrid = 0;
    public List<int> grid;
    IEnumerator CalculateEveryGridPixels()
    {
        int x = 0, y = 0;
        grid = new List<int>();

        while (y != 384)
        {
            //print(x + " , " + y);
            grid.Add(GetCellPixelsInPeelTexture(x, y));
            if (x == 336)
            {
                x = 0;
                y += 48;
            }
            else
            {
                x += 48;
            }
            yield return null;
            //yield return new WaitForSeconds(0.005f);
        }
        canFill = true;
    }

    int ReturnPreCalculatedCellPixels(int x, int y)
    {
        int cell = (y / 6) + (x / 48);
        //print(cell + ": cell");
        return grid[cell];
    }

    int GetCellPixelsInPeelTexture(int x, int y) //Cutout Pixels
    {
        int peelPixels = 0;

        for (int j = y; j < y + cellSize; j++)
        {
            for (int i = x; i < x + cellSize; i++)
            {
                if (curPeelTex.GetPixel(i, j).a < 0.01f)
                {
                    peelPixels++;
                }
            }
        }
        //print("pix " + peelPixels);
        return peelPixels;
    }

    public List<int> cellsColored = new List<int>();
    int NumPixColoredInCell(int x, int y)
    {
        int cell = (y / 6) + (x / 48);
        cellsColored[cell]++;
        return cellsColored[cell];
    }

    bool isCalledOnce;
    int GetCellPixelsInDrawTexture(int x, int y) //Draw Tex pixels
    {
        int texPixels = 0;

        if (!isCalledOnce)
        {
            isCalledOnce = true;

            for (int j = y; j < y + cellSize; j++)
            {
                for (int i = x; i < x + cellSize; i++)
                {
                    if (tex.GetPixel(i, j).a > 0.1f)
                    {
                        texPixels++;
                    }
                }
            }
            isCalledOnce = false;
        }
        return texPixels;
    }

    //int texPixelsClean;
    void GetCellPixelsInDrawTextureClean(int x, int y) //Draw Tex pixels
    {
        int texPixels = 0;

        for (int j = y; j < y + cellSize; j++)
        {
            for (int i = x; i < x + cellSize; i++)
            {
                if (tex.GetPixel(i, j).a > 0.1f)
                {
                    texPixels++;
                }
            }
        }
        //texPixels = texPixelsClean;
    }

    int tempSortOrder = 0;
    void SubCreativeComplete()
    {
        isDrawing = false;
        canFill = false;

        srPooler.DisableAllPooledObjects();

        if (ps.isPlaying)
        {
            ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            canSprayImage.gameObject.SetActive(false);
        }
        if (curSubCreativeIndex < curCreative.subCreatives.Length - 1)
        {
            foreach (SpriteRenderer s in subCreativeSRs)
            {
                s.sortingOrder--;
            }

            if (isGlitterSpray)
            {
                SpriteRenderer glitSR = Instantiate(glitterSpritePrefab, mainSR.transform).GetComponent<SpriteRenderer>();
                glitSR.transform.localPosition = Vector3.zero;// * 0.01f;
                tempSortOrder--;
                //glitSR.sortingOrder = tempSortOrder;
                //foreach (SpriteRenderer s in subGlitterSRs)
                //{
                //    s.sortingOrder--;
                //}
                glitSR.material.SetTexture("_Mask", curCreative.subCreatives[curSubCreativeIndex].texture);
                glitSR.color = curCreative.subCreatives[curSubCreativeIndex].color;
                subCreativeSRs.Add(glitSR);
                glitSR.sprite = Sprite.Create(currGlitTex, new Rect(0, 0, texWidth, texHeight), new Vector2(0.5f, 0.5f), 75);

            }
            else
            {
                SpriteRenderer sr = Instantiate(subCreativeSrPrefab, mainSR.transform).GetComponent<SpriteRenderer>();
                subCreativeSRs.Add(sr);
                sr.sprite = Sprite.Create(curCreative.subCreatives[curSubCreativeIndex].texture, new Rect(0, 0, texWidth, texHeight), new Vector2(0.5f, 0.5f), 75);
            }
            //sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, sr.color.a / 3);

            SetTexAsTransparent();
            curCreative.subCreatives[curSubCreativeIndex].pagePeeler.StartAutoPeel(1f, 1.3f); //orignally (1,1)
            StartCoroutine(AutoPaste(1.25f, 1f)); //Orginally (1.25f, .7f)
            //StartCoroutine(AutoPaste(1.25f, .7f));
            paintBrush.gameObject.SetActive(false);
            ps.gameObject.SetActive(false);
            kompleteSprayCanUI.SetActive(true);
            kompleteSprayCanUI.transform.position = paintBrush.transform.position;
            LeanTween.delayedCall(0.4f, () => KompleteSubCreativeFunc());
            DisableGlitterOff();
        }
        else
        {
            //foreach (SpriteRenderer s in subCreativeSRs) //OUC
            //{
            //    Destroy(s.gameObject);
            //}

            //if (isGlitter2) //NUC
            //{
            //    foreach(SpriteRenderer gr in subGlitterSRs)
            //    {
            //        glitterSRInitSortOrder--;
            //        gr.sortingOrder = glitterSRInitSortOrder;
            //        print(gr.sortingOrder);
            //    }
            //}

            //mainSR.sprite = Sprite.Create(curCreative.finalTex,new Rect(0,0, texWidth, texHeight),new Vector2(0.5f,0.5f),75); //OUC
            //curCreative.subCreatives[curSubCreativeIndex].pagePeeler.StartAutoPeel(5, 5);
            DragToPeel dragger = curCreative.subCreatives[curSubCreativeIndex].pagePeeler.backTransform.GetComponent<DragToPeel>();
            dragger.isMoving = true;
            dragger.isActive = true;
            curCreative.subCreatives[curSubCreativeIndex].pagePeeler.PartialPeel();
            paintBrush.gameObject.SetActive(false);
            UI.Instance.progressionUI.Play("ProgUIScale");
            LevelManager.Instance.progStarExplode_FX[0].Play();
            patternCanvas.SetActive(false);

            //fin
            //paintBrush.gameObject.SetActive(false);
            //ps.gameObject.SetActive(false);
            //kompleteSprayCanUI.SetActive(true);
            //kompleteSprayCanUI.transform.position = paintBrush.transform.position;
            //LeanTween.delayedCall(0.4f, () => KompleteSubCreativeFunc());
            //UI.Instance.Fin();
            DisableGlitterOff();
        }
    }

    internal void ReversePeel()
    {
        curCreative.subCreatives[curCreative.subCreatives.Length - 1].pagePeeler.StartReversePeel();
    }
    internal void CompleteFinalPeel()
    {
        curCreative.subCreatives[curCreative.subCreatives.Length - 1].pagePeeler.StartAutoPeel(0, 1f);
        UI.Instance.Fin();
    }
    internal void CreateNextPreview()
    {
        GameObject pasteSprite = Instantiate<GameObject>(pasteSpritePrefab, previewHolder);
        pasteSprite.transform.GetChild(0).GetComponent<SpriteRenderer>().color = curCreative.subCreatives[curSubCreativeIndex + 1].color;
        pasteSprite.transform.localPosition = Vector3.zero;
        pasteSprite.transform.localScale = Vector3.one;
        SpriteRenderer sr = pasteSprite.GetComponent<SpriteRenderer>();
        sr.color = peelColor;
        sr.sprite = OutlineGenerator.CreateCutout(curCreative.subCreatives[curSubCreativeIndex + 1].texture);

    }
    public IEnumerator AutoPaste(float delay, float duration)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
            Transform preview = previewHolder.GetChild(0);
            preview.SetParent(null);
            LeanTween.scale(preview.gameObject, Vector3.one, duration).setEaseOutQuad();
            LeanTween.alpha(preview.GetChild(0).gameObject, 0, 2);
            LeanTween.move(preview.gameObject, Vector3.zero, duration).setEaseOutQuad().setOnComplete(() =>
            {
                Destroy(preview.gameObject);
            });
            yield return new WaitForSeconds(duration);
        }
        SetSubCreative();
        srPooler.DisableAllPooledObjects();
    }

    //public IEnumerator AutoPaste(float delay, float duration)
    //{
    //    yield return new WaitForSeconds(delay);
    //    GameObject pasteSprite = Instantiate<GameObject>(pasteSpritePrefab);
    //    PagePeeler pagePeeler = pasteSprite.GetComponentInChildren<PagePeeler>();
    //    pagePeeler.frontPageBG.sprite = pagePeeler.frontPage.sprite = OutlineGenerator.CreateCutout(curCreative.subCreatives[curSubCreativeIndex+1].texture);
    //    pagePeeler.StartAutoPaste(duration);
    //    Color.RGBToHSV(peelColor, out float H, out float S, out float V);
    //    V -= 0.2f;
    //    pagePeeler.backPage.color = Color.HSVToRGB(H, S, V);
    //    Color c = peelColor;
    //    c.a = 0.5f;
    //    pagePeeler.frontPage.color = c;
    //    yield return new WaitForSeconds(duration);
    //    SetSubCreative();
    //}
    bool IsColorTransparent(Color col)
    {
        return col.a < 1;
    }
    static float BlendSubpixel(float top, float bottom, float alphaTop, float alphaBottom)
    {
        return (top * alphaTop) + ((bottom - 1f) * (alphaBottom - alphaTop));
    }
    public static Color AlphaBlend(Color top, Color bottom)
    {
        return new Color(BlendSubpixel(top.r, bottom.r, top.a, bottom.a),
            BlendSubpixel(top.g, bottom.g, top.a, bottom.a),
            BlendSubpixel(top.b, bottom.b, top.a, bottom.a),
            top.a + bottom.a
            );
    }
    public static float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
    internal static bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        if (Input.touchCount == 0)
        {
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        }
        else
        {
            eventDataCurrentPosition.position = new Vector2(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y);
        }

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    void KompleteSubCreativeFunc()
    {
        AudioManager.Instance.PlayStarSFX();
        kompleteSprayImg.color = (curCreative.subCreatives[curSubCreativeIndex + 1].color);
        UI.Instance.progressionUI.Play("ProgUIScale");
        LevelManager.Instance.progStarExplode_FX[0].Play();
        LeanTween.move(kompleteSprayCanUI, kompSprayCanUIInitPos, 0.75f).setEaseInOutQuart().setOnComplete(() => SubCreativeCompletionEffectFunc());
    }

    void SubCreativeCompletionEffectFunc()
    {
        KompleteSprayCan_FX.Play();
        //UI.Instance.nextItemUI.GetComponent<Animator>().Play("nxtItemMove");
    }
}
