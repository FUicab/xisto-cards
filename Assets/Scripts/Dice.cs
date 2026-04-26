using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Dice : MonoBehaviour
{
    [HideInInspector] public int value;
    [HideInInspector] public bool used = false;
    [HideInInspector] public bool wild = false;
    [HideInInspector] public bool selected = false;
    [HideInInspector] public bool selectable = false;
    public Color regularColor = new Color(0.73f, 0.86f, 0.89f, 1.0f);
    public Color wildColor = new Color(1f, 0.96f, 0.73f, 1.0f);
    public Color usedColor = new Color(0.66f, 0.66f, 0.66f, 1.0f);
    public Color selectedColor = new Color(1f, 0.77f, 0.58f, 1.0f);
    public Color selectableColor = new Color(0.91f, 0.91f, 0.91f, 1.0f);
    public PlayerRole OwnerRole;
    public GameObject DiceDisplay;
    [HideInInspector] public Image DiceImage;
    [HideInInspector] public TextMeshProUGUI DiceText;
    [HideInInspector] public Outline DiceOutline;

    public void Awake()
    {
        DiceImage = GetComponent<Image>();
        DiceImage.color = regularColor;
        DiceText = GetComponentInChildren<TextMeshProUGUI>();
        DiceOutline = GetComponentInChildren<Outline>();
    }

    public void Roll()
    {
        value = Random.Range(1, 7);
        if(DiceText != null)
        {
            DiceText.text = value.ToString();
        } else {
            GetComponentInChildren<TextMeshProUGUI>().text = value.ToString();
        }
    }

    public void MakeWild()
    {
        wild = true;
        DiceImage.color = wildColor;
        DiceText.text = "⭐";
    }

    public void Use()
    {
        used = true;
        DiceImage.color = usedColor;
    }

    public void Reset()
    {
        Roll();
        UpdateDiceSelectionStatus(false, false);
        DiceImage.color = regularColor;
        used = false;
        wild = false;
    }

    public void UpdateDiceSelectionStatus(bool canBeSelected, bool wasSelected)
    {
        selected = wasSelected;
        selectable = canBeSelected || wasSelected;
        DiceOutline.enabled = wasSelected || canBeSelected;
        if (selected)
        {
            DiceOutline.effectColor = selectedColor;
        }
        if (selectable && !selected) {
            DiceOutline.effectColor = selectableColor;
        }
    }
}

//[System.Serializable]
//public class Dice
//{
//    public int value;
//    public bool used = false;
//    public bool wild = false;
//    public Color regularColor = new Color(0.73f, 0.86f, 0.89f, 1.0f);
//    public Color wildColor = new Color(0.89f, 0.89f, 0.72f, 1.0f);
//    public Color usedColor = new Color(0.66f, 0.66f, 0.66f, 1.0f);
//    public GameObject DiceDisplay;
//    public Image DiceImage;
//    public TextMeshProUGUI DiceText;
//    public Dice(GameObject diceDisplay)
//    {
//        DiceDisplay = diceDisplay;
//        DiceImage = DiceDisplay.GetComponent<Image>();
//        DiceText = DiceDisplay.GetComponentInChildren<TextMeshProUGUI>();
//    }

//    public void Roll()
//    {
//        value = Random.Range(1, 7);
//        DiceText.text = value.ToString();
//    }

//    public void MakeWild()
//    {
//        wild = true;
//        DiceImage.color = wildColor;
//    }

//    public void Use()
//    {
//        used = true;
//        DiceImage.color = usedColor;
//    }

//    public void Reset()
//    {
//        Roll();
//        DiceImage.color = regularColor;
//        used = false;
//        wild = false;
//    }
//}