using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class PaintViewTest : MonoBehaviour
{
    #region 属性

    /// <summary>
    /// 使用的图
    /// </summary>
    [SerializeField]
    private RawImage _useImage;

    [SerializeField]
    private RectTransform _uiCanvas;

    /// <summary>
    /// 默认画布宽
    /// </summary>
    [SerializeField]
    private int _paintCanvasWidth = 300;

    /// <summary>
    /// 默认画布高
    /// </summary>
    [SerializeField]
    private int _paintCanvasHeight = 300;

    /// <summary>
    /// 笔刷shader
    /// </summary>
    [SerializeField]
    private Shader _paintBrushShader;

    /// <summary>
    /// 橡皮的shader
    /// </summary>
    [SerializeField]
    private Shader _clearBrushShader;

    /// <summary>
    /// 画布背景，用来显示区域
    /// </summary>
    [SerializeField]
    private RectTransform _paintCanvasBG;

    /// <summary>
    /// 绘画的画布
    /// </summary>
    [SerializeField]
    private RawImage _paintCanvas;

    /// <summary>
    /// 默认的笔刷贴图
    /// </summary>
    [SerializeField]
    private Texture _defaultBrushTex;

    /// <summary>
    /// 默认的笔刷颜色
    /// </summary>
    [SerializeField]
    private Color _defaultColor;

    /// <summary>
    /// 默认笔刷的大小
    /// </summary>
    [SerializeField]
    [Range(1, 100)]
    private int _defaultBrushSize = 1;

    /// <summary>
    /// 笔刷material
    /// </summary>
    private Material _paintBrushMat;

    /// <summary>
    /// 橡皮的material
    /// </summary>
    private Material _clearBrushMat;

    /// <summary>
    /// 画布
    /// </summary>
    private RenderTexture _renderTex;

    /// <summary>
    /// 笔刷的大小
    /// </summary>
    private float _brushSize;

    /// <summary>
    /// 笔刷的间隔大小
    /// </summary>
    private float _brushLerpSize;

    /// <summary>
    /// 默认上一次点的位置
    /// </summary>
    private Vector2 _lastPoint;

    private int _screenWidth;
    private int _screenHeight;

    #endregion 属性

    void Start()
    {
        _screenWidth = Screen.width;
        _screenHeight = Screen.height;

        InitData();
    }

    private void Update()
    {
        Color clearColor = new Color(0, 0, 0, 0);
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _paintBrushMat.SetColor("_Color", clearColor);
        }

        _useImage.transform.Rotate(new Vector3(0, 0, 10), Space.Self);
    }

    #region 外部接口

    /// <summary>
    /// 设置笔刷大小
    /// </summary>
    /// <param name="size">1-100</param>
    public void SetBrushSize(float size)
    {
        _brushSize = Remap(size, 150.0f, 15.0f);
        _brushLerpSize = (_defaultBrushTex.width + _defaultBrushTex.height) / 2.0f / _brushSize;
        _paintBrushMat.SetFloat("_Size", _brushSize);
    }

    /// <summary>
    /// 设置笔刷贴图
    /// </summary>
    /// <param name="texture"></param>
    public void SetBrushTexture(Texture texture)
    {
        _defaultBrushTex = texture;
        _paintBrushMat.SetTexture("_BrushTex", _defaultBrushTex);
    }

    /// <summary>
    /// 设置笔刷颜色
    /// </summary>
    /// <param name="color"></param>
    public void SetBrushColor(Color color)
    {
        _defaultColor = color;
        _paintBrushMat.SetColor("_Color", _defaultColor);
    }

    /// <summary>
    /// 拖拽
    /// </summary>
    public void DragUpdate()
    {
        if (_renderTex && _paintBrushMat)
        {
            if (Input.GetMouseButton(0))
            {
                LerpPaint(PosMouse2PainCanvas(Input.mousePosition));
            }
        }
    }

    /// <summary>
    /// 拖拽结束
    /// </summary>
    public void DragEnd()
    {
        if (Input.GetMouseButtonUp(0))
        {
            _lastPoint = Vector2.zero;
        }
    }

    #endregion

    #region 内部函数

    [ContextMenu("ResetAttr")]
    void ResetAttr()
    {
        SetBrushTexture(_defaultBrushTex);
        SetBrushColor(_defaultColor);
        SetBrushSize(_defaultBrushSize);
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    [ContextMenu("InitData")]
    void InitData()
    {
        _lastPoint = Vector2.zero;

        UpdateBrushMaterial();

        SetBrushSize(_defaultBrushSize);

        _clearBrushMat = new Material(_clearBrushShader);

        _renderTex = RenderTexture.GetTemporary(_paintCanvasWidth, _paintCanvasHeight, 24);
        _paintCanvas.texture = _renderTex;

        RectTransform rect = _paintCanvas.transform as RectTransform;
        rect.sizeDelta = new Vector2(_paintCanvasWidth, _paintCanvasHeight);
        _paintCanvasBG.sizeDelta = new Vector2(_paintCanvasWidth, _paintCanvasHeight);

        Graphics.Blit(null, _renderTex, _clearBrushMat);
    }

    /// <summary>
    /// 坐标转换：鼠标位置->相对画布位置
    /// </summary>
    /// <param name="mousePos"></param>
    /// <returns></returns>
    private Vector2 PosMouse2PainCanvas(Vector3 mousePos)
    {
        //暂时没有转换，可以先把画布的左下角转屏幕坐标，记录下来，再加减
        return new Vector2(mousePos.x, mousePos.y);
    }

    /// <summary>
    /// 更新笔刷材质
    /// </summary>
    private void UpdateBrushMaterial()
    {
        _paintBrushMat = new Material(_paintBrushShader);
        _paintBrushMat.SetTexture("_BrushTex", _defaultBrushTex);
        _paintBrushMat.SetColor("_Color", _defaultColor);
        _paintBrushMat.SetFloat("_Size", _brushSize);
    }

    /// <summary>
    /// 插点
    /// </summary>
    /// <param name="point">相对画布左下角的点</param>
    private void LerpPaint(Vector2 point)
    {
        Paint(point);

        if (_lastPoint == Vector2.zero)
        {
            _lastPoint = point;
            return;
        }

        float dis = Vector2.Distance(point, _lastPoint);
        if (dis > _brushLerpSize)
        {
            Vector2 dir = (point - _lastPoint).normalized;
            int num = (int)(dis / _brushLerpSize);
            for (int i = 0; i < num; i++)
            {
                Vector2 newPoint = _lastPoint + dir * (i + 1) * _brushLerpSize;
                Paint(newPoint);
            }
        }
        _lastPoint = point;
    }

    /// <summary>
    /// 画点
    /// </summary>
    /// <param name="point">相对画布左下角的点</param>
    private void Paint(Vector2 point)
    {
        if (point.x < 0 || point.x > _paintCanvasWidth || point.y < 0 || point.y > _paintCanvasHeight)
        {
            return;
        }
        Vector2 uv = new Vector2(point.x / (float)_paintCanvasWidth, point.y / (float)_paintCanvasHeight);
        _paintBrushMat.SetVector("_UV", uv);
        Graphics.Blit(_renderTex, _renderTex, _paintBrushMat);
    }

    /// <summary>
    /// 重映射，默认value为1-100
    /// </summary>
    /// <param name="value"></param>
    /// <param name="maxValue"></param>
    /// <param name="minValue"></param>
    /// <returns></returns>
    private float Remap(float value, float startValue, float enValue)
    {
        float returnValue = (value - 1.0f) / (100.0f - 1.0f);
        returnValue = (enValue - startValue) * returnValue + startValue;
        return returnValue;
    }

    [ContextMenu("SavePng")]
    private Texture2D SaveToPng()
    {
        string filename = "test.png";

        Texture2D tex2d = new Texture2D(_paintCanvasWidth, _paintCanvasWidth, TextureFormat.ARGB32, false);
        RenderTexture.active = _renderTex;
        tex2d.ReadPixels(new Rect(0, 0, _paintCanvasWidth, _paintCanvasWidth), 0, 0);
        tex2d.Apply();

        string filepath = Path.Combine(GetPersistentDataPath(), filename);
        byte[] pngShot = tex2d.EncodeToPNG();
        File.WriteAllBytes(filepath, pngShot);
        pngShot = null;

        return tex2d;
    }

    /// <summary>
    /// PersistentDataPath
    /// </summary>
    /// <returns></returns>
    static public string GetPersistentDataPath()
    {
        string filepath = Application.persistentDataPath;
        if (!Directory.Exists(filepath))
        {
            Directory.CreateDirectory(filepath);
        }
        return filepath;
    }

    [ContextMenu("LoadPng")]
    private void LoadPng()
    {
        string name = "test.png";
        this.StartCoroutine(DownloadEnumerator(name, _useImage));
    }

    private IEnumerator DownloadEnumerator(string name, RawImage image)
    {
        string path = Path.Combine(GetPersistentDataPath(), name);
        WWW www = new WWW(path);

        yield return www;

        if (www != null && string.IsNullOrEmpty(www.error))
        {
            Debug.LogError("X");
            image.texture = www.texture;
        }
        else
        {
            Debug.LogError("D " + www.error);
        }
    }

    private void LoadByIO()
    {
        string path = Path.Combine(GetPersistentDataPath(), "test.png");
        
        FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        fileStream.Seek(0, SeekOrigin.Begin);
        byte[] bytes = new byte[fileStream.Length];
        fileStream.Read(bytes, 0, (int)fileStream.Length);
        fileStream.Close();
        fileStream.Dispose();
        fileStream = null;
        
        int width = _paintCanvasWidth;
        int height = _paintCanvasHeight;
        Texture2D texture2D = new Texture2D(width, height);
        texture2D.LoadImage(bytes);

        _useImage.texture = texture2D;
    }

    private void OnGUI()
    {
        if (GUILayout.Button("LoadPng"))
        {
            //LoadPng();
            LoadByIO();
        }
    }

    #endregion
}
