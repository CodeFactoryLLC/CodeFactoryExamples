using CodeFactory;
using CodeFactory.DotNet.CSharp;
using CodeFactory.Formatting.CSharp;
namespace AddMembersExample.Logic
{
    /// <summary>
    /// Extension methods that help format member models into C# implementation syntax.
    /// </summary>
    internal static class MemberFormatExtensions
    {
        /// <summary>
        /// Formats a method with logging, bounds checking, and error handling managed in a try catch block
        /// </summary>
        /// <param name="source">The source method model to generate the source </param>
        /// <returns>The fully formatted syntax for the method</returns>
        public static string FormatMethodSyntax(this CsMethod source)
        { 
            //If no source is found then we return an empty method format.
            if(source == null) return null;
            
            //The source formatter formats the source code that will be emitted to the class.
            SourceFormatter formatter = new SourceFormatter();

            //Checking if the method has xml documentation.
            if(source.HasDocumentation)
            {
                //Iterate over all the xml docs
                foreach (var xmlDoc in source.CSharpFormatXmlDocumentationEnumerator())
                {
                    //Write the docs to the formatter.
                    formatter.AppendCodeLine(0,xmlDoc);
                }        
            }

            //If the method has attributes asigned to make sure they get included in the signature. 
            if(source.HasAttributes)
            {
                //Iterate over the attributes
                foreach (var attributeData in source.Attributes)
                {
                    //Get the C# attribute for each attribute and append it to the formatter output.
                    formatter.AppendCodeLine(0,attributeData.CSharpFormatAttributeSignature());
                }    
            }

            //Generate the method signature and add it to the formatter.
            formatter.AppendCodeLine(0,source.CSharpFormatStandardMethodSignatureWithAsync());

            //Adding the statement start for the method.
            formatter.AppendCodeLine(0,"{");

            //Adding entry point logging for the method.
            formatter.AppendCodeLine(1,$"_logger.LogInformation(\"Entering Method {source.Name}\");");
            formatter.AppendCodeLine(1);

            //Checking the method to see if has parameters. If so bounds check the parameters.
            if(source.HasParameters)
            {
                foreach (var parmData in source.Parameters)
                {   
                    //If the parameter has a default value then we dont bounds check it.
                    if(parmData.HasDefaultValue) continue;

                    //If the parameter is a value type then we dont bounds check it.
                    if(parmData.ParameterType.IsValueType) continue;

                    //If the paramter is a string type then bounds check for empty or null
                    if(parmData.ParameterType.IsWellKnownType & parmData.ParameterType.WellKnownType == CsKnownLanguageType.String)
                    {
                        formatter.AppendCodeLine(2,$"if(string.IsNullOrEmpty({parmData.Name}))");
                        formatter.AppendCodeLine(2,"{");
                        formatter.AppendCodeLine(3,$"_logger.LogError($\"The parameter '{{nameof({parmData.Name})}}' was not provided raising \");");
                        formatter.AppendCodeLine(3,$"_logger.LogInformation(\"Exiting Method {source.Name}\")");
                        formatter.AppendCodeLine(3,$"throw new ArgumentException(\"The required argument was missing data\",nameof({parmData.Name}));");
                        formatter.AppendCodeLine(2,"}");
                        formatter.AppendCodeLine(2);
                    }
                    //If the parameter is not a string then check for null condition.
                    else
                    { 
                        formatter.AppendCodeLine(2,$"if({parmData.Name} == null)");
                        formatter.AppendCodeLine(2,"{");
                        formatter.AppendCodeLine(3,$"_logger.LogError($\"The parameter '{{nameof({parmData.Name})}}' was not provided raising \");");
                        formatter.AppendCodeLine(3,$"_logger.LogInformation(\"Exiting Method {source.Name}\");");
                        formatter.AppendCodeLine(3,$"throw new ArgumentNullException(\"The required argument was missing data\",nameof({parmData.Name}));");
                        formatter.AppendCodeLine(2,"}");
                        formatter.AppendCodeLine(2);
                    }
                }
            }

            //Adding try and catch blocks for the method.
            formatter.AppendCodeLine(1,"try");
            formatter.AppendCodeLine(1,"{");
            formatter.AppendCodeLine(1);
            formatter.AppendCodeLine(1,"}");
            formatter.AppendCodeLine(1,"catch (Exception unhandledError)");
            formatter.AppendCodeLine(1,"{");
            formatter.AppendCodeLine(2,$"_logger.LogError(unhandledError, $\"An unhandled error occured in {source.Name}, see exception for details.\");");
            formatter.AppendCodeLine(2,$"_logger.LogInformation(\"Exiting Method {source.Name}\");");
            formatter.AppendCodeLine(2,"throw unhandledError;");
            formatter.AppendCodeLine(1,"}");
            formatter.AppendCodeLine(1);

            formatter.AppendCodeLine(1,$"_logger.LogInformation(\"Exiting Method {source.Name}\");");
            formatter.AppendCodeLine(1);

            //Adding the statement end for the method.
            formatter.AppendCodeLine(0,"}");
            formatter.AppendCodeLine(0);

            //Returning the full formatted source code.
            return formatter.ReturnSource();
        }
    }
}
