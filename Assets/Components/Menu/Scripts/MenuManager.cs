using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(MenuManager))]
public class MenuManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MenuManager menuManager = (MenuManager)target;
        
        if (GUILayout.Button("Red"))
        {
            menuManager.ConnectionColor(Color.red);
        }
        if (GUILayout.Button("Green"))
        {
            menuManager.ConnectionColor(Color.green);
        }
        for(int i = 0; i < menuManager.menus.Length; i++)
        {
            if (GUILayout.Button("Toggle " + menuManager.menus[i].name))
            {
                menuManager.ToggleMenu(i);
            }
        }
        if (GUILayout.Button("Toggle Hide Menu"))
        {
            menuManager.ToggleHideMenu();
        }
    }
}
#endif

public class MenuManager : MonoBehaviour
{
    public UnityEvent<bool> MenuState;
    public GameObject[] menus;
    public GameObject menuCanvas;
    public GameObject showMenuButton;
    
    // Transition settings
    public float transitionDuration = 0.3f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private Material _leftEnd;
    private Material _rightEnd;
    private DebugLogger[] _loggers;
    private int _open = -1;
    private MeshRenderer _meshRenderer;
    private Coroutine _transitionCoroutine;
    private CanvasGroup _canvasGroup;
    private bool _isHidden = false;
    private Vector3 _showButtonOriginalScale;
    
    private void Awake() 
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _leftEnd = _meshRenderer.materials[1];
        _rightEnd = _meshRenderer.materials[2];
        _loggers = FindObjectsOfType<DebugLogger>();
        
        foreach(GameObject menu in menus)
        {
            menu.SetActive(false);
        }
        
        if (menuCanvas != null)
        {
            _canvasGroup = menuCanvas.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = menuCanvas.AddComponent<CanvasGroup>();
            }
            menuCanvas.SetActive(true);
            _canvasGroup.alpha = 1f;
        }
        
        if (showMenuButton != null)
        {
            _showButtonOriginalScale = showMenuButton.transform.localScale;
            showMenuButton.SetActive(false);
        }
    }
    
    public void ConnectionColor(Color c)
    {
        _leftEnd.color = c;
        _rightEnd.color = c;
    }
    
    public void ToggleLoggers()
    {
        foreach(DebugLogger logger in _loggers)
        {
            logger.toggleDebug();
        }
    }
    
    public void OnRosStatus(bool connected)
    {
        ConnectionColor(connected ? Color.green : Color.red);
    }
    
    private void UpdateState()
    {
        for(int i = 0; i < menus.Length; i++)
        {
            menus[i].SetActive(i == _open);
        }
        
        MenuState.Invoke(_open != -1);
    }
    
    public void ToggleWifiMenu()
    {
        _open = _open == 0 ? -1 : 0;
        UpdateState();
    }
    
    public void ToggleSettingMenu()
    {
        _open = _open == 1 ? -1 : 1;
        UpdateState();
    }
    
    public void ToggleCameraMenu()
    {
        _open = _open == 2 ? -1 : 2;
        UpdateState();
    }
    
    public void ToggleLidarMenu()
    {
        _open = _open == 3 ? -1 : 3;
        UpdateState();
    }
    
    public void ToggleMenu(int i)
    {
        _open = _open == i ? -1 : i;
        UpdateState();
    }
    
    public void HideAllMenus()
    {
        if (_isHidden) return;
        
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }
        _transitionCoroutine = StartCoroutine(HideTransition());
    }
    
    public void ShowMenuCanvas()
    {
        if (!_isHidden) return;

        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }
        _transitionCoroutine = StartCoroutine(ShowTransition());
    }

    public void ToggleHideMenu()
    {
        if (_isHidden)
        {
            ShowMenuCanvas();
        }
        else
        {
            HideAllMenus();
        }
    }

    private IEnumerator HideTransition()
    {
        _open = -1;
        UpdateState();
        
        if (showMenuButton != null)
        {
            showMenuButton.SetActive(true);
        }
        
        float elapsed = 0f;
        float startAlpha = _canvasGroup != null ? _canvasGroup.alpha : 1f;
        
        Color startLeftColor = _leftEnd.color;
        Color startRightColor = _rightEnd.color;
        Color targetLeftColor = new Color(startLeftColor.r, startLeftColor.g, startLeftColor.b, 0);
        Color targetRightColor = new Color(startRightColor.r, startRightColor.g, startRightColor.b, 0);
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsed / transitionDuration);
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            }
            
            _leftEnd.color = Color.Lerp(startLeftColor, targetLeftColor, t);
            _rightEnd.color = Color.Lerp(startRightColor, targetRightColor, t);
            
            yield return null;
        }
        
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }
        _leftEnd.color = targetLeftColor;
        _rightEnd.color = targetRightColor;
        
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(false);
        }
        if (_meshRenderer != null)
        {
            _meshRenderer.enabled = false;
        }
        
        _isHidden = true;
        _transitionCoroutine = null;
    }

    private IEnumerator ShowTransition()
    {
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(true);
        }
        if (_meshRenderer != null)
        {
            _meshRenderer.enabled = true;
        }
        
        float elapsed = 0f;
        
        Color targetLeftColor = _leftEnd.color;
        Color targetRightColor = _rightEnd.color;
        targetLeftColor.a = 1f;
        targetRightColor.a = 1f;
        
        Color startLeftColor = new Color(targetLeftColor.r, targetLeftColor.g, targetLeftColor.b, 0);
        Color startRightColor = new Color(targetRightColor.r, targetRightColor.g, targetRightColor.b, 0);
        
        _leftEnd.color = startLeftColor;
        _rightEnd.color = startRightColor;
        
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsed / transitionDuration);
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            }
            
            _leftEnd.color = Color.Lerp(startLeftColor, targetLeftColor, t);
            _rightEnd.color = Color.Lerp(startRightColor, targetRightColor, t);
            
            yield return null;
        }
        
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }
        _leftEnd.color = targetLeftColor;
        _rightEnd.color = targetRightColor;
        
        if (showMenuButton != null)
        {
            showMenuButton.SetActive(false);
        }
        
        _isHidden = false;
        _transitionCoroutine = null;
    }
}