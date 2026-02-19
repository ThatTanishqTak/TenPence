using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodUIRow : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text ageText;
    [SerializeField] private Slider progressSlider;

    [Header("Marker Line (|)")]
    [SerializeField] private RectTransform markerParent; // optional, else uses slider RectTransform
    [SerializeField] private float markerWidth = 4f;
    [SerializeField] private float markerHeightMultiplier = 1.2f;

    private RectTransform _sliderRT;
    private RectTransform _markerRT;

    public void Setup(Sprite icon, string foodName)
    {
        if (iconImage != null) iconImage.sprite = icon;
        if (nameText != null) nameText.text = foodName;

        if (progressSlider != null)
        {
            progressSlider.interactable = false; // player can't drag it
            _sliderRT = progressSlider.GetComponent<RectTransform>();
        }

        if (markerParent == null && progressSlider != null)
        {
            // Prefer Fill Area if it exists
            var fa = progressSlider.transform.Find("Fill Area") as RectTransform;
            markerParent = fa != null ? fa : _sliderRT;
        }

        CreateMarkerIfNeeded();
    }

    public void SetProgress(int ageYears, int rawToAgedYears, int agedToSpoiledYears)
    {
        if (progressSlider == null) return;

        int totalYears = Mathf.Max(1, Mathf.Max(0, rawToAgedYears) + Mathf.Max(0, agedToSpoiledYears));
        int clampedAge = Mathf.Clamp(ageYears, 0, totalYears);

        progressSlider.minValue = 0;
        progressSlider.maxValue = totalYears;
        progressSlider.value = clampedAge;

        if (ageText != null)
            ageText.text = clampedAge.ToString();

        UpdateMarker(rawToAgedYears, totalYears);
    }

    private void CreateMarkerIfNeeded()
    {
        if (_markerRT != null || markerParent == null) return;

        var go = new GameObject("RawToAgedMarker", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(markerParent, false);

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.color = Color.white;

        _markerRT = img.rectTransform;
    }

    private void UpdateMarker(int rawToAgedYears, int totalYears)
    {
        if (_markerRT == null || markerParent == null) return;

        float t = Mathf.Clamp01(rawToAgedYears / (float)totalYears);

        _markerRT.anchorMin = new Vector2(t, 0.5f);
        _markerRT.anchorMax = new Vector2(t, 0.5f);
        _markerRT.pivot = new Vector2(0.5f, 0.5f);
        _markerRT.anchoredPosition = Vector2.zero;

        float h = (_sliderRT != null ? _sliderRT.rect.height : 20f) * markerHeightMultiplier;
        _markerRT.sizeDelta = new Vector2(markerWidth, h);
    }
}
