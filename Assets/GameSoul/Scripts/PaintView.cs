//-----------------------------------------------------------------------
// <copyright file="PaintView.cs" company="Codingworks Game Development">
//     Copyright (c) codingworks. All rights reserved.
// </copyright>
// <author> codingworks </author>
// <email> coding2233@163.com </email>
// <time> 2017-12-10 </time>
//-----------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;


public class PaintView : MonoBehaviour
{
    #region 属性

    /// <summary>
    /// 笔刷shader
    /// </summary>
    [SerializeField]
    private Shader _paintBrushShader;
    /// <summary>
    /// 笔刷material
    /// </summary>
    private Material _paintBrushMat;

    /// <summary>
    /// 橡皮的shader
    /// </summary>
    [SerializeField]
    private Shader _clearBrushShader;

    /// <summary>
    /// 橡皮的material
    /// </summary>
    private Material _clearBrushMat;

    /// <summary>
    /// 当前选中的笔刷贴图
    /// </summary>
    [SerializeField]
    private RawImage _defaultBrushRawImage;

    /// <summary>
    /// 默认的笔刷贴图
    /// </summary>
    [SerializeField]
    private Texture _defaultBrushTex;

    /// <summary>
    /// 画布
    /// </summary>
    private RenderTexture _renderTex;

    /// <summary>
    /// 当前选中的笔刷颜色
    /// </summary>
    [SerializeField]
    private Image _defaultColorImage;

    /// <summary>
    /// 绘画的画布
    /// </summary>
    [SerializeField]
    private RawImage _paintCanvas;

    /// <summary>
    /// 默认的笔刷颜色
    /// </summary>
    [SerializeField]
    private Color _defaultColor;

    /// <summary>
    /// 笔刷大小的slider
    /// </summary>
    private Text _brushSizeText;
    /// <summary>
    /// 笔刷的大小
    /// </summary>
    private float _brushSize;
    /// <summary>
    /// 屏幕的宽高
    /// </summary>
    private int _screenWidth;
    /// <summary>
    /// 屏幕的宽高
    /// </summary>
    private int _screenHeight;
    /// <summary>
    /// 笔刷的间隔大小
    /// </summary>
    private float _brushLerpSize;
    /// <summary>
    /// 默认上一次点的位置
    /// </summary>
    private Vector2 _lastPoint;

    #endregion 属性

    void Start()
    {
        InitData();
    }

    private void Update()
    {
        Color clearColor = new Color(0, 0, 0, 0);
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _paintBrushMat.SetColor("_Color", clearColor);
        }
    }

    #region 外部接口

    /// <summary>
    /// 设置笔刷大小
    /// </summary>
    /// <param name="size"></param>
    public void SetBrushSize(float size)
    {
        _brushSize = size;
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
        _defaultBrushRawImage.texture = _defaultBrushTex;
    }

    /// <summary>
    /// 设置笔刷颜色
    /// </summary>
    /// <param name="color"></param>
    public void SetBrushColor(Color color)
    {
        _defaultColor = color;
        _paintBrushMat.SetColor("_Color", _defaultColor);
        _defaultColorImage.color = _defaultColor;
    }

    /// <summary>
    /// 选择颜色
    /// </summary>
    /// <param name="image"></param>
    public void SelectColor(Image image)
    {
        SetBrushColor(image.color);
    }

    /// <summary>
    /// 选择笔刷
    /// </summary>
    /// <param name="rawImage"></param>
    public void SelectBrush(RawImage rawImage)
    {
        SetBrushTexture(rawImage.texture);
    }

    /// <summary>
    /// 笔刷大小改变
    /// </summary>
    /// <param name="value"></param>
    public void BrushSizeChanged(Slider slider)
    {
        //float value = slider.maxValue + slider.minValue - slider.value;
        SetBrushSize(Remap(slider.value, 300.0f, 30.0f));
        if (_brushSizeText == null)
        {
            _brushSizeText = slider.transform.Find("Background/Text").GetComponent<Text>();
        }
        _brushSizeText.text = slider.value.ToString("f2");
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
                LerpPaint(Input.mousePosition);
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

    /// <summary>
    /// 初始化数据
    /// </summary>
    void InitData()
    {
        _brushSize = 300.0f;
        _brushLerpSize = (_defaultBrushTex.width + _defaultBrushTex.height) / 2.0f / _brushSize;
        _lastPoint = Vector2.zero;

        if (_paintBrushMat == null)
        {
            UpdateBrushMaterial();
        }
        if (_clearBrushMat == null)
        {
            _clearBrushMat = new Material(_clearBrushShader);
        }
        if (_renderTex == null)
        {
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;

            _renderTex = RenderTexture.GetTemporary(_screenWidth, _screenHeight, 24);
            _paintCanvas.texture = _renderTex;
        }
        Graphics.Blit(null, _renderTex, _clearBrushMat);
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
    /// <param name="point"></param>
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
    /// <param name="point"></param>
    private void Paint(Vector2 point)
    {
        if (point.x < 0 || point.x > _screenWidth || point.y < 0 || point.y > _screenHeight)
        {
            return;
        }
        Vector2 uv = new Vector2(point.x / (float)_screenWidth, point.y / (float)_screenHeight);
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

    #endregion
}
