using UnityEngine;
using UnityEngine.Rendering;
using Rive;

/// <summary>
/// A Unity component for integrating Rive animations.
/// This component manages rendering, which needs to be able to take in inputs to handle updates to the animation.
/// </summary>
[RequireComponent(typeof(UnityEngine.Renderer))]
public class RiveComponent : MonoBehaviour
{
    public Asset riveAsset; // The Rive asset to render
    public int textureSize = 512; // Texture size, warps image if x and y are different.

    [Tooltip("Fit mode for the Rive animation.")]
    public Fit fit = Fit.Contain; // Fit mode for the Rive animation

    [Tooltip("Set animation to render on awake.")]
    public bool playing = true; // Determines if animation plays on update

    [Tooltip("Flip the texture on the Y-axis.")]
    public bool flipY = true; // Rive animations often have to be flipped on the Y-Axis due to how they are rendered
    

    private RenderTexture renderTexture;
    private Rive.RenderQueue renderQueue;
    private Rive.Renderer riveRenderer;
    private CommandBuffer commandBuffer;
    private File riveFile;
    private Artboard artboard;
    private StateMachine stateMachine;

        private void Awake()
    {
        if (riveAsset == null)
        {
            Debug.LogError("Rive asset is not assigned!");
            return;
        }

        InitializeRenderTexture();
        InitializeRive();
        AssignRenderTextureToMaterial();
    }

    private void InitializeRenderTexture()
    {
        renderTexture = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true
        };
        renderTexture.Create();
    }

    private void InitializeRive()
    {
        riveFile = Rive.File.Load(riveAsset);
        artboard = riveFile.Artboard(0);
        stateMachine = artboard?.StateMachine();

        renderQueue = new Rive.RenderQueue(renderTexture);
        riveRenderer = renderQueue.Renderer();
        riveRenderer.Align(fit, Alignment.Center, artboard);
        riveRenderer.Draw(artboard);

        commandBuffer = new CommandBuffer();
        commandBuffer.SetRenderTarget(renderTexture);
        commandBuffer.ClearRenderTarget(true, true, UnityEngine.Color.clear, 0.0f);
        riveRenderer.AddToCommandBuffer(commandBuffer);
    }

    private void Update()
    {
        if (playing)
        {
            stateMachine?.Advance(Time.deltaTime);
            riveRenderer.Submit();
            GL.InvalidateState();

            // Dynamically ensure RenderTexture assignment
            AssignRenderTextureToMaterial();
        }
    }


    
    private void AssignRenderTextureToMaterial()
    {
        UnityEngine.Renderer renderer = GetComponent<UnityEngine.Renderer>();
        if (renderer != null && renderer.material != null && renderer.material.mainTexture != renderTexture)
        {
            renderer.material.mainTexture = renderTexture;

            // Flip the texture on the Y-axis if the flag is enabled
            if (flipY)
            {
                renderer.material.mainTextureScale = new Vector2(1, -1);
                renderer.material.mainTextureOffset = new Vector2(0, 1); // Adjust offset to align the flip
            }
            else
            {
                renderer.material.mainTextureScale = Vector2.one;
                renderer.material.mainTextureOffset = Vector2.zero;
            }
        }
    }

    private void OnDestroy()
    {
        renderTexture?.Release();
        commandBuffer?.Dispose();
        riveFile?.Dispose();
    }

    // Helper Methods
    private bool ValidateStateMachine()
    {
        if (stateMachine == null)
        {
            Debug.LogWarning("State machine is null.");
            return false;
        }
        return true;
    }

    private bool ValidateArtboard()
    {
        if (artboard == null)
        {
            Debug.LogWarning("Artboard is null.");
            return false;
        }
        return true;
    }



// Resusable public methods to be called to manipulate Rive animation

    /// <summary>
    /// Sets the value of a number input, either as a normal or nested input, within the state machine or artboard.
    /// </summary>
    /// <param name="inputName">The name of the input to set.</param>
    /// <param name="value">The value to assign to the input.</param>
    /// <param name="nested">Indicates whether the input is nested. Defaults to false.</param>
    /// <param name="path">The path to the nested input (only used if <paramref name="nested"/> is true).</param>
    /// <remarks>
    /// If the <paramref name="nested"/> parameter is true, the method will set the number input at the specified path within the artboard.
    /// Otherwise, it will attempt to set the number input within the state machine.
    /// </remarks>
    /// <exception cref="NullReferenceException">Thrown if the state machine or artboard is null.</exception>
    /// <example>
    /// Normal input:
    /// <code>
    /// SetNumberInput("Speed", 1.5f);
    /// </code>
    /// Nested input:
    /// <code>
    /// SetNumberInput("NestedSpeed", 2.5f, true, "Character/Motion");
    /// </code>
    /// </example>
    public void SetNumberInput(string inputName, float value, bool nested = false, string path = " ")
    {
        // Validate artboard and state machine
        if (!ValidateArtboard() || !ValidateStateMachine())
        {
            return;
        }

        if (nested)
        {
            artboard.SetNumberInputStateAtPath(inputName, value, path);
            Debug.Log($"Set number nested input '{inputName}' to {value}");
            return;
        }

        SMINumber numberInput = stateMachine.GetNumber(inputName);
        if (numberInput != null)
        {
            numberInput.Value = value;
            Debug.Log($"Set number input '{inputName}' to {value}");
        }

        else
        {
            Debug.LogWarning($"Number input '{inputName}' not found in the state machine.");
        }
    }


    /// <summary>
    /// Sets the value of a boolean input, either as a normal or nested input, within the state machine or artboard.
    /// </summary>
    /// <param name="inputName">The name of the boolean input to set.</param>
    /// <param name="value">The value to assign to the input (true or false).</param>
    /// <param name="nested">Indicates whether the input is nested. Defaults to false.</param>
    /// <param name="path">The path to the nested input (only used if <paramref name="nested"/> is true).</param>
    /// <remarks>
    /// If the <paramref name="nested"/> parameter is true, the method will set the boolean input at the specified path within the artboard.
    /// Otherwise, it will attempt to set the boolean input within the state machine.
    /// </remarks>
    /// <exception cref="NullReferenceException">Thrown if the state machine or artboard is null.</exception>
    /// <example>
    /// Normal input:
    /// <code>
    /// SetBooleanInput("IsRunning", true);
    /// </code>
    /// Nested input:
    /// <code>
    /// SetBooleanInput("NestedIsRunning", false, true, "Character/State");
    /// </code>
    /// </example>
    public void SetBooleanInput(string inputName, bool value, bool nested = false, string path = " ")
    {
        // Validate artboard and state machine
        if (!ValidateArtboard() || !ValidateStateMachine())
        {
            return;
        }

        if (nested)
        {
            artboard.SetBooleanInputStateAtPath(inputName, value, path);
            Debug.Log($"Set bool nested input '{inputName}' to {value}");
            return;
        }

        SMIBool boolInput = stateMachine.GetBool(inputName);
        if (boolInput != null)
        {
            boolInput.Value = value;
            Debug.Log($"Set boolean input '{inputName}' to {value}");
        }
        else
        {
            Debug.LogWarning($"Boolean input '{inputName}' not found in the state machine.");
        }
    }


    /// <summary>
    /// Triggers an input, either as a normal or nested input, within the state machine or artboard.
    /// </summary>
    /// <param name="inputName">The name of the trigger input to fire.</param>
    /// <param name="nested">Indicates whether the input is nested. Defaults to false.</param>
    /// <param name="path">The path to the nested input (only used if <paramref name="nested"/> is true).</param>
    /// <remarks>
    /// If the <paramref name="nested"/> parameter is true, the method will fire the trigger input at the specified path within the artboard.
    /// Otherwise, it will attempt to fire the trigger input within the state machine.
    /// </remarks>
    /// <exception cref="NullReferenceException">Thrown if the state machine or artboard is null.</exception>
    /// <example>
    /// Normal input:
    /// <code>
    /// TriggerInput("Jump");
    /// </code>
    /// Nested input:
    /// <code>
    /// TriggerInput("NestedJump", true, "Character/Actions");
    /// </code>
    /// </example>
    public void TriggerInput(string inputName, bool nested = false, string path = " ")
    {
        // Validate artboard and state machine
        if (!ValidateArtboard() || !ValidateStateMachine())
        {
            return;
        }

        if (nested)
        {
            artboard.FireInputStateAtPath(inputName, path);
            Debug.Log($"Fired nested trigger input '{inputName}'");
            return;
        }

        SMITrigger triggerInput = stateMachine.GetTrigger(inputName);
        if (triggerInput != null)
        {
            triggerInput.Fire();
            Debug.Log($"Fired trigger input '{inputName}'");
        }
        else
        {
            Debug.LogWarning($"Trigger input '{inputName}' not found in the state machine.");
        }
    }


    /// <summary>
    /// Sets the state machine to the specified state machine name within the artboard.
    /// </summary>
    /// <param name="stateMachineName">The name of the state machine to set.</param>
    /// <remarks>
    /// This method attempts to find the state machine by name in the associated artboard.
    /// If the state machine is found, it is set as the active state machine. 
    /// If the specified state machine does not exist, a warning is logged.
    /// </remarks>
    /// <exception cref="NullReferenceException">
    /// Thrown if the <c>riveFile</c> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// SetStateMachine("CharacterMovement");
    /// </code>
    /// </example>
    public void SetStateMachine(string stateMachineName)
    {
        if (riveFile == null) return;

        StateMachine newStateMachine = artboard?.StateMachine(stateMachineName);
        if (newStateMachine != null)
        {
            stateMachine = newStateMachine;
            Debug.Log($"StateMachine set to {stateMachineName}");
        }
        else
        {
            Debug.LogWarning($"StateMachine {stateMachineName} not found in artboard.");
        }
    }


    /// <summary>
    /// Sets the current artboard to the specified artboard name within the Rive file.
    /// </summary>
    /// <param name="artboardName">The name of the artboard to set.</param>
    /// <remarks>
    /// This method attempts to find the artboard by name within the associated Rive file.
    /// If the artboard is found, it becomes the active artboard, and the state machine and renderer are updated accordingly.
    /// If the specified artboard does not exist, a warning is logged.
    /// </remarks>
    /// <exception cref="NullReferenceException">
    /// Thrown if the <c>riveFile</c> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// SetArtboard("MainMenu");
    /// </code>
    /// </example>
    public void SetArtboard(string artboardName)
    {
        if (riveFile == null) return;

        Artboard newArtboard = riveFile.Artboard(artboardName);
        if (newArtboard != null)
        {
            artboard = newArtboard;
            stateMachine = artboard.StateMachine();
            riveRenderer.Align(fit, Alignment.Center, artboard);
            riveRenderer.Draw(artboard);
            Debug.Log($"Artboard switched to {artboardName}");
        }
        else
        {
            Debug.LogWarning($"Artboard {artboardName} not found in Rive file.");
        }
    }

}
