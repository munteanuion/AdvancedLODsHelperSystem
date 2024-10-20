using UnityEditor;
using System.IO;
using UnityEngine;

public class PackageFileMover : AssetPostprocessor
{
    const string PACKAGE_NAME = "com.munteanuion.lodhelper";
    const string SYMBOL_NAME = "ADVANCED_LODS_HELPER_SYSTEM";

    static string packageFolder = $"Library\\PackageCache\\{PACKAGE_NAME}\\Runtime\\AdvancedLODsHelperSystem"; // Calea din pachet
    static string packageFolderThisScript = $"Library\\PackageCache\\{PACKAGE_NAME}\\Editor\\PackageFileMover.cs"; // Calea din pachet
    static string destinationFolder = "Assets\\Plugins\\AdvancedLODsHelperSystem"; // Calea în Assets



    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        bool isExistImportedAsset = false;
        
        foreach (string asset in importedAssets)
        {
            if (asset.Contains(PACKAGE_NAME))
            {
                isExistImportedAsset = true;
                break;
            }
        }

        if (!isExistImportedAsset)
        {
            Directory.Delete(destinationFolder);

            RemoveDefineSymbol();

            AssetDatabase.Refresh();
        }


#if !SYMBOL_NAME

        File.Delete(destinationFolder);

        RemoveDefineSymbol();

        AssetDatabase.Refresh();

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

        //File.Delete(packageFolderThisScript);

        AssetDatabase.Refresh();

#endif


        void AddDefineSymbol()
        {
            // Define simbolul pe care vrei să-l adaugi
            string defineSymbol = SYMBOL_NAME;

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
        static void RemoveDefineSymbol()
        {
            string defineSymbol = SYMBOL_NAME;

            // Obține simbolurile curente
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);

            // Dacă simbolul există, îl eliminăm
            if (currentDefines.Contains(defineSymbol))
            {
                // Elimină simbolul și actualizează lista
                currentDefines = currentDefines.Replace(defineSymbol, "").Replace(";;", ";").Trim(';');
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, currentDefines);
                Debug.Log($"Removed scripting define symbol: {defineSymbol}");
            }
            else
            {
                Debug.LogWarning($"Scripting define symbol {defineSymbol} does not exist.");
            }
        }


    [MenuItem("Tools/-Remove Old Version LOD Helper")]
    static void DeleteOldVersion()
    {
        Directory.Delete(destinationFolder);

        RemoveDefineSymbol();

        AssetDatabase.Refresh();
    }


    // Funcția care șterge vechile fișiere și le înlocuiește cu noile fișiere din pachet
    static void ReplaceOldFilesInAssetsFolder()
    {
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
            // Obține calea relativă a fișierului față de folderul sursă
            string relativePath = file.Substring(packageFolder.Length + 1); // +1 pentru a omite '\\'

            // Creează calea destinație, păstrând structura folderului
            string destPath = Path.Combine(destinationFolder, relativePath);
            string destDir = Path.GetDirectoryName(destPath); // Obține directorul destinație

            // Verifică dacă directorul destinație există, dacă nu, creează-l
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // Copiază fișierul și suprascrie dacă există deja
            File.Copy(file, destPath, true);
            Debug.Log($"Moved file: {relativePath}");
        }

        DeleteOldFiles(packageFolder);

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
