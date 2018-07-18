using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR.WSA.WebCam;

public class PhotoCaptureRawImageExample : MonoBehaviour
{
    PhotoCapture photoCaptureObject = null;
    Texture2D targetTexture = null;
    Renderer quadRenderer = null;

    // Use this for initialization
    void Start()
    {
        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

        targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height, TextureFormat.RGBA32, false);

        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject) {
            photoCaptureObject = captureObject;

            CameraParameters c = new CameraParameters();
            c.cameraResolutionWidth = targetTexture.width;
            c.cameraResolutionHeight = targetTexture.height;
            c.pixelFormat = CapturePixelFormat.BGRA32;

            captureObject.StartPhotoModeAsync(c, delegate (PhotoCapture.PhotoCaptureResult result) {
                photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            });
        });
    }

    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        List<byte> imageBufferList = new List<byte>();
        // Copy the raw IMFMediaBuffer data into our empty byte list.
        photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);

        // In this example, we captured the image using the BGRA32 format.
        // So our stride will be 4 since we have a byte for each rgba channel.
        // The raw image data will also be flipped so we access our pixel data
        // in the reverse order.
        int stride = 4;
        float denominator = 1.0f / 255.0f;
        List<Color> colorArray = new List<Color>();
        for (int i = imageBufferList.Count - 1; i >= 0; i -= stride)
        {
            float a = (int)(imageBufferList[i - 0]) * denominator;
            float r = (int)(imageBufferList[i - 1]) * denominator;
            float g = (int)(imageBufferList[i - 2]) * denominator;
            float b = (int)(imageBufferList[i - 3]) * denominator;

            colorArray.Add(new Color(r, g, b, a));
        }

        targetTexture.SetPixels(colorArray.ToArray());
        targetTexture.Apply();

        if (quadRenderer == null)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadRenderer = p.GetComponent<Renderer>() as Renderer;
            quadRenderer.material = new Material(Shader.Find("Custom/Unlit/UnlitTexture"));

            p.transform.parent = this.transform;
            p.transform.localPosition = new Vector3(0.0f, 0.0f, 1.0f);
        }

        quadRenderer.material.SetTexture("_MainTex", targetTexture);

        // Take another photo
        photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
    }
}