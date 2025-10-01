using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class PaintManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField, Tooltip("Skinned Mesh Renderer vom Avatar/Player \n >>> Ch36 <<<")] private SkinnedMeshRenderer smr;
    [SerializeField, Tooltip("Rechter Index Finger Tip \n >>> R_IndexTip <<<")] private Transform rightIndexFingerTip;
    [SerializeField, Tooltip("Linker Index Finger Tip \n >>> L_IndexTip <<<")] private Transform leftIndexFingerTip;
    [SerializeField] private Texture2D referenceTexture;


    [Header("RenderTextures")]
    [SerializeField, Tooltip("Material welches von dem Pinsel auf die RenderTexture gezeichnet wird. \n >>> BrushShader <<<")]
    private Material brushMaterial;

    [SerializeField, Tooltip("Material des Charakters, welches die BaseTextur, previewRT und finalRT anzeigt. \n >>> CharacterShader <<<")]
    private Material paintTargetMaterial;
    private RenderTexture finalRT;
    private RenderTexture previewRT;
    private RenderTexture feedbackRT;
    [SerializeField] private Texture2D exportTex;


    [Header("Settings")]
    [SerializeField] private bool isPaintableAvatar = false;
    [SerializeField, Tooltip("Soll die linke Hand zum zeichnen verwendet werden?")] private bool useLeftHand = false;
    [SerializeField] private bool isEraserActive = false;
    [SerializeField] private bool useHardBrush = false;
    [SerializeField, Range(0.031f, 0.2f)] private float previewStartDistance = 0.2f;
    [SerializeField, Range(0.01f, 0.05f)] private float paintDistance = 0.05f;


    [Header("Brush Control")]
    [SerializeField] private float brushSize = 0.01f;
    [SerializeField] private Color brushColor = new Color(32f / 255f, 150f / 255f, 243f / 255f, 1f);


    private Mesh bakedMesh;
    private MeshCollider tempCollider;
    private Material combineMaterial;
    private bool canPaint = true;


    void Start()
    {
        paintTargetMaterial = Instantiate(smr.sharedMaterial);
        smr.material = paintTargetMaterial;

        const int res = 2048;
        finalRT = new RenderTexture(res, res, 0, RenderTextureFormat.ARGB32);
        previewRT = new RenderTexture(res, res, 0, RenderTextureFormat.ARGB32);
        feedbackRT = new RenderTexture(res, res, 0, RenderTextureFormat.ARGB32);
        finalRT.Create(); previewRT.Create(); feedbackRT.Create();

        paintTargetMaterial.SetTexture("_FinalTex", finalRT);
        paintTargetMaterial.SetTexture("_PreviewTex", previewRT);
        paintTargetMaterial.SetTexture("_FeedbackTex", feedbackRT);


        combineMaterial = new Material(Shader.Find("Hidden/BlitAdd"));

        bakedMesh = new Mesh();
        tempCollider = new GameObject("TempCollider").AddComponent<MeshCollider>();
        tempCollider.transform.SetParent(transform, false);
        int avatarLayer = LayerMask.NameToLayer("AvatarPaint");
        if (avatarLayer == -1) avatarLayer = 0;
        tempCollider.gameObject.layer = avatarLayer;
        if (IsOwner) tempCollider.gameObject.tag = "LocalAvatar";
        BakeCurrentMesh();

        StartCoroutine(ReBakeRoutine());
    }

    public void setIndexFingerTip(Transform rightFingerTip, Transform leftFingerTip)
    {
        rightIndexFingerTip = rightFingerTip;
        leftIndexFingerTip = leftFingerTip;
    }


    private void Update()
    {
        UpdatePainting();
    }


    private RenderTexture createRenderTexture()
    {
        RenderTexture rt = new RenderTexture(2048, 2048, 0, RenderTextureFormat.ARGB32);
        rt.useMipMap = false;
        rt.autoGenerateMips = false;
        rt.Create();
        return rt;
    }


    private void BakeCurrentMesh()
    {
        if (bakedMesh != null) Destroy(bakedMesh);

        bakedMesh = new Mesh();
        smr.BakeMesh(bakedMesh, true);

        if (tempCollider == null)
        {
            GameObject temp = new GameObject("TempCollider");
            temp.transform.position = smr.transform.position;
            temp.transform.rotation = smr.transform.rotation;
            temp.transform.localScale = Vector3.one;
            temp.layer = LayerMask.NameToLayer("Ignore Raycast");
            tempCollider = temp.AddComponent<MeshCollider>();
            tempCollider.convex = false;

        }

        tempCollider.sharedMesh = bakedMesh;
    }


    void UpdatePainting()
    {
        if (!IsOwner || isPaintableAvatar || !canPaint) return;

        ClearPreviewRT();

        Transform hand = useLeftHand ? leftIndexFingerTip : rightIndexFingerTip;
        Ray ray = new Ray(hand.position, hand.forward);

        int avatarLayer = LayerMask.NameToLayer("AvatarPaint");
        int mask = 1 << avatarLayer;
        if (!Physics.Raycast(ray, out RaycastHit hit, previewStartDistance, mask, QueryTriggerInteraction.Ignore)) return;

        AvatarPaintable target = hit.collider.GetComponentInParent<AvatarPaintable>();
        if (target == null) return;
        if (target.IsOwner) return;

        Vector2 uv = GetUVFromHit(bakedMesh, hit);
        float dist = hit.distance;
        float t = Mathf.InverseLerp(previewStartDistance, paintDistance, dist);
        float radius = Mathf.Lerp(brushSize, 0f, 1f - t);

        if (dist > paintDistance || isEraserActive)
        {
            PaintManager targetPM = target.GetComponent<PaintManager>();
            targetPM.ClearPreviewRT();
            targetPM.DrawPreview(uv, radius, brushColor, useHardBrush, isEraserActive);
        }

        if (dist <= paintDistance)
        {
            var stroke = new PaintStroke
            {
                uv = uv,
                radius = radius,
                color = brushColor,
                hard = useHardBrush,
                isErase = isEraserActive
            };
            target.SubmitStrokeServerRpc(stroke);
        }
    }


    private Vector2 GetUVFromHit(Mesh mesh, RaycastHit hit)
    {
        int triIndex = hit.triangleIndex;
        Vector3 bary = hit.barycentricCoordinate;

        int[] triangles = mesh.triangles;
        Vector2[] uvs = mesh.uv;

        int i0 = triangles[triIndex * 3 + 0];
        int i1 = triangles[triIndex * 3 + 1];
        int i2 = triangles[triIndex * 3 + 2];

        Vector2 uv0 = uvs[i0];
        Vector2 uv1 = uvs[i1];
        Vector2 uv2 = uvs[i2];

        // Debug.Log("UV hit at: "+(uv0 * bary.x + uv1 * bary.y + uv2 * bary.z));

        return uv0 * bary.x + uv1 * bary.y + uv2 * bary.z;
    }


    public void SaveFinalTextureAsPNG()
    {
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = finalRT;

        Texture2D tex = new Texture2D(finalRT.width, finalRT.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, finalRT.width, finalRT.height), 0, 0);
        tex.Apply();

        RenderTexture.active = currentRT;

        //byte[] bytes = tex.EncodeToPNG();
        //Destroy(tex);

        string path = Application.persistentDataPath + "/" + DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss") + ".png";
        //System.IO.File.WriteAllBytes(path, bytes);
        //Debug.Log("Texture saved to: " + path);

        Texture2D blended = CombineTextures(tex, referenceTexture, true);
        blended = CombineTextures(blended, exportTex, false);

        byte[] pngData = blended.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, pngData);
    }


    private Texture2D CombineTextures(Texture2D upperTex, Texture2D lowerTex, bool isFirstParamFinalRT)
    {
        var topData = upperTex.GetPixels();
        var botData = lowerTex.GetPixels();

        int count = botData.Length;
        var rData = new Color[count];

        for (int i = 0; i < count; i++)
        {
            if (isFirstParamFinalRT)
                if (topData[i].a > 0)
                {
                    topData[i].a = 0.5f;
                    topData[i].r = 1;
                    topData[i].g = 1;
                    topData[i].b = 1;
                }

            Color botCol = botData[i];
            Color topCol = topData[i];
            float srcF = topCol.a;
            float destF = 1f - topCol.a;
            float alpha = srcF + destF * botCol.a;
            Color R = (topCol * srcF + botCol * botCol.a * destF) / alpha;
            R.a = alpha;
            rData[i] = R;
        }

        var res = new Texture2D(finalRT.width, finalRT.height);
        res.SetPixels(rData);
        res.Apply();
        return res;
    }


    public PaintStats CompareWithReference()
    {
        RenderTexture.active = finalRT;
        Texture2D paintedTex = new Texture2D(finalRT.width, finalRT.height, TextureFormat.RGBA32, false);
        paintedTex.ReadPixels(new Rect(0, 0, finalRT.width, finalRT.height), 0, 0);
        paintedTex.Apply();
        RenderTexture.active = null;

        Color32[] paintPixels = paintedTex.GetPixels32();
        Color32[] refPixels = referenceTexture.GetPixels32();

        PaintStats stats = new PaintStats();

        int totalPixels = refPixels.Length;

        for (int i = 0; i < totalPixels; i++)
        {
            bool isInRefMask = refPixels[i].a > 0.1f;
            if (isInRefMask) stats.referenceMaskPixelCount++;

            bool painted = paintPixels[i].a > 0.1f;

            if (painted)
            {
                stats.totalPaintedPixels++;
                if (isInRefMask)
                    stats.correctPaintedPixels++;
                else
                    stats.overpaintedPixels++;
            }
        }

        return stats;
    }


    /// TODO remove little previewCircle if ray is not touching another avatar
    public void DrawPreview(Vector2 uv, float radius, Color col, bool hard, bool isEraserActive)
    {
        Color previewCol = isEraserActive
            ? Color.white
            : col;
        brushMaterial.SetFloat("_UseHardBrush", hard ? 1 : 0);
        brushMaterial.SetColor("_BrushColor", previewCol);
        brushMaterial.SetFloat("_BrushSize", radius);
        brushMaterial.SetVector("_BrushUV", new Vector4(uv.x, uv.y, 0, 0));

        Graphics.Blit(null, previewRT, brushMaterial, 0);
    }


    public void DrawStroke(Vector2 uv, float radius, Color col, bool hard, bool isErase)
    {
        Color brushColToUse = isErase
            ? Color.white
            : col;

        brushMaterial.SetFloat("_UseHardBrush", hard ? 1.0f : 0.0f);
        brushMaterial.SetColor("_BrushColor", brushColToUse);
        brushMaterial.SetFloat("_BrushSize", radius);
        brushMaterial.SetVector("_BrushUV", new Vector4(uv.x, uv.y, 0, 0));
        combineMaterial.SetFloat("_EraseMode", isErase ? 1f : 0f);

        RenderTexture backupRT = RenderTexture.GetTemporary(finalRT.width, finalRT.height, 0, finalRT.format);
        Graphics.Blit(finalRT, backupRT);

        RenderTexture brushRT = RenderTexture.GetTemporary(finalRT.width, finalRT.height, 0, finalRT.format);
        Graphics.SetRenderTarget(brushRT);
        GL.Clear(true, true, Color.clear);
        Graphics.SetRenderTarget(null);
        Graphics.Blit(null, brushRT, brushMaterial, 0);

        combineMaterial.SetTexture("_BaseTex", backupRT);
        combineMaterial.SetTexture("_BrushTex", brushRT);
        Graphics.Blit(null, finalRT, combineMaterial);

        RenderTexture.ReleaseTemporary(backupRT);
        RenderTexture.ReleaseTemporary(brushRT);
    }


    public void ClearPreviewRT()
    {
        Graphics.SetRenderTarget(previewRT);
        GL.Clear(true, true, Color.clear);
        Graphics.SetRenderTarget(null);
    }


    public void ClearFinalRT()
    {
        Graphics.SetRenderTarget(finalRT);
        GL.Clear(true, true, Color.clear);
        Graphics.SetRenderTarget(null);
    }

    [ClientRpc]
    public void ClearPaintClientRpc(ClientRpcParams rpcParams = default)
    {
        ClearFinalRT();
    }


    public IEnumerator ShowFeedbackFlash(int muscleIndex, float time)
    {
        canPaint = false;

        var data = GameManager.Instance.getMuscleDB().Muscles[muscleIndex];
        Graphics.Blit(data.ReferenceTexture, feedbackRT);

        paintTargetMaterial.SetFloat("_ShowFeedback", 1f);
        yield return new WaitForSeconds(time);
        paintTargetMaterial.SetFloat("_ShowFeedback", 0f);

        canPaint = true;
    }


    [Rpc(SendTo.Everyone)]
    public void ShowFeedBackFlashRpc(int muscleIndex, float time)
    {
        StartCoroutine(ShowFeedbackFlash(muscleIndex, time));
    }



    // ? was .2s but Quest 2 is not strong enough
    IEnumerator ReBakeRoutine()
    {
        WaitForSeconds w = new WaitForSeconds(0.5f);
        while (true)
        {
            BakeCurrentMesh();
            yield return w;
        }
    }


    public void SetReferenceTexture(Texture2D refTex)
    {
        referenceTexture = refTex;
        Debug.Log("<b><color=#a2ff00>[PaintManager]</color></b> New Reference Texture loaded: " + refTex.name);
    }


    public void setBrushSize(float bs)
    {
        Debug.Log("<b><color=#a2ff00>[PaintManager]</color></b> BrushSize changed to: " + bs);
        this.brushSize = bs;
    }

    public void setBrushBlue()
    {
        brushColor = new Color(32f / 255f, 150f / 255f, 243f / 255f, 1f);
        Debug.Log("<b><color=#a2ff00>[PaintManager]</color></b> BrushColor is now blue");
    }

    public void setBrushRed()
    {
        brushColor = new Color(243f / 255f, 36f / 255f, 32f / 255f, 1f);
        Debug.Log("<b><color=#a2ff00>[PaintManager]</color></b> BrushColor is now red");
    }

    public void setBrushYellow()
    {
        brushColor = new Color(243f / 255f, 217f / 255f, 32f / 255f, 1f);
        Debug.Log("<b><color=#a2ff00>[PaintManager]</color></b> BrushColor is now yellow");
    }

    public void toggleLeftHand()
    {
        useLeftHand = !useLeftHand;
        Debug.Log("<b><color=#a2ff00>[PaintManager]</color></b> Use Left Hand: " + useLeftHand);
    }

    public void toggleEraser()
    {
        isEraserActive = !isEraserActive;
        Debug.Log("<b><color=#a2ff00>[PaintManager]</color></b> Eraser active: " + isEraserActive);
    }

}