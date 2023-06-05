using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SuperFluid.Public.SourceGeneration;
using System.Diagnostics;
namespace SuperFluid.Internal.SourceGeneration;

public class FluentApiDefinitionSyntaxReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> ApiDefinitions = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax classDeclaration)
        {
            foreach (var attributeList in classDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    Debug.WriteLine(attribute.Name.ToString());
                    if (attribute.Name.ToString() == nameof(FluentApiDefinitionAttribute))
                    {
                        ApiDefinitions.Add(classDeclaration);
                    }
                }
            }
        }
    }
}