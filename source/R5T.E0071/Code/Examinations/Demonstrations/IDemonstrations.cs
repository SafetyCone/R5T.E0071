using System;
using System.Linq;

using R5T.T0141;
using R5T.T0212.F000;


namespace R5T.E0071
{
    [DemonstrationsMarker]
    public partial interface IDemonstrations : IDemonstrationsMarker
    {
        public void Get_MemberDocumentationsByIdentityName()
        {
            /// Inputs.
            var documentationElement =
                Instances.DocumentationElements.R5T_T0211
                ;
            var outputFilePath = Instances.FilePaths.OutputTextFilePath;


            /// Run.
            var documentationTarget = new NoneDocumentationTarget();
            
            var memberDocumentationsByIdentityName = Instances.DocumentationElementOperator.Get_MemberDocumentationsByIdentityName(
                documentationElement,
                documentationTarget);

            var lines = memberDocumentationsByIdentityName
                .Select(pair =>
                {
                    //var xmlDocumentationComment = Instances.MemberElementOperator.Get_XmlDocumentationComment(pair.Value.MemberElement);

                    //return $"{pair.Key}:\n{xmlDocumentationComment}\n";

                    var output = pair.Value.MemberElement.Value.ToString();
                    return $"{pair.Key}:\n{output}\n";
                });

            Instances.NotepadPlusPlusOperator.WriteLinesAndOpen(
                outputFilePath,
                lines);
        }

        public void Get_AssemblyName()
        {
            /// Inputs.
            var documentationElement =
                Instances.DocumentationElements.R5T_T0211
                ;


            /// Run.
            var assemblyName = Instances.DocumentationElementOperator.Get_AssemblyName(
                documentationElement);

            Console.WriteLine($"{assemblyName}: assembly name.");
        }
    }
}
