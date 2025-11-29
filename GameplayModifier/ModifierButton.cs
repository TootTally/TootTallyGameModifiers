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
    public bool onCooldown = false;

    private GameObject glow;
    private SecondDegreeDynamicsAnimation anim = new SecondDegreeDynamicsAnimation(2.5f, 1f, 2.5f);

    public ModifierButton(Transform transform, GameModifiers.Metadata modifier, bool active, Vector2 size, int bubbleBorder, int fontSize, bool useWorldPosition, Action onClick = null)
    {
        var name = modifier.ModifierType.ToString();
        var sprite = AssetManager.GetSprite($"{modifier.Name}.png");
        this.active = active;
        this.modifier = modifier;
        button = GameObjectFactory.CreateCustomButton(transform, Vector2.zero, size, sprite, name, onClick);
        var gameObject = button.gameObject;
        var bubble = GameObjectFactory.CreateBubble(Vector2.zero, name + "Bubble", modifier.Description, new Vector2(1f, 0f), bubbleBorder, true, fontSize);
        gameObject.AddComponent<BubblePopupHandler>().Initialize(transform, bubble, useWorldPosition);

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

    public void ToggleOn()
    {
        active = true;
        TootTallyAnimationManager.AddNewEulerAngleAnimation(button.gameObject, new Vector3(0, 0, 8), 0.15f, anim, sender => { onCooldown = false; });
        glow.SetActive(true);
    }

    public void ToggleOff()
    {
        active = false;
        TootTallyAnimationManager.AddNewEulerAngleAnimation(button.gameObject, Vector3.zero, 0.15f, anim, sender => { onCooldown = false; });
        glow.SetActive(false);
    }
}
