using CodeFactory;
using CodeFactory.DotNet.CSharp;
using CodeFactory.Formatting.CSharp;
using System.Linq;

namespace AddMembersExample.Logic
{
    /// <summary>
    /// Extension methods that help format member models into C# implementation syntax.
    /// </summary>
    internal static class MemberFormatExtensions
    {
        /// <summary>
        /// Formats a event definition as standard c# sytnax.
        /// </summary>
        /// <param name="source">The source event model to generate the source. </param>
        /// <param name="manager">The namespace manager used to format namespaces on type definitions.</param>
        /// <returns>The fully formatted syntax for the event.</returns>
        public static string FormatEventSyntax(this CsEvent source, NamespaceManager manager)
        { 
            //If no source is found then we return an empty event format.
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
                    formatter.AppendCodeLine(0,attributeData.CSharpFormatAttributeSignature(manager));
                }    
            }

            //Formatting the event declaration.
            formatter.AppendCodeLine(0,source.CSharpFormatEventDeclaration(manager));
            formatter.AppendCodeLine(0);

            return formatter.ReturnSource();

        }

        /// <summary>
        /// Formats a property definition using standard property get and set syntax.
        /// </summary>
        /// <param name="source">The source property model to generate the source. </param>
        /// <param name="manager">The namespace manager used to format namespaces on type definitions.</param>
        /// <returns>The fully formatted syntax for the property.</returns>
        public static string FormatPropertySyntax(this CsProperty source, NamespaceManager manager)
        {
            //If no source is found then we return an empty property format.
            if(source == null) return null;
            
            //The source formatter formats the source code that will be emitted to the class.
            SourceFormatter formatter = new SourceFormatter();


            //Formatting the backing field for the property.
            var fieldName  = $"_{source.Name.ConvertToCamelCase()}";

            formatter.AppendCodeLine(0,"/// <summary>");
            formatter.AppendCodeLine(0,$"/// Backing field for the property '{source.Name}'");
            formatter.AppendCodeLine(0,"/// </summary>");
            formatter.AppendCodeLine(0,$"private {source.PropertyType.CSharpFormatTypeName(manager)} {fieldName};");
            formatter.AppendCodeLine(0);

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
                    formatter.AppendCodeLine(0,attributeData.CSharpFormatAttributeSignature(manager));
                }    
            }

            ////String builder to store the get and set accessors for the property
            //StringBuilder getSetSignature  = new StringBuilder();

            ////Adding the get key word if the property supports getting the property.
            //if(source.HasGet) getSetSignature.Append("get; ");       
            
            ////Adding the set key word and setting its access based up if is defined in the property definition.
            //getSetSignature.Append( source.HasSet ? "set;" : "private set;");

            //formatter.AppendCodeLine(0,$"public {source.PropertyType.CSharpFormatTypeName(manager)} {source.Name} {{ {getSetSignature.ToString()} }}");
            formatter.AppendCodeLine(0,source.CSharpFormatDefaultPropertySignatureWithBackingField(fieldName,manager));
            formatter.AppendCodeLine(0);

            return formatter.ReturnSource();
        }


        /// <summary>
        /// Formats a method with logging, bounds checking, and error handling managed in a try catch block
        /// </summary>
        /// <param name="source">The source method model to generate the source </param>
        /// <param name="manager">The namespace manager used to format namespaces on type definitions.</param>
        /// <returns>The fully formatted syntax for the method</returns>
        public static string FormatMethodSyntax(this CsMethod source,NamespaceManager manager)
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
                    formatter.AppendCodeLine(0,attributeData.CSharpFormatAttributeSignature(manager));
                }    
            }

            //Generate the method signature and add it to the formatter.
            formatter.AppendCodeLine(0,source.CSharpFormatStandardMethodSignatureWithAsync(manager));

            //Adding the statement start for the method.
            formatter.AppendCodeLine(0,"{");

            //Adding entry point logging for the method.
            formatter.AppendCodeLine(1,$"_logger.LogInformation($\"Entering Method {{nameof({source.Name})}}\");");
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
                        formatter.AppendCodeLine(1,$"if(string.IsNullOrEmpty({parmData.Name}))");
                        formatter.AppendCodeLine(1,"{");
                        formatter.AppendCodeLine(2,$"_logger.LogError($\"The parameter '{{nameof({parmData.Name})}}' was not provided raising \");");
                        formatter.AppendCodeLine(2,$"_logger.LogInformation($\"Exiting Method {{nameof({source.Name})}}\");");
                        formatter.AppendCodeLine(2,$"throw new ArgumentException(\"The required argument was missing data\",nameof({parmData.Name}));");
                        formatter.AppendCodeLine(1,"}");
                        formatter.AppendCodeLine(1);
                    }
                    //If the parameter is not a string then check for null condition.
                    else
                    { 
                        formatter.AppendCodeLine(1,$"if({parmData.Name} == null)");
                        formatter.AppendCodeLine(1,"{");
                        formatter.AppendCodeLine(2,$"_logger.LogError($\"The parameter '{{nameof({parmData.Name})}}' was not provided raising \");");
                        formatter.AppendCodeLine(2,$"_logger.LogInformation($\"Exiting Method {{nameof({source.Name})}}\");");
                        formatter.AppendCodeLine(2,$"throw new ArgumentNullException(\"The required argument was missing data\",nameof({parmData.Name}));");
                        formatter.AppendCodeLine(1,"}");
                        formatter.AppendCodeLine(1);
                    }
                }
            }

            // Flag used to determing if the method has a return type to return.
            bool hasReturnType = false;
            CsType returnType = source.ReturnType;

            // Determing if the method has a return type.
            if(returnType != null)
            {
                //Checking to see if the return type is a task if so format for the correct return type.
                if(source.ReturnType.Namespace == "System.Threading.Tasks" & source.ReturnType.Name == "Task")
                {
                    //If the task is a generic then extract the target type and set that as the return type.
                    if(source.ReturnType.IsGeneric)
                    {
                        hasReturnType = true;

                        returnType = source.ReturnType.GenericTypes.FirstOrDefault();


                        // If we cannot find the return type then set the return type to false.
                        if(returnType == null) 
                        {
                            hasReturnType = false; 
                            returnType = null;
                        }

                    }
                    else
                    {
                        //Is strictly a Task type. No return statement is needed for the async method.
                        returnType = null;
                    }
                }
            }

            //Confirming that a return type is still set after checking the task.
            if(returnType != null)
            {

                //Format the result variable to support a default value being set if supported.
                if(returnType.IsValueType)
                {
                    hasReturnType = true;
                    formatter.AppendCodeLine(1,string.IsNullOrEmpty(returnType.ValueTypeDefaultValue) ? $"{returnType.CSharpFormatTypeName(manager)} result;" : $"{returnType.CSharpFormatTypeName(manager)} result = {returnType.ValueTypeDefaultValue};"); 
                    formatter.AppendCodeLine(1);
                }
                //Format the result variable and set its initial value to null.
                else
                {
                    hasReturnType = true;
                    formatter.AppendCodeLine(1,$"{returnType.CSharpFormatTypeName(manager)} result = null;");     
                    formatter.AppendCodeLine(1);
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
            formatter.AppendCodeLine(2,$"_logger.LogInformation($\"Exiting Method {{nameof({source.Name})}}\");");
            formatter.AppendCodeLine(2,"throw unhandledError;");
            formatter.AppendCodeLine(1,"}");
            formatter.AppendCodeLine(1);

            formatter.AppendCodeLine(1,$"_logger.LogInformation($\"Exiting Method {{nameof({source.Name})}}\");");
            formatter.AppendCodeLine(1);
            
            if(hasReturnType)
            { 
                formatter.AppendCodeLine(1,"return result;" );
                formatter.AppendCodeLine(1);
            }
            

            //Adding the statement end for the method.
            formatter.AppendCodeLine(0,"}");
            formatter.AppendCodeLine(0);

            //Returning the full formatted source code.
            return formatter.ReturnSource();
        }

        /// <summary>
        /// Converts a string to camel case format.
        /// </summary>
        /// <param name="source">string for formath the data.</param>
        /// <returns>Formatted camel case format or null if no string is provided.</returns>
        public static string ConvertToCamelCase(this string source)
        { 
            if(string.IsNullOrEmpty(source)) return null;
            
            return source.Length == 1 ? $"{source.Substring(0,1).ToLower()}" : $"{source.Substring(0,1).ToLower()}{source.Substring(1)}";
        }
    }
}
