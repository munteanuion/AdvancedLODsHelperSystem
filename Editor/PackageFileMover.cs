using UnityEditor;
using System.IO;

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
