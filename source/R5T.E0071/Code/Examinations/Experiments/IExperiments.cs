using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

using R5T.T0141;
using R5T.T0162;
using R5T.T0172.Extensions;
using R5T.T0181;
using R5T.T0212;
using R5T.T0212.F000;


namespace R5T.E0071
{
    [ExperimentsMarker]
    public partial interface IExperiments : IExperimentsMarker
    {
        /// <summary>
        /// Starting with a framework's packs directory path (example: C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\6.0.18\ref\net6.0),
        /// get all documentation file paths in the directory.
        /// This is assumed to just be all XML file paths.
        /// </summary>
        public void GetAllDotnetFrameworkDocumentationFilePaths()
        {
           
        }

        public async Task GetAllDocumentationFilePaths()
        {
            // Given a starting project file path, get all recursive project reference file paths. (Use R5T.O0006)
            // For each file path, get the documentation file path. (Do this using din directory, with Debug configuration, and net6.0 target framework.) (Use F0040, F0052, F0115)
            // Load all documentation files into a single member documentations-by-identity name.

            /// Inputs.
            var targetProjectFilePath = @"C:\Code\DEV\Git\GitHub\SafetyCone\R5T.E0071\source\R5T.E0071\R5T.E0071.csproj"
                .ToProjectFilePath();
            var outputFilePath = Instances.FilePaths.OutputTextFilePath;


            /// Run.
            var allRecursiveProjectFilePaths = await Instances.ProjectFileOperations.Get_RecursiveProjectReferences(
                targetProjectFilePath);

            var projectDocumentationTargetsByDocumentationFilePaths = allRecursiveProjectFilePaths
                .Select(projectFilePath =>
                {
                    var documentationFilePath = Instances.ProjectPathsOperator.GetDocumentationFilePathForProjectFilePath(projectFilePath.Value)
                        .ToDocumentationXmlFilePath();

                    var projectDocumentationTarget = new ProjectDocumentationTarget
                    {
                        ProjectFilePath = projectFilePath,
                    };

                    return (documentationFilePath, projectDocumentationTarget);
                })
                .ToDictionary(
                    x => x.documentationFilePath,
                    x => x.projectDocumentationTarget);

            // Find documentation files that do not exist.
            foreach (var pair in projectDocumentationTargetsByDocumentationFilePaths)
            {
                var documentationFilePath = pair.Key;

                var exists = Instances.FileSystemOperator.FileExists(documentationFilePath.Value);
                if(!exists)
                {
                    Console.WriteLine(documentationFilePath);
                }
            }

            var memberDocumentationsByIdentityName = Instances.MemberDocumentationOperator.Get_InitialMemberDocumentationByIdentityName();

            foreach (var pair in projectDocumentationTargetsByDocumentationFilePaths)
            {
                var documentationFilePath = pair.Key;
                var documentationTarget = pair.Value;

                await Instances.DocumentationFileOperator.Add_MemberDocumentationsByIdentityName(
                    documentationFilePath,
                    memberDocumentationsByIdentityName,
                    documentationTarget);
            }

            //var lines = memberDocumentationsByIdentityName.Values
            //    .Select(Instances.MemberDocumentationOperator.Describe)
            //    ;

            //Instances.NotepadPlusPlusOperator.WriteLinesAndOpen(
            //    outputFilePath,
            //    lines);



            var lines = memberDocumentationsByIdentityName.Values
                .Select(Instances.MemberDocumentationOperator.Describe)
                ;

            Instances.NotepadPlusPlusOperator.WriteLinesAndOpen(
                outputFilePath,
                lines);
        }

        /// <summary>
        /// See R5T.S0082.IDocumentationCommentOperations.
        /// </summary>
        public void ConvertInheritdocElements()
        {
            /// Inputs.
            var documentationElement = Instances.DocumentationElements.R5T_T0211;
            var outputFilePath = Instances.FilePaths.OutputTextFilePath;


            /// Run.
            var documentationTarget = new NoneDocumentationTarget
            {
                Note = "Using documentation element."
            };

            var memberDocumentationsByIdentityName = Instances.DocumentationElementOperator.Get_MemberDocumentationsByIdentityName(
                documentationElement,
                documentationTarget);

            var processedDocumentationsByIdentityName = new Dictionary<IIdentityName, MemberDocumentation>();

            var missingNames = new HashSet<IIdentityName>();

            static void ProcessMemberDocumentation(
                MemberDocumentation memberDocumentation,
                IDictionary<IIdentityName, MemberDocumentation> memberDocumentationsByIdentityName,
                IDictionary<IIdentityName, MemberDocumentation> processedDocumentationsByIdentityName,
                HashSet<IIdentityName> missingNames)
            {
                // Short-circuit if the member documentation has already been processed.
                if(processedDocumentationsByIdentityName.ContainsKey(memberDocumentation.IdentityName))
                {
                    return;
                }

                // Else, process all inheritdoc elements.
                var inheritdocElements = Instances.XElementOperator.Get_Children(
                    memberDocumentation.MemberElement.Value,
                    Instances.XmlDocumentationCommentElementNames.Inheritdoc)
                    .Now();

                foreach (var inheritdocElement in inheritdocElements)
                {
                    var cref = Instances.InheritdocElementOperator.Get_Cref(inheritdocElement);
                    var hasPath = Instances.InheritdocElementOperator.Has_Path(inheritdocElement);

                    var nameAlreadyProcessed = processedDocumentationsByIdentityName.ContainsKey(cref);
                    if(!nameAlreadyProcessed)
                    {
                        var nameIsAvailable = memberDocumentationsByIdentityName.ContainsKey(cref);
                        if(!nameIsAvailable)
                        {
                            // Note the missing name.
                            missingNames.Add(cref);

                            continue;
                        }

                        // Recurse.
                        var memberDocumentationForName = memberDocumentationsByIdentityName[cref];

                        ProcessMemberDocumentation(
                            memberDocumentationForName,
                            memberDocumentationsByIdentityName,
                            processedDocumentationsByIdentityName,
                            missingNames);

                        // Now the name will be available.
                    }

                    // Name is available, or we have continued.
                    var processedDocumentationForName = processedDocumentationsByIdentityName[cref];

                    if(hasPath)
                    {
                        // Assume path could select multiple elements.
                        var elements = processedDocumentationForName.MemberElement.Value.XPathSelectElements(
                            "." + hasPath.Result.Value)
                            .ToArray();
                        //;

                        inheritdocElement.ReplaceWith(
                            elements);
                    }
                    else
                    {
                        var replacementNodes = Instances.XElementOperator.Get_Nodes_ExceptLeadingAndTrailingWhitespaceNodes(
                            processedDocumentationForName.MemberElement.Value)
                            .Now();

                        inheritdocElement.ReplaceWith(
                        //processedDocumentationForName.MemberElement.Value.Nodes()
                        //    // Don't include the new lines included just for formatting.
                        //    .Where(xNode =>
                        //    {
                        //        if(xNode is XText textNode)
                        //        {
                        //            var value = textNode.Value;

                        //            var trimmedIsEmpty = value.Trim() == String.Empty;
                        //            if(trimmedIsEmpty)
                        //            {
                        //                return false;
                        //            }
                        //        }

                        //        return true;
                        //    })
                        replacementNodes
                        );
                    }
                }

                // Cleanup extra ending blank lines that occur if there is trailing XML text outside of an XML documentation element.
                var lastNode = memberDocumentation.MemberElement.Value.LastNode;

                var lastNodeIsNewLine = lastNode is XText textNode && textNode.Value.Trim() == System.String.Empty;
                if(lastNodeIsNewLine)
                {
                    var priorNode = lastNode.PreviousNode;
                    if(priorNode is XText priorTextNode && priorTextNode.Value.TrimEnd('\n') != priorTextNode.Value)
                    {
                        lastNode.Remove();
                    }
                }

                processedDocumentationsByIdentityName.Add(
                    memberDocumentation.IdentityName,
                    memberDocumentation);
            }

            foreach (var pair in memberDocumentationsByIdentityName)
            {
                ProcessMemberDocumentation(
                    pair.Value,
                    memberDocumentationsByIdentityName,
                    processedDocumentationsByIdentityName,
                    missingNames);
            }

            var lines = processedDocumentationsByIdentityName
                .Select(pair =>
                {
                    //var prettyPrinted = pair.Value.MemberElement.Value.ToString();

                    //var xmlDocumentationComment = Instances.MemberElementOperator.Get_XmlDocumentationComment(pair.Value.MemberElement);

                    //return $"{pair.Key}:\n{xmlDocumentationComment}\n";

                    //// Pretty print.
                    //var tempXDocument = Instances.XDocumentOperator.Parse(xmlDocumentationComment);

                    //var prettyPrinted = Instances.XDocumentOperator.WriteToString(tempXDocument);

                    //return $"{pair.Key}:\n{prettyPrinted}\n";

                    //var prettyPrinted = pair.Value.MemberElement.Value.ToString();

                    //var element = pair.Value.MemberElement.Value;

                    //var textNodes = element.DescendantNodes()
                    //    .OfType<XText>()
                    //    .Now()
                    //    ;

                    //foreach (var textNode in textNodes)
                    //{
                    //    var value = textNode.Value;

                    //    var newValue = value
                    //    //.Replace("\n        ", "\n")
                    //    .Replace("\n            ", "\n")
                    //    ;

                    //    textNode.Value = newValue;
                    //}

                    ////var prettyPrinted = element.ToString();

                    //var interimText = element.ToString();

                    //var interimElement = XElement.Parse(
                    //    interimText,
                    //    LoadOptions.None);

                    //var prettyPrinted = interimElement.ToString();

                    //var stringBuilder = new StringBuilder();

                    //var xmlWriterSettings = new XmlWriterSettings
                    //{
                    //    //Indent = true,
                    //    OmitXmlDeclaration = true,
                    //    ConformanceLevel = ConformanceLevel.Fragment
                    //    //NewLineHandling = NewLineHandling.Replace,
                    //    // Cannot set.
                    //    //OutputMethod = XmlOutputMethod.Text,
                    //};

                    //using (var xmlWriter = XmlWriter.Create(
                    //    stringBuilder,
                    //    xmlWriterSettings))
                    //{
                    //    //element.WriteTo(xmlWriter);
                    //    foreach (var node in element.Nodes())
                    //    {
                    //        node.WriteTo(xmlWriter);
                    //    }
                    //}

                    //var prettyPrinted = stringBuilder.ToString();

                    //var prettyPrinted = Instances.XElementOperator.Get_InnerXml(pair.Value.MemberElement.Value);

                    //var stringBuilder = new StringBuilder();

                    //foreach (var node in pair.Value.MemberElement.Value.Nodes())
                    //{
                    //    if (node is XText textNode)
                    //    {
                    //        var value = textNode.Value;

                    //        var trimmedIsEmpty = value.Trim() == String.Empty;
                    //        if (trimmedIsEmpty)
                    //        {
                    //            continue;
                    //        }
                    //    }

                    //    stringBuilder.Append(node.ToString());
                    //}
                    //var nodes = Instances.XElementOperator.Get_Nodes_ExceptLeadingAndTrailingWhitespaceNodes(
                    //    pair.Value.MemberElement.Value)
                    //    .Now();

                    //// Fix the last node for readout.
                    //var lastNode = nodes.Last();
                    //if (lastNode is XText textNode)
                    //{
                    //    textNode.Value = textNode.Value.TrimEnd('\n');
                    //}

                    ////var nodes = pair.Value.MemberElement.Value.Nodes().Now();
                    //foreach (var node in nodes)
                    //{
                    //    stringBuilder.Append(node.ToString());
                    //}

                    //var prettyPrinted = stringBuilder.ToString();

                    //return $"{pair.Key}:\n{prettyPrinted}\n";

                    //var tempNode = new XElement("XmlDocumentationComment");
                    //tempNode.Add(nodes);
                    //tempNode.AddFirst(new XText("\n"));

                    //var xTextEnd = new XText("\n");

                    //tempNode.LastNode.ReplaceWith(
                    //    tempNode.LastNode,
                    //    xTextEnd);

                    ////return $"{pair.Key}:\n{tempNode}\n";

                    //var stringBuilder2 = new StringBuilder();

                    //var xmlWriterSettings = new XmlWriterSettings
                    //{
                    //    //Indent = true,
                    //    OmitXmlDeclaration = true,
                    //    //ConformanceLevel = ConformanceLevel.Fragment
                    //    //NewLineHandling = NewLineHandling.Replace,
                    //    // Cannot set.
                    //    //OutputMethod = XmlOutputMethod.Text,
                    //};

                    //using (var xmlWriter = XmlWriter.Create(
                    //    stringBuilder2,
                    //    xmlWriterSettings))
                    //{
                    //    tempNode.WriteTo(xmlWriter);
                    //}

                    //var tempText = stringBuilder2.ToString();

                    //return $"{pair.Key}:\n{tempText}\n";

                    //var tempNode2 = XElement.Parse(tempText);

                    //return $"{pair.Key}:\n{tempNode2}\n";

                    //// Fix the last node for readout.
                    //var lastNode = pair.Value.MemberElement.Value.Nodes().Last();
                    //if (lastNode is XText textNode)
                    //{
                    //    textNode.Value = textNode.Value.TrimEnd('\n');
                    //}

                    //var stringBuilder = new StringBuilder();

                    //var xmlWriterSettings = new XmlWriterSettings
                    //{
                    //    OmitXmlDeclaration = true,
                    //};

                    //using (var xmlWriter = XmlWriter.Create(
                    //    stringBuilder,
                    //    xmlWriterSettings))
                    //{
                    //    pair.Value.MemberElement.Value.WriteTo(xmlWriter);
                    //}

                    //var text = stringBuilder.ToString();

                    //return $"{pair.Key}:\n{text}\n";

                    var output = Instances.MemberDocumentationOperator.Describe(pair.Value);
                    return output;
                });

            Instances.NotepadPlusPlusOperator.WriteLinesAndOpen(
                outputFilePath,
                lines);
        }
    }
}
