using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float animationSpeed = 10f;

    private RectTransform rectTransform;
    private Vector3 normalScale;
    private Vector3 targetScale;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        normalScale = rectTransform.localScale;
        targetScale = normalScale;
    }

    void Update()
    {
        // Smoothly move toward the target scale
        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            targetScale,
            animationSpeed * Time.deltaTime
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Mouse is hovering over button
        targetScale = normalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Mouse leaves button
        targetScale = normalScale;
    }
}
