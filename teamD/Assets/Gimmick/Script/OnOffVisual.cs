using UnityEngine;

/// <summary>State indicators and feedback only; OnOffFloor owns solidity.</summary>
[DisallowMultipleComponent]
public sealed class OnOffVisual : MonoBehaviour
{
    [SerializeField] private Transform _button;
    [SerializeField] private Renderer _face;
    [SerializeField] private GameObject _onSymbol;
    [SerializeField] private GameObject _offSymbol;
    [SerializeField] private GimmickPulse _pulse;
    [SerializeField] private Color _onColor = new Color(.95f, .44f, .16f);
    [SerializeField] private Color _offColor = new Color(.12f, .55f, .84f);
    [SerializeField] private bool _floorIsOn;

    private MaterialPropertyBlock _properties;
    private Vector3 _rest;
    private bool _hasRest, _initialized, _lastState;
    private float _pressTime = -1;

    public void ShowSwitch(bool isOn)
    {
        Color color = isOn ? _onColor : _offColor;
        if (_face != null)
        {
            _properties ??= new MaterialPropertyBlock();
            _face.GetPropertyBlock(_properties);
            _properties.SetColor("_BaseColor", color);
            _properties.SetColor("_EmissionColor", color * .16f);
            _face.SetPropertyBlock(_properties);
        }
        if (_onSymbol != null) _onSymbol.SetActive(isOn);
        if (_offSymbol != null) _offSymbol.SetActive(!isOn);
        Feedback(isOn, color, true);
    }

    public void ShowFloor(bool solid, bool animate)
    {
        Feedback(solid, _floorIsOn ? _onColor : _offColor, animate);
    }

    private void Feedback(bool state, Color color, bool animate)
    {
        if (_initialized && state != _lastState && animate && Application.isPlaying)
            _pulse?.Play(color);
        _initialized = true;
        _lastState = state;
    }

    // Both ON and OFF are toggled by a press, never by remaining on the button.
    public void Press()
    {
        if (_button == null) return;
        if (!_hasRest) { _rest = _button.localPosition; _hasRest = true; }
        _pressTime = 0;
    }

    private void Update()
    {
        if (_pressTime < 0 || _button == null) return;
        _pressTime += Time.deltaTime;
        float t = Mathf.Clamp01(_pressTime / .22f);
        _button.localPosition = _rest + Vector3.down * (.075f * Mathf.Sin(t * Mathf.PI));
        if (t >= 1) { _button.localPosition = _rest; _pressTime = -1; }
    }

    private void OnDisable()
    {
        if (_hasRest && _button != null) _button.localPosition = _rest;
        _pressTime = -1;
        _initialized = false;
    }
}
