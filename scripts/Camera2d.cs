using Godot;
using System;

public partial class Camera2d : Camera2D
{
    private float zoomStep = 0.01f; //The Zoom step size
    private Vector2 minZoom = new Vector2(0.03f, 0.03f); //The minimum zoom level
    private Vector2 maxZoom = new Vector2(0.4f, 0.4f); //The  maximum zoom level

    //Camera shake variables
    private float shakeIntensity = 10f; //default value (edited when called)
    private RandomNumberGenerator rng = new RandomNumberGenerator();

    //use events to track button pressing/scrolling
    //I mostly did this manually for simplicity
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
            {
                ZoomIn();
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
            {
                ZoomOut();
            }
        }
    }

    public override void _Process(double delta)
    {
        if (shakeIntensity > 0)
        {
            Vector2 shakeOffset = new Vector2(
                rng.RandfRange(-shakeIntensity, shakeIntensity),
                rng.RandfRange(-shakeIntensity, shakeIntensity)
            );
            Offset = shakeOffset;
            
            shakeIntensity = Mathf.Lerp(shakeIntensity, 0, (float)delta * 5f); //Smoothly decrease the shake
        }
        else
        {
            Offset = Vector2.Zero; //Reset offset when the shake is finished
        }
    }

    private void ZoomIn()
    {
        if (Zoom > minZoom)
        {
            Zoom -= new Vector2(zoomStep, zoomStep);
        }
    }

    private void ZoomOut()
    {
        if (Zoom < maxZoom)
        {
            Zoom += new Vector2(zoomStep, zoomStep);
        }
    }

    public void ShakeCamera(float intensity)
    {
        shakeIntensity = intensity;
    }
}
