#if UNITY_EDITOR && ADVANCED_LODS_HELPER_SYSTEM

using BrainFailProductions.PolyFew;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Formats.Fbx.Exporter; //Need to import FBX Exporter package from unity registry
using System.IO;



namespace AdvancedLODsHelperSystem
{
    public static class AdvancedLODsHelperSystem
    {
        [MenuItem("CONTEXT/LODGroup/0.Auto Generate LODs with PolyFew")]
        public async static void GenerateLODsWithPolyFew(MenuCommand command)
        {
            Debug.LogError($"!Nu uita sa dezactivezi AutoSave Prefab!");

            DeleteAllLodsWithoutFirst(command);

            LODGroup lodGroup = (LODGroup)command.context;
            if (lodGroup == null)
            {
                Debug.LogError("No LODGroup component found on this object.");
                return;
            }

            Transform parentTransform = lodGroup.transform;

            //scoatem mesh-ul de pe obiectul parinte al prefabului
            if (parentTransform.GetComponent<MeshFilter>() && parentTransform.GetComponent<MeshRenderer>())
            {
                GameObject lodObject = new GameObject($"{parentTransform.name}_LOD0");
                lodObject.transform.SetParent(parentTransform);
                lodObject.AddComponent<MeshFilter>().sharedMesh = parentTransform.GetComponent<MeshFilter>().sharedMesh;
                lodObject.AddComponent<MeshRenderer>().sharedMaterials = parentTransform.GetComponent<MeshRenderer>().sharedMaterials;
                GameObject.DestroyImmediate(parentTransform.GetComponent<MeshFilter>());
                GameObject.DestroyImmediate(parentTransform.GetComponent<MeshRenderer>());
            }

            LOD[] lods = lodGroup.GetLODs();
            int lodCount = lods.Length;

            // Caută toate obiectele care au MeshFilter și MeshRenderer în children
            MeshFilter[] meshFilters = parentTransform.GetComponentsInChildren<MeshFilter>();
            MeshRenderer[] meshRenderers = parentTransform.GetComponentsInChildren<MeshRenderer>();

            if (meshFilters.Length == 0 || meshRenderers.Length == 0)
            {
                Debug.LogError("No objects with MeshFilter and MeshRenderer found.");
                return;
            }

            foreach (var meshFilter in meshFilters)
            {
                GameObject obj = meshFilter.gameObject;

                // Verifică dacă MeshRenderer este prezent pe același obiect
                MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    Debug.LogError($"MeshRenderer missing on object {obj.name}.");
                    continue;
                }

                // Generează LOD-urile noi în funcție de numărul de LOD-uri din LODGroup
                for (int i = 1; i < lodCount; i++)
                {
                    GameObject lodObject = new GameObject($"{obj.name.Split("_LOD")[0]}_LOD{i}");
                    lodObject.transform.SetParent(obj.transform);

                    lodObject.transform.localPosition = Vector3.zero;
                    lodObject.transform.localEulerAngles = Vector3.zero;

                    MeshFilter lodMeshFilter = lodObject.AddComponent<MeshFilter>();
                    MeshRenderer lodMeshRenderer = lodObject.AddComponent<MeshRenderer>();

                    // Setează materialele pentru LOD
                    lodMeshRenderer.sharedMaterials = meshRenderer.sharedMaterials;
                    lodMeshFilter.sharedMesh = meshFilter.sharedMesh;

                    Selection.activeGameObject = lodObject;

                    PolyFew polyFew1 = lodObject.GetComponent<PolyFew>();
                    if (polyFew1 == null)
                    {
                        polyFew1 = lodObject.AddComponent<PolyFew>();
                    }

                    // Configurează PolyFew cu setările tale
                    // Rezumat:
                    // preserveBorders: Protejează marginile deschise ale modelului.
                    // preserveUVSeams: Protejează zonele cu cusături UV pentru a evita distorsiuni în texturi.
                    // preserveUVFoldover: Protejează zonele în care UV - urile se pliază sau se suprapun.
                    // useEdgeSort: Oferă o reducere a poligoanelor mai precisă și de calitate superioară.
                    // recalculateNormals: Recalculează normalele mesh - ului pentru o iluminare corectă după optimizare.
                    // regardCurvature: Protejează detaliile de curbură și reduce poligoanele în zonele plane.
                    polyFew1.dataContainer.reductionStrength = /*(100 / lodCount) * i;*/Mathf.Abs(lodGroup.GetLODs()[i].screenRelativeTransitionHeight * 100 - 100);
                    
                    polyFew1.dataContainer.preserveBorders = true;
                    polyFew1.dataContainer.useEdgeSort = true;
                    polyFew1.dataContainer.recalculateNormals = true;

                    polyFew1.dataContainer.preserveUVFoldover = true;
                    polyFew1.dataContainer.preserveUVSeams = true;
                    polyFew1.dataContainer.regardCurvature = false;

                    await Task.Delay(100); //Awaitable.NextFrameAsync();

                    try
                    {
                        BrainFailProductions.PolyFew.InspectorDrawer.ManualUpdateMeshOptimizedResult();
                    }
                    catch (System.Exception)
                    {
                        throw;
                    }

                    await Task.Delay(100); // Awaitable.NextFrameAsync();

                    SaveMeshSubAssetMainFunc(lodMeshFilter);
                }
            }

            // Resetarea referințelor din LODGroup pentru a reflecta noile LOD-uri generate
            for (int i = 0; i < lodCount; i++)
            {
                // Obține toate obiectele care conțin "_LOD" urmat de numărul i (de exemplu: _LOD0, _LOD1, _LOD2 etc.)
                List<MeshRenderer> lodRenderers = new List<MeshRenderer>();
                foreach (Transform child in parentTransform.GetComponentsInChildren<Transform>(true)) // true include și inactivele
                {
                    if (child.name.Contains($"_LOD{i}")) // Verifică dacă numele conține "_LOD{i}"
                    {
                        MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
                        if (meshRenderer != null)
                        {
                            lodRenderers.Add(meshRenderer);
                        }
                    }
                }

                // Setează referințele pentru acest LOD
                lods[i].renderers = lodRenderers.ToArray();
            }

            lodGroup.SetLODs(lods);

            Debug.Log("LOD generation with PolyFew completed.");
        }

        

        [MenuItem("CONTEXT/LODGroup/1.*Set Corect Names For LODs")]
        public static void SetCorectNamesLods(MenuCommand command)
        {
            SetCorectNamesMainFunc((LODGroup)command.context);
        }

        private static void SetCorectNamesMainFunc(LODGroup lODGroup)
        {
            if (lODGroup != null)
            {
                LOD[] lods = lODGroup.GetLODs();

                if (lods.Length == 0)
                {
                    Debug.LogWarning("LODGroup nu conține LOD-uri.");
                    return;
                }

                MeshFilter meshFilterLOD0 = lods[0].renderers[0].GetComponent<MeshFilter>();

                if (meshFilterLOD0 == null)
                {
                    Debug.LogWarning("LOD 0 nu conține un MeshFilter valid.");
                    return;
                }

                if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                {
                    // Verifică dacă obiectul selectat este root-ul unui prefab
                    GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(meshFilterLOD0.gameObject);
                    if (prefabRoot.GetComponent<MeshFilter>())
                    {
                        GameObject emptyObject = new GameObject(meshFilterLOD0.name);
                        emptyObject.AddComponent<MeshFilter>().sharedMesh = meshFilterLOD0.sharedMesh;
                        emptyObject.AddComponent<MeshRenderer>().materials = meshFilterLOD0.GetComponent<MeshRenderer>().sharedMaterials;

                        emptyObject.transform.position = meshFilterLOD0.transform.position;
                        emptyObject.transform.rotation = meshFilterLOD0.transform.rotation;
                        emptyObject.transform.localScale = meshFilterLOD0.transform.localScale;

                        int indexLoop = 0;
                        foreach (var lod in lods[0].renderers)
                        {
                            if (lod == prefabRoot.GetComponent<Renderer>())
                            {
                                lods[0].renderers[indexLoop] = emptyObject.GetComponent<Renderer>();
                            }
                            indexLoop++;
                        }
                        lODGroup.SetLODs(lods);

                        GameObject.DestroyImmediate(meshFilterLOD0.GetComponent<MeshRenderer>());
                        GameObject.DestroyImmediate(meshFilterLOD0);

                        emptyObject.transform.SetParent(PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot.transform);

                        EditorSceneManager.MarkSceneDirty(PrefabStageUtility.GetCurrentPrefabStage().scene);
                    }
                }

                for (int i = 0; i < lods.Length; i++)
                {
                    Renderer[] renderers = lods[i].renderers;

                    foreach (Renderer renderer in renderers)
                    {
                        if (!renderer) continue;

                        renderer.gameObject.name = renderer.gameObject.name.Split("_LOD")[0];
                        renderer.gameObject.name = renderer.gameObject.name + "_LOD" + i;
                    }
                }
            }
            else
            {
                Debug.LogWarning("LODGroup nu a fost găsit.");
            }
        }

        [MenuItem("CONTEXT/LODGroup/2.Edit All Lods With PolyFew")]
        public static void PreparePolyFewLodGroup(MenuCommand command)
        {
            LODGroup lODGroup = (LODGroup)command.context;

            if (lODGroup != null)
            {
                LOD[] lods = lODGroup.GetLODs();

                if (lods.Length == 0)
                {
                    Debug.LogWarning("LODGroup nu conține LOD-uri.");
                    return;
                }

                MeshFilter meshFilterLOD0 = lods[0].renderers[0].GetComponent<MeshFilter>();

                if (meshFilterLOD0 == null)
                {
                    Debug.LogWarning("LOD 0 nu conține un MeshFilter valid.");
                    return;
                }

                Mesh meshLOD0 = meshFilterLOD0.sharedMesh;

                for (int i = 1; i < lods.Length; i++)
                {
                    Renderer[] renderers = lods[i].renderers;

                    foreach (Renderer renderer in renderers)
                    {
                        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();

                        if (meshFilter != null)
                        {
                            meshFilter.sharedMesh = meshLOD0;

                            if (!meshFilter.gameObject.GetComponent<PolyFew>())
                            {
                                meshFilter.gameObject.AddComponent<PolyFew>();
                            }
                        }
                    }
                }
                EditorSceneManager.MarkSceneDirty(PrefabStageUtility.GetCurrentPrefabStage().scene);

                Debug.Log($"Mesh-ul din LOD 0 a fost setat pentru toate LOD-urile și componenta PolyFew a fost adăugată pentru LOD-urile de la 1 în sus.");
            }
            else
            {
                Debug.LogWarning("LODGroup nu a fost găsit.");
            }
        }

        [MenuItem("CONTEXT/LODGroup/3.Delete PolyFew From All Lods")]
        public static void DeletePolyFewLodGroup(MenuCommand command)
        {
            LODGroup lODGroup = (LODGroup)command.context;

            if (lODGroup != null)
            {
                LOD[] lods = lODGroup.GetLODs();

                if (lods.Length == 0)
                {
                    Debug.LogWarning("LODGroup nu conține LOD-uri.");
                    return;
                }

                MeshFilter meshFilterLOD0 = lods[0].renderers[0].GetComponent<MeshFilter>();

                if (meshFilterLOD0 == null)
                {
                    Debug.LogWarning("LOD 0 nu conține un MeshFilter valid.");
                    return;
                }

                Mesh meshLOD0 = meshFilterLOD0.sharedMesh;

                for (int i = 1; i < lods.Length; i++)
                {
                    Renderer[] renderers = lods[i].renderers;

                    foreach (Renderer renderer in renderers)
                    {
                        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();

                        if (meshFilter != null)
                        {
                            if (meshFilter.gameObject.GetComponent<PolyFew>())
                            {
                                GameObject.DestroyImmediate(meshFilter.gameObject.GetComponent<PolyFew>());
                            }
                        }
                    }
                }

                EditorSceneManager.MarkSceneDirty(PrefabStageUtility.GetCurrentPrefabStage().scene);
            }
            else
            {
                Debug.LogWarning("LODGroup nu a fost găsit.");
            }
        }

        [MenuItem("CONTEXT/LODGroup/4.Delete All Lods Without LOD 0")]
        public static void DeleteAllLodsWithoutFirst(MenuCommand command)
        {
            LODGroup lODGroup = (LODGroup)command.context;

            if (lODGroup != null)
            {
                LOD[] lods = lODGroup.GetLODs();

                if (lods.Length == 0)
                {
                    Debug.LogWarning("LODGroup nu conține LOD-uri.");
                    return;
                }

                for (int i = 1; i < lods.Length; i++)
                {
                    Renderer[] renderers = lods[i].renderers;

                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer) GameObject.DestroyImmediate(renderer.gameObject);
                    }
                }

                EditorSceneManager.MarkSceneDirty(PrefabStageUtility.GetCurrentPrefabStage().scene);
            }
            else
            {
                Debug.LogWarning("LODGroup nu a fost găsit.");
            }
        }

        /// <summary>
        /// //////////////////////////////////////////////////////////
        /// </summary>

        [MenuItem("CONTEXT/MeshFilter/1.Edit Mesh With PolyFew")]
        public static void EditMeshWithPolyFew(MenuCommand command)
        {
            MeshFilter meshFilter = (MeshFilter)command.context;

            if (meshFilter != null)
            {
                meshFilter.gameObject.AddComponent<PolyFew>();
            }
            else
            {
                Debug.LogWarning("LODGroup nu a fost găsit.");
            }
        }

        [MenuItem("CONTEXT/MeshFilter/2.*Save Mesh In Prefab If It Not Asset")]
        public static void SaveMeshAsSubAsset(MenuCommand command)
        {
            SaveMeshSubAssetMainFunc((MeshFilter)command.context);
        }

        private static void SaveMeshSubAssetMainFunc(MeshFilter meshFilter)
        {
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                // Obține PrefabStage-ul curent
                PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

                if (prefabStage != null)
                {
                    // Calea către prefab-ul deschis în Prefab Edit Mode
                    string prefabPath = prefabStage.assetPath;

                    // Încarcă prefab-ul pentru editare
                    GameObject prefab = prefabStage.prefabContentsRoot;

                    // Caută MeshFilter în prefab și adaugă mesh-ul ca sub-asset dacă nu există
                    MeshFilter prefabMeshFilter = meshFilter;

                    if (prefabMeshFilter != null)
                    {
                        // Creează o copie a mesh-ului
                        Mesh meshCopy = Object.Instantiate(meshFilter.sharedMesh);
                        meshCopy.name = meshFilter.gameObject.name;

                        // Adaugă mesh-ul ca sub-asset în cadrul prefab-ului deschis
                        AssetDatabase.AddObjectToAsset(meshCopy, prefabPath);
                        meshFilter.sharedMesh = meshCopy;

                        PolyFew polyFew = meshFilter.GetComponent<PolyFew>();
                        if (polyFew) GameObject.DestroyImmediate(polyFew);

                        AssetDatabase.SaveAssets();

                        Debug.Log("Mesh salvat ca sub-asset în prefab la: " + prefabPath);
                    }
                    else
                    {
                        Debug.LogWarning("Prefab-ul deschis nu conține un MeshFilter.");
                    }
                }
                else
                {
                    Debug.LogWarning("Niciun prefab nu este deschis în modul de editare.");
                }
            }
            else
            {
                Debug.LogWarning("MeshFilter sau mesh-ul nu a fost găsit.");
            }
        }

        [MenuItem("CONTEXT/LODGroup/5.Delete Unused Mesh Sub-Assets In Opened Prefab")]
        [MenuItem("CONTEXT/MeshFilter/3.Delete Unused Mesh Sub-Assets In Opened Prefab")]
        public static void DeleteUnusedMeshSubAssetsInOpenPrefab(MenuCommand command)
        {
            // Obține PrefabStage-ul curent
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            if (prefabStage != null)
            {
                // Calea către prefab-ul deschis în Prefab Edit Mode
                string prefabPath = prefabStage.assetPath;

                // Încarcă prefab-ul pentru editare
                GameObject prefabRoot = prefabStage.prefabContentsRoot;

                // Obține toate MeshFilter-urile din prefab
                MeshFilter[] meshFiltersInPrefab = prefabRoot.GetComponentsInChildren<MeshFilter>();

                // Obține toate sub-asset-urile de tip Mesh din acest prefab
                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(prefabPath);
                var meshSubAssets = allAssets.OfType<Mesh>().ToList();

                // Obține toate mesh-urile utilizate în MeshFilter
                var usedMeshes = meshFiltersInPrefab
                                    .Where(mf => mf.sharedMesh != null)
                                    .Select(mf => mf.sharedMesh)
                                    .ToList();

                int deletedCount = 0;

                // Șterge mesh-urile care nu sunt utilizate
                foreach (var mesh in meshSubAssets)
                {
                    if (!usedMeshes.Contains(mesh))
                    {
                        // Șterge sub-asset-ul
                        Object.DestroyImmediate(mesh, true);
                        deletedCount++;
                    }
                }

                // Salvează modificările
                AssetDatabase.SaveAssets();

                Debug.Log($"{deletedCount} mesh-uri neutilizate au fost șterse din prefab la: {prefabPath}");
            }
            else
            {
                Debug.LogWarning("Niciun prefab nu este deschis în modul de editare.");
            }
        }
    }




    public class FbxEditor : Editor
    {
        private static GameObject tempPrefab;
        private static string originalFbxPath;
        private static string tempPrefabPath = "Assets/TempFbxEditPrefab.prefab";

        [MenuItem("CONTEXT/ModelImporter/Edit FBX Mode")]
        public static void OpenFbxEditMode(MenuCommand command)
        {
            // Obținem calea originală a fișierului FBX
            originalFbxPath = AssetDatabase.GetAssetPath(command.context);

            if (Path.GetExtension(originalFbxPath).ToLower() == ".fbx")
            {
                // Instanțiem FBX-ul ca un prefab temporar
                GameObject fbxObject = AssetDatabase.LoadAssetAtPath<GameObject>(originalFbxPath);
                tempPrefab = PrefabUtility.InstantiatePrefab(fbxObject) as GameObject;

                if (tempPrefab != null)
                {
                    tempPrefab.name = fbxObject.name + "_TempPrefab";

                    // Salvează temporar prefab-ul
                    tempPrefabPath = originalFbxPath.Split(".fbx")[0] + "_TEMP_PREFAB.prefab";
                    GameObject tmpPrefab = PrefabUtility.SaveAsPrefabAsset(tempPrefab, tempPrefabPath);
                    AssetDatabase.Refresh();

                    // Intrăm în Prefab Edit Mode pentru Prefab-ul temporar
                    PrefabUtility.UnpackPrefabInstance(tempPrefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    PrefabUtility.ConnectGameObjectToPrefab(tempPrefab, tmpPrefab);
                    PrefabStage.prefabStageClosing += OnPrefabStageClosing;
                    AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<Object>(tempPrefabPath));
                    Debug.LogError("!Nu uita sa opresti autosave la edit prefab mode!");
                }
            }
            else
            {
                Debug.LogError("Fișierul selectat nu este un FBX.");
            }
        }

        private async static void OnPrefabStageClosing(PrefabStage stage)
        {
            // Asigură-te că prefab-ul pe care îl închidem este cel temporar
            if (stage.assetPath == tempPrefabPath)
            {
                // Conversia Prefab-ului temporar înapoi în FBX
                if (tempPrefab != null && !string.IsNullOrEmpty(originalFbxPath))
                {
                    ConvertPrefabToFbx(tempPrefab, originalFbxPath);

                    AssetDatabase.DeleteAsset(tempPrefabPath);

                    // Ștergem prefab-ul temporar din scenă
                    if (tempPrefab != null)
                    {
                        DestroyImmediate(tempPrefab);
                    }

                    AssetDatabase.Refresh();
                    Debug.Log($"FBX-ul {originalFbxPath} a fost actualizat.");
                }

                // Deregistrăm callback-ul pentru a nu declanșa din nou
                PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
            }
        }

        private static void ConvertPrefabToFbx(GameObject prefab, string fbxPath)
        {
            // Exportăm Prefab-ul în FBX
            ModelExporter.ExportObject(fbxPath, prefab);
            Debug.Log($"Prefab-ul {prefab.name} a fost exportat în {fbxPath}");
        }
    }

}
#endif