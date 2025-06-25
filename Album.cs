using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PhotoAlbum3D : MonoBehaviour
{
    [Header("Album Components")]
    public GameObject albumRoot;                  
    public MeshRenderer photoQuadRenderer;           
    public TextMeshPro descriptionText3D;          

    [Header("Input Actions")]
    public InputActionProperty toggleAlbumAction;  
    public InputActionProperty joystickAction;     
    public InputActionProperty deletePhotoAction;

    private List<string> photoPaths = new List<string>();
    private int currentIndex = 0;
    private bool isAlbumOpen = false;
    private float lastInputTime = -999f;
    private float inputCooldown = 0.5f;
    private Texture2D currentTexture;


    void Start()
    {
        RefreshAlbumList();
    }

    void OnEnable()
    {
        toggleAlbumAction.action.performed += ctx => ToggleAlbum();
        deletePhotoAction.action.performed += ctx => DeleteCurrentPhoto();

        toggleAlbumAction.action.Enable();
        joystickAction.action.Enable();
        deletePhotoAction.action.Enable();
    }

    void OnDisable()
    {
        toggleAlbumAction.action.performed -= ctx => ToggleAlbum();
        deletePhotoAction.action.performed -= ctx => DeleteCurrentPhoto();

        toggleAlbumAction.action.Disable();
        joystickAction.action.Disable();
        deletePhotoAction.action.Disable();
    }

    void ToggleAlbum()
    {
   
        var cameraCtrl = Object.FindFirstObjectByType<FloatCameraController>();
        if (cameraCtrl != null && cameraCtrl.IsCameraActive())
        {
            Object.FindFirstObjectByType<FloatingMessageManager>()?.ShowMessage("Shutdown camera to turn on album");
            return;
        }

        isAlbumOpen = !isAlbumOpen;
        albumRoot.SetActive(isAlbumOpen);
        RefreshAlbumList();

        if (cameraCtrl != null)
            cameraCtrl.enabled = !isAlbumOpen;
    }


    void Update()
    {

        if (!isAlbumOpen || photoPaths.Count == 0) return;

        Vector2 input = joystickAction.action.ReadValue<Vector2>();
        if (Time.time - lastInputTime < inputCooldown) return;

        if (input.x >= 0.5f)
        {
            ShowPhoto(currentIndex + 1);
            lastInputTime = Time.time;
        }
        else if (input.x <= -0.5f)
        {
            ShowPhoto(currentIndex - 1);
            lastInputTime = Time.time;
        }


        if (descriptionText3D != null && Camera.main != null)
        {
            descriptionText3D.transform.LookAt(Camera.main.transform);
            descriptionText3D.transform.rotation = Quaternion.Euler(0, descriptionText3D.transform.rotation.eulerAngles.y + 180f, 0);
        }
    }

    void ShowPhoto(int index)
    {
        if (photoPaths.Count == 0) return;

        index = (index + photoPaths.Count) % photoPaths.Count; 
        string path = photoPaths[index];

        if (!File.Exists(path)) return;

        byte[] data = File.ReadAllBytes(path);

        if (currentTexture != null)
            Destroy(currentTexture);

        currentTexture = new Texture2D(2, 2);
        currentTexture.LoadImage(data);
        photoQuadRenderer.material.mainTexture = currentTexture;

        string txtPath = Path.ChangeExtension(path, ".txt");
        descriptionText3D.text = File.Exists(txtPath) ? File.ReadAllText(txtPath) : "(No description)";

        currentIndex = index;
    }

    public void RefreshAlbumList()
    {
        photoPaths.Clear();
        string[] files = Directory.GetFiles(Application.persistentDataPath, "photo_*.png");
        photoPaths.AddRange(files);
        photoPaths.Sort();

        if (photoPaths.Count > 0)
            ShowPhoto(0);
        else
        {
            photoQuadRenderer.material.mainTexture = null;
            descriptionText3D.text = "(No photos)";
        }
    }

    public void DeleteCurrentPhoto()
    {
        if (photoPaths.Count == 0) return;

        string photoPath = photoPaths[currentIndex];
        string txtPath = Path.ChangeExtension(photoPath, ".txt");

        if (File.Exists(photoPath)) File.Delete(photoPath);
        if (File.Exists(txtPath)) File.Delete(txtPath);

        Debug.Log("Deleted photo: " + Path.GetFileName(photoPath));
        RefreshAlbumList();
    }

    public void AddNewPhoto(string path)
    {
        if (!photoPaths.Contains(path))
        {
            photoPaths.Add(path);
            photoPaths.Sort();
        }

        ShowPhoto(photoPaths.IndexOf(path));
    }
    public bool IsAlbumCurrentlyOpen()
    {
        return isAlbumOpen;
    }

}
