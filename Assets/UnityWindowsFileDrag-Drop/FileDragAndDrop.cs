using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using B83.Win32;
using System.IO;
using System;
using spz;

public class FileDragAndDrop : MonoBehaviour
{
    void OnEnable()
    {
        // Must be installed on the main thread to get the right thread ID.
        UnityDragAndDropHook.InstallHook();
        UnityDragAndDropHook.OnDroppedFiles += OnFiles;
    }

    void OnDisable()
    {
        UnityDragAndDropHook.UninstallHook();
        UnityDragAndDropHook.OnDroppedFiles -= OnFiles;
    }

    void OnFiles(List<string> aFiles, B83.Win32.POINT aPos)
    {
        // Convert Windows POINT to Unity screen coordinates - just flip Y
        Vector2Int screenCoord = new Vector2Int(
            aPos.x, 
            Screen.height - aPos.y  // Flip Y coordinate only
        );

        Debug.Log("Drag and Drop Coord: " + screenCoord.x + ", " +screenCoord.y);

        // Do something with the dropped file names. aPos will contain the 
        // mouse position within the window where the files have been dropped.
        string str = "Drag-and-Dropped " + aFiles.Count + " files at: " + aPos + "\n\t" +
            aFiles.Aggregate((a, b) => a + "\n\t" + b);
        Debug.Log(str);

        if (AllFiles3D(aFiles))
        {
            // Handle 3D files
            ModelsHandler_3D_UI.instance.OnDragAndDrop_3D_File(aFiles[0]);
            return; // Only import the first model.
        }

        if (AllFilesImages(aFiles)){// Handle image files
            bool consumed = Gen3D_MGR.instance.OnImportedImages_DragAndDrop(aFiles, screenCoord);

            Debug.Log("Drag and Drop isConsumed after OnImportedImages_DragAndDrop: " + consumed);

            if (!consumed){ 
                consumed = Art2D_IconsUI_List.instance.OnImport_DragAndDrop(aFiles);
            }
            return; // Imported all the files, now return.
        }

        string msg = "Drag-and-drop contains unsupported file types.";
        Viewport_StatusText.instance.ShowStatusText(msg, false, 4, false);
    }

    bool AllFiles3D(List<string> files)
    {
        return files.All(file =>
        {
            string extension = Path.GetExtension(file).ToLowerInvariant();
            return extension.Equals(".obj", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".fbx", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".glb", StringComparison.OrdinalIgnoreCase);
        });
    }

    bool AllFilesImages(List<string> files)
    {
        return files.All(file =>
        {
            string extension = Path.GetExtension(file).ToLowerInvariant();
            return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".tga", StringComparison.OrdinalIgnoreCase);
        });
    }
}
