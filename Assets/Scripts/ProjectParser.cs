using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class ProjectParser : MonoBehaviour
{
    [SerializeField]
    private string rootProjectDirectory;

    [Button]
    public Dictionary<ISymbol, HashSet<string>> ScanProject(bool writeToFile=false)
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

        //var allNodes = syntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes());

        // Finding all major C# elements
        //var namespaceDeclarations = allNodes.OfType<NamespaceDeclarationSyntax>();
        //var classDeclarations = allNodes.OfType<ClassDeclarationSyntax>();
        //var interfaceDeclarations = allNodes.OfType<InterfaceDeclarationSyntax>();
        //var fieldDeclarations = allNodes.OfType<FieldDeclarationSyntax>();
        //var propertyDeclarations = allNodes.OfType<PropertyDeclarationSyntax>();
        //var methodDeclarations = allNodes.OfType<MethodDeclarationSyntax>();
        //var constructorDeclarations = allNodes.OfType<ConstructorDeclarationSyntax>();

        // Generating reference information for each element
        Dictionary<ISymbol, HashSet<string>> referencedSymbols = new Dictionary<ISymbol, HashSet<string>>();

        // Find all class declarations in the syntax trees along with their corresponding semantic models
        var classDeclarationsWithModels =
            syntaxTrees.SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                        .Select(decl => (Declaration: decl, SemanticModel: compilation.GetSemanticModel(tree))));

        // Add class symbols to the referencedSymbols dictionary if not already present
        foreach (var (classDeclaration, semanticModel) in classDeclarationsWithModels)
        {
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
            if (classSymbol != null && !referencedSymbols.ContainsKey(classSymbol))
            {
                referencedSymbols.Add(classSymbol, new HashSet<string>());
            }
        }

        //Add method symbols to ensure even non-referenced ones are added
        foreach (var semanticModel in semanticModelList)
        {
            SyntaxNode root = semanticModel.SyntaxTree.GetRoot();
            var methodDeclarations = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach(var method in methodDeclarations)
            {
                var methodSymbol = semanticModel.GetDeclaredSymbol(method);
                if (methodSymbol != null && !referencedSymbols.ContainsKey(methodSymbol))
                {
                    referencedSymbols.Add(methodSymbol, new HashSet<string>());
                }
            }
        }

        foreach (var semanticModel in semanticModelList)
        {
            SyntaxNode root = semanticModel.SyntaxTree.GetRoot();
            SymbolInfo info = semanticModel.GetSymbolInfo(root);
            if (info.Symbol != null && !referencedSymbols.ContainsKey(info.Symbol))
            {
                referencedSymbols.Add(info.Symbol, new HashSet<string>());
            }

            foreach (var syntaxNode in root.DescendantNodes())
            {
                var symbolInfo = semanticModel.GetSymbolInfo(syntaxNode);
                if (symbolInfo.Symbol == null)
                {
                    continue;
                }

                if (!referencedSymbols.ContainsKey(symbolInfo.Symbol))
                {
                    referencedSymbols[symbolInfo.Symbol] = new HashSet<string>();
                }

                var containingMethod = syntaxNode.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                var containingType = syntaxNode.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();

                if (containingType == null)
                {
                    continue;
                }

                if (containingMethod == null)
                {
                    ConstructorDeclarationSyntax ctor = syntaxNode.Ancestors().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
                    if (ctor != null)
                    {
                        string str = $"{semanticModel.GetDeclaredSymbol(ctor)}";
                        referencedSymbols[symbolInfo.Symbol].Add(str);
                    }
                }
                else
                {
                    string str = $"{semanticModel.GetDeclaredSymbol(containingMethod)}";
                    referencedSymbols[symbolInfo.Symbol].Add(str);
                }
            }
        }

        if (writeToFile)
        {
            WriteToFile(referencedSymbols);
        }

        return referencedSymbols;
    }

    private void WriteToFile(Dictionary<ISymbol, HashSet<string>> referencedSymbols)
    {
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

        string outputFilePath = Path.Combine(Application.dataPath, "output.txt");
        File.WriteAllText(outputFilePath, output.ToString());
    }
}
