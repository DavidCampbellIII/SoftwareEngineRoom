using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ReferenceData
{
    public ISymbol symbol { get; }
    public SyntaxNode syntaxNode { get; }

    public ReferenceData(ISymbol symbol, SyntaxNode syntaxNode)
    {
        this.symbol = symbol;
        this.syntaxNode = syntaxNode;
    }
}
