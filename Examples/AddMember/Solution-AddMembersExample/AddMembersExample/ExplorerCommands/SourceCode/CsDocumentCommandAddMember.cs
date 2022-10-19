using CodeFactory.DotNet.CSharp;
using CodeFactory.Logging;
using CodeFactory.VisualStudio;
using CodeFactory.VisualStudio.SolutionExplorer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AddMembersExample.Logic;
using CodeFactory;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using CodeFactory.Formatting.CSharp;

namespace AddMembersExample.ExplorerCommands.SourceCode
{
    /// <summary>
    /// Code factory command for automation of a C# document when selected from a project in solution explorer.
    /// </summary>
    public class CsDocumentCommandAddMember : CSharpSourceCommandBase
    {
        private static readonly string commandTitle = "Add Member";
        private static readonly string commandDescription = "Adds interface members that are missing from the target class that implements the interface.";

#pragma warning disable CS1998

        /// <inheritdoc />
        public CsDocumentCommandAddMember(ILogger logger, IVsActions vsActions) : base(logger, vsActions, commandTitle, commandDescription)
        {
            //Intentionally blank
        }

        #region Overrides of VsCommandBase<IVsCSharpDocument>

        /// <summary>
        /// Validation logic that will determine if this command should be enabled for execution.
        /// </summary>
        /// <param name="result">The target model data that will be used to determine if this command should be enabled.</param>
        /// <returns>Boolean flag that will tell code factory to enable this command or disable it.</returns>
        public override async Task<bool> EnableCommandAsync(VsCSharpSource result)
        {
            //Result that determines if the the command is enabled and visible in the context menu for execution.
            bool isEnabled = false;

            try
            {
                //Getting the hosting class from the source code file.
                var classData = result.SourceCode.Classes.FirstOrDefault();

                //Determine if a class was found. If not do not enable the command.
                isEnabled = classData != null;

                //If isEnabled check to make sure there are missing interface members. Otherwise do not display the command.
                if(isEnabled) isEnabled = classData.MissingInterfaceMembers().Any();

            }
            catch (Exception unhandledError)
            {
                _logger.Error($"The following unhandled error occured while checking if the solution explorer C# document command {commandTitle} is enabled. ",
                    unhandledError);
                isEnabled = false;
            }

            return isEnabled;
        }

        /// <summary>
        /// Code factory framework calls this method when the command has been executed. 
        /// </summary>
        /// <param name="result">The code factory model that has generated and provided to the command to process.</param>
        public override async Task ExecuteCommandAsync(VsCSharpSource result)
        {
            try
            {
                
                //Local variable to keep track of the source code in the file.
                var sourceCode = result.SourceCode;

                //Getting the implementation class in the source code.
                var classData = sourceCode?.Classes.FirstOrDefault();

                //If not class is found exit the command.
                if(classData == null) return;

                //Getting the missing interface members
                var missingMembers = classData.MissingInterfaceMembers();

                //If no members are missing then return nothing to automate.
                if(!missingMembers.Any()) return;

                
                //Checking to make sure the logger namespace has been added to the source code file. 
                if(!sourceCode.HasUsingStatement("Microsoft.Extensions.Logging"))
                {   // adding the missing using statement
                    sourceCode = await sourceCode.AddUsingStatementAsync("Microsoft.Extensions.Logging"); 

                    //Reloading the class data from the updated source code.
                    classData = sourceCode.GetModel(classData.LookupPath) as CsClass;
                }

                //User for formatting source code to be added to the source code file.
                var formatter = new SourceFormatter();

                //Checking to make sure there is a logger field that has been added to class.
                if (!classData.Fields.Any(f => f.Name == "_logger"))
                {
                    //Formatting the source code for the logger field to be added to the class.
                    formatter.AppendCodeLine(2,"/// <summary>");
                    formatter.AppendCodeLine(2,"/// Field that is used for all logging in the class. Must be initlized in the constructor.");
                    formatter.AppendCodeLine(2,"/// </summary>");
                    formatter.AppendCodeLine(2,"private readonly ILogger _logger;");
                    formatter.AppendCodeLine(2);

                    //Adding the field source code to the beginning of the class definition.
                    sourceCode = await classData.AddToBeginningAsync(formatter.ReturnSource());

                    //Reloading the class datas from the updated source code. 
                    classData = sourceCode.GetModel(classData.LookupPath) as CsClass;
                }

                //Checking all the target types for the missing members and making sure the using statements for the supporting types are added.
                sourceCode = await sourceCode.AddMissingNamespaces(missingMembers,classData.Namespace);

                //Reloading the class datas from the updated source code. 
                classData = sourceCode.GetModel(classData.LookupPath) as CsClass;

                //Loading all the target namespaces into the namespace manager so type definitions can be shortened.
                var manager = new NamespaceManager(sourceCode.NamespaceReferences,classData.Namespace);


                //Checking to see if the class has a constructor.
                if(!classData.Methods.Any(m => m.MethodType == CsMethodType.Constructor & m.Parameters.Any(p => p.ParameterType.Namespace == "Microsoft.Extensions.Logging" & p.ParameterType.Name == "ILogger" )))
                {
                    //Clearing the formatter
                    formatter.ResetFormatter();

                    //formatting a constructor that includes passing the logger instance to the class.
                    formatter.AppendCodeLine(2);
                    formatter.AppendCodeLine(2,"/// <summary>");
                    formatter.AppendCodeLine(2,$"/// Constructor that creates an instance of the <see cref=\"{classData.Name}\"/> class.");
                    formatter.AppendCodeLine(2,"/// </summary>");
                    formatter.AppendCodeLine(2,"/// <param name=\"logger\">The logger instance to handle logging for this class.</param>");
                    formatter.AppendCodeLine(2,$"public {classData.Name}(ILogger<{classData.Name}> logger)");
                    formatter.AppendCodeLine(2,"{");
                    formatter.AppendCodeLine(3,"_logger = logger;");
                    formatter.AppendCodeLine(2,"}");
                    formatter.AppendCodeLine(2);

                    //Writing the constructor code after the logger field.
                    sourceCode = await classData.AddToEndAsync(formatter.ReturnSource());

                    //Reloading the class data after the source code has been updated.
                    classData = sourceCode.GetModel(classData.LookupPath) as CsClass;

                }
                    


                //Loop through each missing member and add it to the class.
                foreach (var missingMember in missingMembers)
                {
                    //Switching on the type of member that is missing
                    switch (missingMember.MemberType)
                    {
                        case CsMemberType.Event:
                            break;

                        case CsMemberType.Method:

                            var methodData = missingMember as CsMethod;

                            var formattedSource = methodData.FormatMethodSyntax(manager);

                            if(string.IsNullOrEmpty(formattedSource)) continue;

                            formatter.ResetFormatter();
                            formatter.AppendCodeBlock(2,formattedSource);

                            sourceCode = await classData.AddToEndAsync(formatter.ReturnSource());

                            classData = sourceCode.GetModel(classData.LookupPath) as CsClass;

                            break;

                        case CsMemberType.Property:
                            break;

                        default:
                            break;
                    }
                }

            }
            catch (Exception unhandledError)
            {
                _logger.Error($"The following unhandled error occured while executing the solution explorer C# document command {commandTitle}. ",
                    unhandledError);

            }

        }

        #endregion
    }
}
