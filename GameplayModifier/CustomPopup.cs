using TMPro;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Graphics;
using TootTallyCore.Utils.Assets;
using TootTallyCore;
using UnityEngine.UI;
using UnityEngine;

namespace TootTallyGameModifiers;

public class CustomPopup
{
    public CustomButton openPopupButton;
    public GameObject popupBox;
    public bool isPopupEnabled = false;
    private TootTallyAnimation anim;

    public CustomPopup(string title, Transform buttonTransform, Vector2 buttonAnchoredPosition, Vector2 buttonSize, Sprite buttonImage, Transform popupTransform, Vector2 popupSize, int titleFontSize, Vector2 closeButtonSize)
    {
        openPopupButton = GameObjectFactory.CreateCustomButton(buttonTransform, buttonAnchoredPosition, buttonSize, buttonImage,
            $"{title} Button", OnOpenButtonClick);
        popupBox = GameModifierFactory.GetBorderedVerticalBox(popupSize, 4, popupTransform);
        popupBox.transform.localScale = Vector2.zero;
        popupBox.name = title;

        var popupContainer = popupBox.transform.GetChild(0).gameObject;
        popupContainer.GetComponent<Image>().color = Theme.colors.leaderboard.text.CompareRGB(Color.black) ? new Color(1, 1, 1, 1) : new Color(0, 0, 0, 1);

        var boxRect = popupBox.GetComponent<RectTransform>();
        boxRect.anchorMin = boxRect.anchorMax = boxRect.pivot = Vector2.one / 2f;

        var titleText = GameObjectFactory.CreateSingleText(popupContainer.transform, $"{title} Title", title);
        titleText.fontSize = titleFontSize;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Bottom;
        titleText.rectTransform.sizeDelta = new Vector2(450, 30);

        GameObjectFactory.CreateCustomButton(popupContainer.transform, Vector2.one * -5f, closeButtonSize, AssetManager.GetSprite("Close64.png"), $"Close {title} Button", OnCloseAnimation)
            .gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
    }

    public void OnCloseAnimation()
    {
        isPopupEnabled = false;
        anim?.Dispose();
        anim = TootTallyAnimationManager.AddNewScaleAnimation(popupBox, Vector2.zero, .5f, new SecondDegreeDynamicsAnimation(3.5f, 1f, 1.1f), delegate
        {
            popupBox.gameObject.SetActive(false);
            anim = null;
        });
    }

    public void OnOpenAnimation()
    {
        isPopupEnabled = true;
        anim?.Dispose();
        popupBox.transform.localScale = Vector2.zero;
        popupBox.SetActive(true);
        anim = TootTallyAnimationManager.AddNewScaleAnimation(popupBox, Vector2.one, .6f, new SecondDegreeDynamicsAnimation(2.75f, 1f, 1.1f));
    }

    private void OnOpenButtonClick()
    {
        isPopupEnabled = !isPopupEnabled;
        if (isPopupEnabled)
            OnOpenAnimation();
        else
            OnCloseAnimation();
    }
}
