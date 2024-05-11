using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class ImageData
{
    public string image;
}
public class ForceAcceptAll : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        // Always accept
        return true;
    }
}
public class TakePhotos : MonoBehaviour
{

    public void TakePhoto()
    {
        StartCoroutine(TakeAPhoto());
    }

    IEnumerator TakeAPhoto()
    {
        // Wait until rendering is complete, before take the photo.
        yield return new WaitForEndOfFrame();

        Camera camera = Camera.main;
        int width = Screen.width;
        int height = Screen.height;
        // Create a new render texture the size of the screen.
        RenderTexture rt = new RenderTexture(width, height, 24);
        camera.targetTexture = rt;

        // The Render Texture in RenderTexture.active is the one
        // that will be read by ReadPixels.
        var currentRT = RenderTexture.active;
        RenderTexture.active = rt;

        // Render the camera's view.
        camera.Render();

        // Make a new texture and read the active Render Texture into it.
        Texture2D image = new Texture2D(width, height);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();

        // Change back the camera target texture.
        camera.targetTexture = null;

        // Replace the original active Render Texture.
        RenderTexture.active = currentRT;

        // Save to an image file.
        // Encode the image texture into PNG. Can be change to another image file.
        byte[] bytes = image.EncodeToPNG();


        // Sample file name: 20230925_192218.png
        string fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(filePath, bytes);

        // Free up memory.
        Destroy(rt);
        Destroy(image);

        // Call the SendImageToFlask coroutine and pass the image data
        StartCoroutine(SendImageToFlask(bytes));
        
    }

    IEnumerator SendImageToFlask(byte[] imageData)
    {
        string uri = "https://192.168.10.183:5000/";
        string path = "describe";

        // Create JSON data with the image bytes as base64 string
        ImageData data = new ImageData();
        data.image = Convert.ToBase64String(imageData);
        string json = JsonUtility.ToJson(data);
        byte[] postData = System.Text.Encoding.UTF8.GetBytes(json);

        // Create a UnityWebRequest to send the image data as a POST request
        using (UnityWebRequest www = new UnityWebRequest(uri + path, "POST"))
        {
            // Set the content type
            www.SetRequestHeader("Content-Type", "application/json");

            // Create a CertificateHandler that accepts all certificates
            var cert = new ForceAcceptAll();
            www.certificateHandler = cert;

            // Set the image data as the request body
            www.uploadHandler = new UploadHandlerRaw(postData);
            www.downloadHandler = new DownloadHandlerBuffer();

            // Send the request
            yield return www.SendWebRequest();

            // Dispose of the CertificateHandler
            cert?.Dispose();
            // Check for errors
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
            }
            else
            {
                Debug.Log("Image sent successfully to Flask");
            }
        }

        StartCoroutine(GetAudioClip_Coroutine());
    }

    IEnumerator GetAudioClip_Coroutine()
    {
        Debug.Log("Getting the voice file now");
        
        string uri = "https://192.168.10.183:5000/";
        string pathToAudio = "output";

        var cert = new ForceAcceptAll();
    
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri+pathToAudio, AudioType.WAV))
        {
            www.timeout = 130;

            www.certificateHandler = cert;

            yield return www.SendWebRequest();

            cert?.Dispose();

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
            }
            else
            {
                AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                AudioSource audioSource = GetComponent<AudioSource>();
                audioSource.clip = myClip;
                audioSource.Play();
            }

        }

    }
}
