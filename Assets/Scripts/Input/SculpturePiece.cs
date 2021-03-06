﻿using System.Collections;
using ExitGames.Client.Photon;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class SculpturePiece : MonoBehaviour
{
    private LobbyDoorController _lobbyDoorController;
    
    private MeshRenderer _renderer;
    private CanvasGroup _canvasGroup;
    
    private MeshRenderer _glowRenderer;
    private Material _glowMaterial;
    private float _defaultIntensity, _interactingIntensity;
    private Vector2 _initialSize, _defaultSize, _interactingSize;

    [SerializeField]
    private Color _glowColor = Color.white;

    private static readonly int ScaleXProperty = Shader.PropertyToID("_ScaleX");
    private static readonly int ScaleYProperty = Shader.PropertyToID("_ScaleY");
    private static readonly int IntensityProperty = Shader.PropertyToID("_Intensity");

    /// <summary>
    /// Bool shared within the class to verify that the player only interacts with one piece at a time.
    /// </summary>
    private static bool _isAnyPieceInteractedWith;
    
    /// <summary>
    /// Bool to verify that the player interacts with this piece.
    /// Can only be true if _isAnyPieceInteractedWith was false before.
    /// </summary>
    private bool _isThisPieceInteractedWith;
    
    [SerializeField]
    private int _targetSceneIndex;
    
    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        _canvasGroup = GetComponentInChildren<CanvasGroup>();
        _canvasGroup.transform.parent = transform.parent;
        
        // [1] -> Ignore this object, [0] would be equal to GetComponent<MeshRenderer>()
        _glowRenderer = GetComponentsInChildren<MeshRenderer>()[1];
        _glowMaterial = _glowRenderer.material;
        _glowMaterial.color = _glowColor;
    }
    
    public void Setup(LobbyDoorController lobbyDoorController, float defaultIntensity, float interactingIntensity, float defaultScaleMultiplier, float interactingScaleMultiplier)
    {
        _lobbyDoorController = lobbyDoorController;
        
        _defaultIntensity = defaultIntensity;
        _interactingIntensity = interactingIntensity;
        _glowMaterial.SetFloat(IntensityProperty, _defaultIntensity);
        
        Vector3 meshSize = _renderer.bounds.size;
        _initialSize = new Vector2(meshSize.x, meshSize.y);
        _defaultSize = _initialSize * defaultScaleMultiplier;
        _interactingSize = _initialSize * interactingScaleMultiplier;
        _glowMaterial.SetFloat(ScaleXProperty, _defaultSize.x);
        _glowMaterial.SetFloat(ScaleYProperty, _defaultSize.y);
    }

    private void OnEnable()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        InputManager.Instance.OnMainButtonDown += OnTriggerDown;
#elif UNITY_ANDROID
        InputManager.Instance.CurrentlyUsedController.OnTriggerDown += OnTriggerDown;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        InputManager.Instance.OnMainButtonDown -= OnTriggerDown;
#elif UNITY_ANDROID
        InputManager.Instance.CurrentlyUsedController.OnTriggerDown -= OnTriggerDown;
#endif
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Only allow interaction if the player is not interacting with any other piece already
        if (_isAnyPieceInteractedWith)
        {
            return;
        }
        
        _isAnyPieceInteractedWith = true;
        _isThisPieceInteractedWith = true;

        StopAllCoroutines();
        StartCoroutine(C_FadeIntensity(_interactingIntensity));
        StartCoroutine(C_FadeScale(_interactingSize));

        _canvasGroup.transform.rotation = SceneReferences.PlayerCamera.transform.rotation;
        StartCoroutine(C_FadeRoomName(1));
    }

    private void OnTriggerExit(Collider other)
    {
        // Can only be true if this piece was the only one the player interacted with
        if (!_isThisPieceInteractedWith)
        {
            return;
        }
        
        _isAnyPieceInteractedWith = false;
        _isThisPieceInteractedWith = false;
        
        StopAllCoroutines();
        StartCoroutine(C_FadeIntensity(_defaultIntensity));
        StartCoroutine(C_FadeScale(_defaultSize));
        StartCoroutine(C_FadeRoomName(0));
    }

    /// <summary>
    /// Is called when the controller trigger is down. Do not confuse with unity event functions above.
    /// </summary>
    private void OnTriggerDown()
    {
        if (!_isThisPieceInteractedWith)
        {
            return;
        }
        
        // Not using the rotation of the camera on purpose
        Vector3 lookDirection = transform.position - SceneReferences.PlayerCamera.transform.position;
        Vector3 lookRotation = Quaternion.LookRotation(lookDirection).eulerAngles;
        _lobbyDoorController.OpenDoor(lookRotation.y);
        
        OnTriggerExit(null);
        
        SceneLoader.SetTargetScene(_targetSceneIndex);
    }

    private IEnumerator C_FadeIntensity(float target)
    {
        float start = _glowMaterial.GetFloat(IntensityProperty);
        float t = 0;
        while (t < 1)
        {
            _glowMaterial.SetFloat(IntensityProperty, Mathf.Lerp(start, target, t));
            
            t += Time.deltaTime;
            yield return null;
        }

        _glowMaterial.SetFloat(IntensityProperty, target);
    }
    
    private IEnumerator C_FadeScale(Vector2 target)
    {
        Vector2 start = new Vector2(_glowMaterial.GetFloat(ScaleXProperty), _glowMaterial.GetFloat(ScaleYProperty));

        float t = 0;
        while (t < 1)
        {
            Vector2 v = Vector2.Lerp(start, target, t);
            _glowMaterial.SetFloat(ScaleXProperty, v.x);
            _glowMaterial.SetFloat(ScaleYProperty, v.y);
            
            t += Time.deltaTime;
            yield return null;
        }
        
        _glowMaterial.SetFloat(ScaleXProperty, target.x);
        _glowMaterial.SetFloat(ScaleYProperty, target.y);
    }

    private IEnumerator C_FadeRoomName(float target)
    {
        float start = _canvasGroup.alpha;

        float t = 0;
        while (t < 1)
        {
            _canvasGroup.alpha = Mathf.Lerp(start, target, t);
            t += Time.deltaTime;
            yield return null;
        }

        _canvasGroup.alpha = target;
    }
}
