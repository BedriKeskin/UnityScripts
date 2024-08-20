using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Collections;
using System.Linq;

public class InputFieldManager : MonoBehaviour
{
    public List<TMP_InputField> inputFields;
    public bool activateFirstInputField = true, activateNextField = true;

    private TMP_InputField _inputField;
    private TouchScreenKeyboard _keyboard;
    private RectTransform _panelUnderSafeArea;
    private Vector3 _originalPosition;
    private float _inputFieldPositionY;

    private void OnEnable()
    {
        StartCoroutine(KeyboardHeightRoutine());
    }

    private void Start()
    {
        _panelUnderSafeArea = GetPanelUnderSafeArea();
        _originalPosition = _panelUnderSafeArea.position;
        
        TouchScreenKeyboard.hideInput = true;
        
        foreach (TMP_InputField inputField in inputFields)
        {
            inputField.onSelect.AddListener(delegate
            {
                _keyboard = OpenKeyboard(inputField);
                _inputField = inputField;
                _inputFieldPositionY = inputField.GetComponent<RectTransform>().position.y;
            });

            inputField.onValueChanged.AddListener(delegate { OnValueChanged(inputField); });
                
            if (activateNextField && inputField.characterLimit != 1) inputField.onEndEdit.AddListener(delegate
            {
                ActivateNextOrIfLastHideKeyboard(inputField);
            });
        }

        if (activateFirstInputField && !TouchScreenKeyboard.visible/* :p */) inputFields[0].ActivateInputField();
    }

    private static TouchScreenKeyboard OpenKeyboard(TMP_InputField inputField)
    {
        TMP_InputField.ContentType contentType = inputField.contentType;
        
        TouchScreenKeyboardType keyboardType = contentType switch
        {
            TMP_InputField.ContentType.Standard => TouchScreenKeyboardType.Default,
            TMP_InputField.ContentType.Autocorrected => TouchScreenKeyboardType.Default,
            TMP_InputField.ContentType.IntegerNumber => TouchScreenKeyboardType.NumberPad,
            TMP_InputField.ContentType.DecimalNumber => TouchScreenKeyboardType.DecimalPad,
            TMP_InputField.ContentType.Alphanumeric => TouchScreenKeyboardType.Default,
            TMP_InputField.ContentType.Name => TouchScreenKeyboardType.Default,
            TMP_InputField.ContentType.EmailAddress => TouchScreenKeyboardType.EmailAddress,
            TMP_InputField.ContentType.Password => TouchScreenKeyboardType.Default,
            TMP_InputField.ContentType.Pin => TouchScreenKeyboardType.OneTimeCode,
            TMP_InputField.ContentType.Custom => TouchScreenKeyboardType.NumberPad,
            _ => TouchScreenKeyboardType.Default
        };
        
        return TouchScreenKeyboard.Open(inputField.text, keyboardType, false, false, false, false, "");
    }

    private void OnValueChanged(TMP_InputField inputField)
    {
        if (inputField.characterLimit != 1) return;
        
        if (string.IsNullOrEmpty(inputField.text))//do NOT think to merge this if with if below
        {
            if (inputFields.IndexOf(inputField) != 0) inputFields[inputFields.IndexOf(inputField) - 1].ActivateInputField();
        }
        else
        {
            ActivateNextOrIfLastHideKeyboard(inputField);
        }
    }

    private void ActivateNextOrIfLastHideKeyboard(TMP_InputField inputField)
    {
        if (inputFields.IndexOf(inputField) != inputFields.Count - 1) inputFields[inputFields.IndexOf(inputField) + 1].ActivateInputField();
        else if (_keyboard != null) _keyboard.active = false;
    }

    private IEnumerator KeyboardHeightRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            if (_inputField && !_inputField.isFocused)
            {
                _keyboard.active = false;
                _panelUnderSafeArea.position = _originalPosition;
                continue;
            }
            
            if (TouchScreenKeyboard.visible && _panelUnderSafeArea.position == _originalPosition)
            {    
                float keyboardHeight = GetKeyboardHeight() ;
                float displacementValue = keyboardHeight - _inputFieldPositionY;
                
                if (displacementValue < 0) continue;
                _panelUnderSafeArea.position = new Vector3(_originalPosition.x, _originalPosition.y + keyboardHeight, _originalPosition.z);
            }
        }
    }
        
    private static float GetKeyboardHeight()
    {
#if UNITY_IOS
        return TouchScreenKeyboard.area.height * 1f;
#elif UNITY_ANDROID
            using (AndroidJavaObject unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject view = activity.Call<AndroidJavaObject>("getWindow").Call<AndroidJavaObject>("getDecorView");
                AndroidJavaObject rect = new AndroidJavaObject("android.graphics.Rect");
                view.Call("getWindowVisibleDisplayFrame", rect);
                int screenHeight = Screen.height;
                int visibleHeight = rect.Call<int>("height");
                return screenHeight - visibleHeight;
            }
#else
            return 0;
#endif
    }
        
    private RectTransform GetPanelUnderSafeArea()
    {
        Transform parent = transform;
        List<Transform> transforms = new List<Transform>();
    
        while(parent != GetComponentInParent<Canvas>().rootCanvas.transform)
        {
            transforms.Add(parent);
            parent = parent.parent;
        }

        return transforms[^2].GetComponent<RectTransform>();
    }
    
    public string GetCode()
    {
        return inputFields.Aggregate("", (current, inputField) => current + inputField.text);
    }
    
    public void ActivateFirstEmptyInputField()
    {
        foreach (TMP_InputField inputField in inputFields.Where(inputField => string.IsNullOrEmpty(inputField.text)))
        {
            inputField.ActivateInputField();
            break;
        }
    }
    
    private void OnDisable()
    {
        _panelUnderSafeArea.position = _originalPosition;
    }
}
