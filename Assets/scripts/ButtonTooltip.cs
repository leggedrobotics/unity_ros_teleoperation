using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;

public class ButtonTooltip : MonoBehaviour
{
    [SerializeField, TextArea] 
    private string tooltipText = "Tooltip text here";
    
    [SerializeField] 
    private GameObject tooltipPrefab;
    
    [SerializeField] 
    private Vector3 tooltipOffset = new Vector3(0, 0.05f, 0);
    
    [SerializeField] 
    private Color backgroundColor = new Color(0, 0, 0, 0.8f);
    
    [SerializeField] 
    private Color textColor = Color.white;
    
    [SerializeField] 
    private Vector2 tooltipSize = new Vector2(0.15f, 0.04f);
    
    private GameObject tooltipInstance;
    private TrackedDeviceGraphicRaycaster raycaster;
    
    private void Start()
    {
        // Try to hook into XR UI hover events
        var interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener(OnHoverEnter);
            interactable.hoverExited.AddListener(OnHoverExit);
        }
    }
    
    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        ShowTooltip();
    }
    
    private void OnHoverExit(HoverExitEventArgs args)
    {
        HideTooltip();
    }
    
    private void ShowTooltip()
    {
        if (tooltipInstance != null)
        {
            tooltipInstance.SetActive(true);
            return;
        }
        CreateTooltip();
    }
    
    private void HideTooltip()
    {
        if (tooltipInstance != null)
        {
            tooltipInstance.SetActive(false);
        }
    }
    
    private void CreateTooltip()
    {
        if (tooltipPrefab != null)
        {
            tooltipInstance = Instantiate(tooltipPrefab, transform.position + tooltipOffset, Quaternion.identity, transform);
            var textComponent = tooltipInstance.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = tooltipText;
            }
        }
        else
        {
            tooltipInstance = new GameObject("Tooltip");
            tooltipInstance.transform.SetParent(transform);
            tooltipInstance.transform.localPosition = tooltipOffset;
            tooltipInstance.transform.localRotation = Quaternion.identity;
            tooltipInstance.transform.localScale = Vector3.one;
            
            var tooltipCanvas = tooltipInstance.AddComponent<Canvas>();
            tooltipCanvas.renderMode = RenderMode.WorldSpace;
            
            var parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                tooltipCanvas.worldCamera = parentCanvas.worldCamera;
            }
            
            var canvasRect = tooltipCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = tooltipSize;
            canvasRect.localScale = Vector3.one;
            canvasRect.pivot = new Vector2(0.5f, 0f);
            
            tooltipInstance.AddComponent<GraphicRaycaster>();
            
            // Background panel
            var panel = new GameObject("Background");
            panel.transform.SetParent(canvasRect, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = backgroundColor;
            
            // Text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(panel.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(40, 20);
            textRect.offsetMax = new Vector2(-40, -20);
            
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = tooltipText;
            text.color = textColor;
            text.alignment = TextAlignmentOptions.Center;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            text.enableWordWrapping = true;
            
            text.enableAutoSizing = true;
            text.fontSizeMin = 12;
            text.fontSizeMax = 32;
        }
        
        tooltipInstance.SetActive(true);
    }
    
    private void OnDestroy()
    {
        if (tooltipInstance != null)
            Destroy(tooltipInstance);
    }

    [ContextMenu("Simulate Hover Enter")]
    private void SimulateHoverEnter()
    {
        ShowTooltip();
        Debug.Log("Simulated hover enter");
    }

    [ContextMenu("Simulate Hover Exit")]
    private void SimulateHoverExit()
    {
        HideTooltip();
        Debug.Log("Simulated hover exit");
    }
}