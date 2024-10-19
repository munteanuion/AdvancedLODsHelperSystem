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

        //AddDefineSymbol();

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
        string packageFolder = $"Library\\PackageCache\\{PACKAGE_NAME}\\Runtime\\AdvancedLODsHelperSystem"; // Calea din pachet
        string destinationFolder = "Assets\\Plugins\\AdvancedLODsHelperSystem"; // Calea în Assets

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

        // Obține toate fișierele din folderul sursă
        string[] allFiles = Directory.GetFiles(packageFolder, "*.*", SearchOption.AllDirectories);

        foreach (string file in allFiles)
        {
            string fileName = Path.GetFileName(file);
            string destPath = Path.Combine(destinationFolder, fileName);

            // Copiază fișierul și suprascrie dacă există deja
            File.Copy(file, destPath, true);
            Debug.Log($"Moved file: {fileName}");
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
