using System;
using TootTallyCore.Graphics.Animations;
using TootTallyCore.Graphics;
using TootTallyCore.Utils.Assets;
using TootTallyCore;
using UnityEngine;
using UnityEngine.UI;

namespace TootTallyGameModifiers;

public class ModifierButton
{
    public CustomButton button;
    public bool active;
    public GameModifiers.Metadata modifier;
    public bool canClickButtons = true;

    private GameObject glow;
    private SecondDegreeDynamicsAnimation anim = new SecondDegreeDynamicsAnimation(2.5f, 1f, 2.5f);

    public ModifierButton(Transform transform, GameModifiers.Metadata modifier, bool active, Vector2 size, int fontSize, bool useWorldPosition, Action onClick = null)
    {
        var name = modifier.ModifierType.ToString();
        var sprite = AssetManager.GetSprite($"{modifier.Name}.png");
        this.active = active;
        this.modifier = modifier;
        button = GameObjectFactory.CreateCustomButton(transform, Vector2.zero, size, sprite, name, onClick ?? Toggle);
        var gameObject = button.gameObject;
        var bubble = GameObjectFactory.CreateBubble(Vector2.zero, name + "Bubble", modifier.Description, 6, true, fontSize);
        gameObject.AddComponent<BubblePopupHandler>().Initialize(bubble, useWorldPosition);

        glow = new GameObject("glow", typeof(Image));
        Image component = glow.GetComponent<Image>();
        component.useSpriteMesh = true;
        component.color = Theme.colors.replayButton.text;
        component.maskable = true;
        component.sprite = AssetManager.GetSprite("glow.png");
        glow.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x * 1.5f, size.y * 1.5f);
        glow.transform.SetParent(gameObject.transform);
        glow.transform.localScale = Vector3.one / 1.2f;
        if (active) ToggleOn(); else ToggleOff();
        RectTransform component2 = gameObject.GetComponent<RectTransform>();
        component2.pivot = Vector2.one / 2f;
    }

    public void Toggle()
    {
        if (!canClickButtons) return;
        canClickButtons = false;
        active = !active;
        if (active) ToggleOn(); else ToggleOff();
    }

    public void ToggleOn()
    {
        TootTallyAnimationManager.AddNewEulerAngleAnimation(button.gameObject, new Vector3(0, 0, 8), 0.15f, anim, sender => { canClickButtons = true; });
        glow.SetActive(true);
    }

    public void ToggleOff()
    {
        TootTallyAnimationManager.AddNewEulerAngleAnimation(button.gameObject, Vector3.zero, 0.15f, anim, sender => { canClickButtons = true; });
        glow.SetActive(false);
    }
}
