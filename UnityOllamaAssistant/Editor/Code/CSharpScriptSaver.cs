/*
Autore: Fabrizio Radica
Versione: 1.1
Data: 2026-05-10
Descrizione: Salvataggio controllato editor-only di soli script .cs generati dall'assistente.
*/

using System.IO;
using UnityEditor;
using UnityEngine;

namespace RadicaDesign.UnityOllamaAssistant.Editor.Code
{
    public static class CSharpScriptSaver
    {
        private const string DefaultFolder = "Assets/Scripts";

        public static bool SaveGeneratedScript(CSharpGeneratedScriptCandidate candidate)
        {
            if (candidate == null || !candidate.IsValid)
            {
                EditorUtility.DisplayDialog(
                    "Salvataggio Script",
                    "Nessuno script C# valido da salvare.",
                    "OK"
                );

                return false;
            }

            EnsureDefaultFolder();

            string fileName = string.IsNullOrWhiteSpace(candidate.SuggestedFileName)
                ? "GeneratedScript.cs"
                : candidate.SuggestedFileName;

            string path = EditorUtility.SaveFilePanelInProject(
                "Salva script C# generato",
                fileName,
                "cs",
                "Salva esclusivamente uno script C# generato dall'assistente.",
                DefaultFolder
            );

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (!path.StartsWith("Assets/"))
            {
                EditorUtility.DisplayDialog(
                    "Percorso non valido",
                    "Lo script deve essere salvato dentro la cartella Assets del progetto Unity.",
                    "OK"
                );

                return false;
            }

            if (Path.GetExtension(path).ToLowerInvariant() != ".cs")
            {
                EditorUtility.DisplayDialog(
                    "Estensione non valida",
                    "Il sistema può salvare solo file .cs generati dall'assistente.",
                    "OK"
                );

                return false;
            }

            File.WriteAllText(path, candidate.Code);
            AssetDatabase.ImportAsset(path);
            AssetDatabase.Refresh();

            Debug.Log("Script C# generato salvato: " + path);
            return true;
        }

        private static void EnsureDefaultFolder()
        {
            if (AssetDatabase.IsValidFolder(DefaultFolder))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets"))
            {
                return;
            }

            AssetDatabase.CreateFolder("Assets", "Scripts");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
