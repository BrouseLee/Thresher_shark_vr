using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class FloatCameraController : MonoBehaviour
{
    private Boolean cameraOn = false;

    [Header("Camera Components")]
    public GameObject floatCameraRig;
    public Camera virtualCamera;
    public RenderTexture renderTexture;

    [Header("Zoom Settings")]
    public float zoomspeed = 30f;
    public float minFOV = 20;
    public float maxFOV = 80;

    [Header("Input Actions")]
    public InputActionProperty toggleCameraAction;
    public InputActionProperty takePhotoAction;
    public InputActionProperty zoomAction;

    void OnToggleCamera(InputAction.CallbackContext ctx)
    {
        var album = UnityEngine.Object.FindFirstObjectByType<PhotoAlbum3D>();
        if (album != null && album.IsAlbumCurrentlyOpen())
        {
            UnityEngine.Object.FindFirstObjectByType<FloatingMessageManager>()?.ShowMessage("Shutdown album to turn on camera");
            return;
        }


        cameraOn = !cameraOn;
        floatCameraRig.SetActive(cameraOn);
    }

    void OnTakePhoto(InputAction.CallbackContext ctx)
    {
        if (!cameraOn) return;

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string path = Path.Combine(Application.persistentDataPath, $"photo_{timestamp}.png");
        string txtPath = Path.ChangeExtension(path, ".txt");


        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D image = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        image.Apply();

        byte[] bytes = image.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        RenderTexture.active = currentRT;
        Destroy(image);

        Debug.Log("Photo saved to: " + path);


        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(virtualCamera);
        var allDetectables = FindObjectsByType<DetectableObject>(FindObjectsSortMode.None);
        List<string> detectedInfo = new List<string>();

        foreach (var d in allDetectables)
        {
            Renderer r = d.Renderer;
            if (r != null && GeometryUtility.TestPlanesAABB(frustumPlanes, r.bounds))
            {
                string types = string.Join(", ", d.objectTypes);
                detectedInfo.Add($"- {d.name}£¨Types: {types}£©");
            }
        }

        if (detectedInfo.Count > 0)
            File.WriteAllText(txtPath, string.Join("\n", detectedInfo));
        else
            File.WriteAllText(txtPath, "(No detectable objects)");

        Debug.Log(" Description saved to: " + txtPath);
    }


    void OnEnable()
    {
        toggleCameraAction.action.performed += OnToggleCamera;
        takePhotoAction.action.performed += OnTakePhoto;
        toggleCameraAction.action.Enable();
        takePhotoAction.action.Enable();
        zoomAction.action.Enable();
    }

    private void OnDisable()
    {
        toggleCameraAction.action.performed -= OnToggleCamera;
        takePhotoAction.action.performed -= OnTakePhoto;
        toggleCameraAction.action.Disable();
        takePhotoAction.action.Disable();
        zoomAction.action.Disable();
    }

    private void Update()
    {
        if (!cameraOn) return;

        float zoomInput = zoomAction.action.ReadValue<float>();
        if (Mathf.Abs(zoomInput) > 0.01f)
        {
            virtualCamera.fieldOfView = Mathf.Clamp(
                virtualCamera.fieldOfView - zoomInput * zoomspeed * Time.deltaTime,
                minFOV, maxFOV
            );
        }
    }

    public bool IsCameraActive()
    {
        return cameraOn && floatCameraRig.activeSelf;
    }

}


public enum ObjectType
{
    Fish,
    Shark,
    Moon
}
