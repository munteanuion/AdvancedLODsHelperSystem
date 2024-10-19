using UnityEditor;
using System.IO;
using UnityEngine;

public class PackageFileMover : AssetPostprocessor
{
    const string PACKAGE_NAME = "com.munteanuion.lodhelper";



    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        // Verifică dacă pachetul specific a fost importat
        foreach (string asset in importedAssets)
        {
            if (asset.Contains(PACKAGE_NAME))
            {
                ReplaceOldFilesInAssetsFolder();
                break;
            }
        }

        AddDefineSymbol();

        void AddDefineSymbol()
        {
            // Define simbolul pe care vrei să-l adaugi
            string defineSymbol = "ADVANCED_LODS_HELPER_SYSTEM";

            // Obține simbolurile de define curente
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);

            // Verifică dacă simbolul există deja pentru a evita duplicarea
            if (!currentDefines.Contains(defineSymbol))
            {
                // Adaugă noul define symbol la lista curentă
                currentDefines += ";" + defineSymbol;

                // Setează simbolurile noi
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, currentDefines);
                Debug.Log($"Added scripting define symbol: {defineSymbol}");
            }
            else
            {
                Debug.LogWarning($"Scripting define symbol {defineSymbol} already exists.");
            }
        }
    }
    [MenuItem("Tools/Move Files to Assets Folder")]
    // Funcția care șterge vechile fișiere și le înlocuiește cu noile fișiere din pachet
    static void ReplaceOldFilesInAssetsFolder()
    {
        string packageFolder = $"Library/PackageCache/{PACKAGE_NAME}/Runtime/AdvancedLODsHelperSystem"; // Calea din pachet
        string destinationFolder = "Assets/Plugins/AdvancedLODsHelperSystem"; // Calea în Assets

        // Dacă există fișiere în folderul de destinație, le șterge
        if (Directory.Exists(destinationFolder))
        {
            DeleteOldFiles(destinationFolder);
        }
        else
        {
            // Creează folderul de destinație dacă nu există
            Directory.CreateDirectory(destinationFolder);
        }

        // Copiază fișierele noi din pachet în Assets
        foreach (string file in Directory.GetFiles(packageFolder))
        {
            string fileName = Path.GetFileName(file);
            string destPath = Path.Combine(destinationFolder, fileName);

            // Copiază fișierul și suprascrie dacă există deja
            File.Copy(file, destPath, true);
        }

        // Mută fișierele de script și meta files
        string[] files = Directory.GetFiles(packageFolder, "*.cs");

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destinationFolder, fileName);
            string metaFile = file + ".meta"; // Fișierul meta corespunzător

            // Mută fișierul script
            File.Copy(file, destFile, true);  // Suprascrie fișierul existent
            Debug.Log($"Moved script: {fileName}");

            // Mută fișierul meta
            if (File.Exists(metaFile))
            {
                string destMetaFile = Path.Combine(destinationFolder, fileName + ".meta");
                File.Copy(metaFile, destMetaFile, true); // Suprascrie fișierul meta existent
                Debug.Log($"Moved meta file: {fileName}.meta");
            }
            else
            {
                Debug.LogWarning($"Meta file not found for: {fileName}");
            }
        }

        // Reîmprospătează Asset Database
        AssetDatabase.Refresh();
    }

    // Funcția care șterge toate fișierele din folderul de destinație
    static void DeleteOldFiles(string folderPath)
    {
        foreach (string file in Directory.GetFiles(folderPath))
        {
            File.Delete(file);
        }

        // Șterge și subfolderele dacă există
        foreach (string dir in Directory.GetDirectories(folderPath))
        {
            Directory.Delete(dir, true);
        }

        // Reîmprospătează Asset Database după ce fișierele au fost șterse
        AssetDatabase.Refresh();
    }
}
