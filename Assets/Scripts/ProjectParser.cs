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
using System;
using System.Reflection;

public class ProjectParser : MonoBehaviour
{
    public string rootProjectDirectory;

    [Button]
    public Dictionary<ISymbol, HashSet<string>> ScanProject(bool writeToFile=false)
    {
        //string path = Path.Combine(Application.dataPath, rootProjectDirectory);
        string[] csharpFiles = Directory.GetFiles(rootProjectDirectory, "*.cs", SearchOption.AllDirectories);
        List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();

        foreach (string file in csharpFiles)
        {
            string fileContent = File.ReadAllText(file);
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(fileContent);
            syntaxTrees.Add(syntaxTree);
        }

        CSharpCompilation compilation = CSharpCompilation.Create("ProjectCompilation", syntaxTrees);
        List<SemanticModel> semanticModelList = syntaxTrees.Select(tree => compilation.GetSemanticModel(tree)).ToList();

        Dictionary<ISymbol, HashSet<string>> referencedSymbols = new Dictionary<ISymbol, HashSet<string>>();

        //First pass to add in all the classes and methods
        foreach (SemanticModel semanticModel in semanticModelList)
        {
            SyntaxNode root = semanticModel.SyntaxTree.GetRoot();
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (ClassDeclarationSyntax classDeclaration in classDeclarations)
            {
                INamedTypeSymbol classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
                if (classSymbol != null && !referencedSymbols.ContainsKey(classSymbol))
                {
                    referencedSymbols.Add(classSymbol, new HashSet<string>());
                }
            }

            var methodDeclarations = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (MethodDeclarationSyntax method in methodDeclarations)
            {
                IMethodSymbol methodSymbol = semanticModel.GetDeclaredSymbol(method);
                if (methodSymbol != null && !referencedSymbols.ContainsKey(methodSymbol))
                {
                    referencedSymbols.Add(methodSymbol, new HashSet<string>());
                }
            }

            var ctorDeclarations = root.DescendantNodes().OfType<ConstructorDeclarationSyntax>();
            foreach (ConstructorDeclarationSyntax ctor in ctorDeclarations)
            {
                IMethodSymbol ctorSymbol = semanticModel.GetDeclaredSymbol(ctor);
                if (ctorSymbol != null && !referencedSymbols.ContainsKey(ctorSymbol))
                {
                    referencedSymbols.Add(ctorSymbol, new HashSet<string>());
                }
            }
        }

        //Second pass to add in all the references
        foreach (SemanticModel semanticModel in semanticModelList)
        {
            SyntaxNode root = semanticModel.SyntaxTree.GetRoot();
            SymbolInfo info = semanticModel.GetSymbolInfo(root);
            if (info.Symbol != null && !referencedSymbols.ContainsKey(info.Symbol))
            {
                referencedSymbols.Add(info.Symbol, new HashSet<string>());
            }

            foreach (SyntaxNode syntaxNode in root.DescendantNodes())
            {
                ISymbol symbol = semanticModel.GetSymbolInfo(syntaxNode).Symbol;
                if (symbol == null || IsLocalFunction(symbol))
                {
                    continue;
                }

                if (!referencedSymbols.ContainsKey(symbol))
                {
                    referencedSymbols[symbol] = new HashSet<string>();
                }

                MethodDeclarationSyntax containingMethod = syntaxNode.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                ClassDeclarationSyntax containingType = syntaxNode.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();

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
                        referencedSymbols[symbol].Add(str);
                    }
                }
                else
                {
                    IMethodSymbol containingMethodSymbol = semanticModel.GetDeclaredSymbol(containingMethod);
                    referencedSymbols[symbol].Add(containingMethodSymbol.ToString());
                }
            }
        }

        if (writeToFile)
        {
            WriteToFile(referencedSymbols);
        }

        return referencedSymbols;
    }

    private bool IsLocalFunction(ISymbol symbol)
    {
        return symbol is IMethodSymbol methodSymbol && methodSymbol.MethodKind == MethodKind.LocalFunction;
    }

    private void WriteToFile(Dictionary<ISymbol, HashSet<string>> referencedSymbols)
    {
        StringBuilder output = new StringBuilder();
        foreach (var entry in referencedSymbols)
        {
            ISymbol symbol = entry.Key;
            output.AppendLine($"Element: {symbol} ({symbol.Kind})");
            output.AppendLine("Referenced in:");

            foreach (string reference in entry.Value)
            {
                output.AppendLine($"\t- {reference}");
            }
            output.AppendLine();
        }

        string outputFilePath = Path.Combine(Application.dataPath, "output.txt");
        File.WriteAllText(outputFilePath, output.ToString());
    }
}
