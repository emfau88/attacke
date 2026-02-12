using UnityEngine;
using UnityEngine.UI;

public class FloatingText : MonoBehaviour
{
    private Text _text;
    private float _life;
    private Vector3 _velocity;

    public static void Spawn(Transform parent, Font font, string msg, Vector2 anchoredPos, Color color)
    {
        var go = new GameObject("FloatingText", typeof(RectTransform), typeof(Text), typeof(FloatingText));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(420, 60);
        rect.anchoredPosition = anchoredPos;

        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = 28;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.text = msg;

        var ft = go.GetComponent<FloatingText>();
        ft._text = t;
        ft._life = 1.0f;
        ft._velocity = new Vector3(0f, 45f, 0f);
    }

    private void Update()
    {
        _life -= Time.deltaTime;
        if (_life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        var rt = (RectTransform)transform;
        rt.anchoredPosition += new Vector2(_velocity.x, _velocity.y) * Time.deltaTime;
        var c = _text.color;
        c.a = Mathf.Clamp01(_life);
        _text.color = c;
    }
}
