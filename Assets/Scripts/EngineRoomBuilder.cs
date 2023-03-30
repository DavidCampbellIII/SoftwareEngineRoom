using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sirenix.OdinInspector;

public class EngineRoomBuilder : MonoBehaviour
{
    [SerializeField]
    private string rootProjectDirectory;

    [Button]
    private void ScanProject()
    {
        string path = Path.Combine(Application.dataPath, rootProjectDirectory);
        var csharpFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
        List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();

        foreach (var file in csharpFiles)
        {
            var fileContent = File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(fileContent);
            syntaxTrees.Add(syntaxTree);
        }

        var compilation = CSharpCompilation.Create("ProjectCompilation", syntaxTrees);
        var semanticModelList = syntaxTrees.Select(tree => compilation.GetSemanticModel(tree)).ToList();

        var allNodes = syntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes());

        // Finding all major C# elements
        var namespaceDeclarations = allNodes.OfType<NamespaceDeclarationSyntax>();
        var classDeclarations = allNodes.OfType<ClassDeclarationSyntax>();
        var interfaceDeclarations = allNodes.OfType<InterfaceDeclarationSyntax>();
        var fieldDeclarations = allNodes.OfType<FieldDeclarationSyntax>();
        var propertyDeclarations = allNodes.OfType<PropertyDeclarationSyntax>();
        var methodDeclarations = allNodes.OfType<MethodDeclarationSyntax>();
        var constructorDeclarations = allNodes.OfType<ConstructorDeclarationSyntax>();

        // Generating reference information for each element
        var referencedSymbols = new Dictionary<ISymbol, HashSet<string>>();

        foreach (var semanticModel in semanticModelList)
        {
            foreach (var syntaxNode in semanticModel.SyntaxTree.GetRoot().DescendantNodes())
            {
                var symbolInfo = semanticModel.GetSymbolInfo(syntaxNode);
                if (symbolInfo.Symbol != null)
                {
                    if (!referencedSymbols.ContainsKey(symbolInfo.Symbol))
                    {
                        referencedSymbols[symbolInfo.Symbol] = new HashSet<string>();
                    }

                    // Get the containing method or constructor for the reference
                    var containingMethodOrCtor = syntaxNode.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                    // Get the containing type for the reference
                    var containingType = syntaxNode.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();

                    if(containingMethodOrCtor == null || containingType == null)
                    {
                        continue;
                    }

                    string str = $"{containingType.Identifier}.{containingMethodOrCtor.Identifier}()";
                    referencedSymbols[symbolInfo.Symbol].Add(str);
                }
            }
        }

        // Output the information for each major element and where they are referenced
        foreach (var entry in referencedSymbols)
        {
            if(entry.Value.Count == 0)
            {
                continue;
            }
            Debug.Log($"Element: {entry.Key}");
            Debug.Log("Referenced in:");

            foreach (var reference in entry.Value)
            {
                Debug.Log($"\t- {reference}");
            }
        }
    }
}
