using UnityEngine.UI;
using UnityEngine;
using TootTallyCore.Utils.Assets;

namespace TootTallyGameModifiers;

public static class GameModifierFactory
{
    public static CustomPopup CreateModifiersPopup(Transform buttonTransform, Vector2 buttonAnchoredPosition, Vector2 buttonSize, Transform popupTransform, Vector2 popupSize, int titleFontSize, Vector2 closeButtonSize)
    {
        return new CustomPopup("Modifiers", buttonTransform, buttonAnchoredPosition, buttonSize, AssetManager.GetSprite("ModifierButton.png"), popupTransform, popupSize, titleFontSize, closeButtonSize);
    }

    public static GameObject CreatePopupContainer(CustomPopup popup, Vector2 size)
    {
        var container = popup.popupBox.transform.GetChild(0).gameObject;
        var hContainer = GetHorizontalBox(size, container.transform);
        GameObject.DestroyImmediate(hContainer.GetComponent<HorizontalLayoutGroup>());
        var gridLayout = hContainer.AddComponent<GridLayoutGroup>();
        //gridLayout.padding = new RectOffset(padding, padding, padding, padding);
        //gridLayout.spacing = spacing;
        gridLayout.cellSize = Vector2.one * 32;
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        return hContainer;
    }

    public static GameObject GetVerticalBox(Vector2 size, Transform parent = null)
    {
        var box = GameObject.Instantiate(AssetBundleManager.GetPrefab("verticalbox"), parent);
        box.GetComponent<RectTransform>().sizeDelta = size;
        return box;
    }
    public static GameObject GetHorizontalBox(Vector2 size, Transform parent = null)
    {
        var box = GameObject.Instantiate(AssetBundleManager.GetPrefab("horizontalbox"), parent);
        box.GetComponent<RectTransform>().sizeDelta = size;
        return box;
    }

    public static GameObject GetBorderedVerticalBox(Vector2 size, int bordersize, Transform parent = null)
    {
        var box = GameObject.Instantiate(AssetBundleManager.GetPrefab("borderedverticalbox"), parent);
        box.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(bordersize, bordersize, bordersize, bordersize);
        box.GetComponent<RectTransform>().sizeDelta = size + (Vector2.one * 2f * bordersize);
        return box;
    }

    public static GameObject GetBorderedHorizontalBox(Vector2 size, int bordersize, Transform parent = null)
    {
        var box = GameObject.Instantiate(AssetBundleManager.GetPrefab("borderedhorizontalbox"), parent);
        box.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(bordersize, bordersize, bordersize, bordersize);
        box.GetComponent<RectTransform>().sizeDelta = size + (Vector2.one * 2f * bordersize);
        return box;
    }
}
