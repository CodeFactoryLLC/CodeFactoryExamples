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

                            var formattedSource = methodData.FormatMethodSyntax();

                            if(string.IsNullOrEmpty(formattedSource)) continue;

                            var formatter = new SourceFormatter();
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
