using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sirenix.OdinInspector;
using System.Text;

public class EngineRoomBuilder : MonoBehaviour
{
    [SerializeField]
    private string rootProjectDirectory;

    [Button]
    private void ScanProject()
    {
        string path = Path.Combine(Application.dataPath, rootProjectDirectory);
        string[] csharpFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
        List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();

        foreach (string file in csharpFiles)
        {
            string fileContent = File.ReadAllText(file);
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(fileContent);
            syntaxTrees.Add(syntaxTree);
        }

        CSharpCompilation compilation = CSharpCompilation.Create("ProjectCompilation", syntaxTrees);
        List<SemanticModel> semanticModelList = syntaxTrees.Select(tree => compilation.GetSemanticModel(tree)).ToList();

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
        StringBuilder output = new StringBuilder();

        foreach (var entry in referencedSymbols)
        {
            ISymbol symbol = entry.Key;
            output.AppendLine($"Element: {symbol} ({symbol.Kind})");
            output.AppendLine("Referenced in:");

            foreach (var reference in entry.Value)
            {
                output.AppendLine($"\t- {reference}");
            }
            output.AppendLine();
        }

        // Write the contents to a file
        string outputFilePath = Path.Combine(Application.dataPath, "output.txt");
        File.WriteAllText(outputFilePath, output.ToString());
    }
}
