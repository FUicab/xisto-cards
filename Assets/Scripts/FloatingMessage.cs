using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FloatingMessage : MonoBehaviour
{
    private Rigidbody2D rbody;
    private TextMeshProUGUI messageBox;
    private CanvasGroup canvasGroup;

    public float initialYVelocity = 3f;
    public float initialXVelocityRange = 0.5f;
    public float lifeSpan = 0.8f;

    void Awake(){
        rbody = GetComponent<Rigidbody2D>();
        canvasGroup = GetComponentInChildren<CanvasGroup>();
        messageBox = GetComponentInChildren<TextMeshProUGUI>();
    }

    void Start()
    {
        // rbody.velocity = new Vector2(Random.Range(-initialXVelocityRange,initialXVelocityRange),initialYVelocity);
        // rbody.velocity = new Vector2(0,initialYVelocity);
        Destroy(gameObject,lifeSpan);
    }

    void Update(){
        canvasGroup.alpha -= Time.deltaTime / lifeSpan;
    }

    public void SetMessage(string msg){
        messageBox.text = msg;
    }
}
