using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace GenRules
{
    class Program
    {
        static void Main(string[] args)
        {
            var assembly = Assembly.LoadFile(new FileInfo(@".\IFS2.Equipment.TicketingRules.CommonRules.dll").FullName);
            
            var allPurpose = assembly.GetType(typeof(IFS2.Equipment.TicketingRules.Rules.AllPurpose).ToString());
            var methods = allPurpose.GetMethods(BindingFlags.Static | BindingFlags.Public);//.Select(x=>String.Format("{0}.{1}.{2}", allPurpose.Namespace, allPurpose.Name, x.Name)));
            var validationMethods = methods.Where(x => x.ReturnType == typeof(IFS2.Equipment.Common.TTErrorTypes));
            var updateMethods = methods.Except(validationMethods);

            List<Type> types = assembly.GetTypes().ToList();            

            List<InvocationExpressionSyntax> expressions = new List<InvocationExpressionSyntax>();
            if (validationMethods.Count() > 0)
                expressions.Add(GetInvocationExpression("AddValidateRule_All", validationMethods.Select(x=> Normalize(x.DeclaringType.FullName) + "." + x.Name)));
            if (updateMethods.Count() > 0)
                expressions.Add(GetInvocationExpression("AddUpdateAction_All", updateMethods.Select(x => Normalize(x.DeclaringType.FullName) + "." + x.Name)));
            
            var treatments = Enum.GetNames(typeof(IFS2.Equipment.Common.MediaDetectionTreatment));
            List<int> fams = new List<int> { 10, 20, 40, 60, 80, 100 };
            
            foreach(var treatment in treatments)
            {
                Type typ;
                
                typ = assembly.GetType($"IFS2.Equipment.TicketingRules.Rules.{treatment}.AllFamilies");
                if (typ != null)
                {
                    methods = typ.GetMethods(BindingFlags.Static | BindingFlags.Public);
                    validationMethods = methods.Where(x => x.ReturnType == typeof(IFS2.Equipment.Common.TTErrorTypes));
                    updateMethods = methods.Except(validationMethods);

                    IEnumerable<string> pars = new List<string> { typeof(IFS2.Equipment.Common.MediaDetectionTreatment).Name + "." + treatment };
                    if (validationMethods.Count() > 0)
                        expressions.Add(GetInvocationExpression("AddValidateRule_ATreatmentType_AllFamilies", pars.Concat(validationMethods.Select(x => Normalize(x.DeclaringType.FullName) + "." + x.Name))));
                    if (updateMethods.Count() > 0)
                        expressions.Add(GetInvocationExpression("AddUpdateAction_ATreatmentType_AllFamilies", pars.Concat(updateMethods.Select(x => Normalize(x.DeclaringType.FullName) + "." + x.Name))));
                }

                foreach (int fam in fams)
                {
                    string f = "Fam" + fam.ToString("D3");
                    typ = assembly.GetType($"IFS2.Equipment.TicketingRules.Rules.{treatment}.{f}.AllModes");
                    if (typ != null)
                    {
                        methods = typ.GetMethods(BindingFlags.Static | BindingFlags.Public);
                        validationMethods = methods.Where(x => x.ReturnType == typeof(IFS2.Equipment.Common.TTErrorTypes));
                        updateMethods = methods.Except(validationMethods);

                        IEnumerable<string> pars = new List<string> { typeof(IFS2.Equipment.Common.MediaDetectionTreatment).Name + "." + treatment, fam.ToString() };
                        if (validationMethods.Count() > 0)                            
                            expressions.Add(GetInvocationExpression("AddValidateRule_ATreatmentType_AFamily_AllFareModes", pars.Concat(validationMethods.Select(x => Normalize(x.DeclaringType.FullName) + "." + x.Name))));                            
                        
                        if (updateMethods.Count() > 0)                            
                            expressions.Add(GetInvocationExpression("AddUpdateAction_ATreatmentType_AFamily_AllFareModes", pars.Concat(updateMethods.Select(x => Normalize(x.DeclaringType.FullName) + "." + x.Name))));
                    }

                    foreach(IFS2.Equipment.TicketingRules.FareMode mode in Enum.GetValues(typeof(IFS2.Equipment.TicketingRules.FareMode)))
                    {
                        typ = assembly.GetType($"IFS2.Equipment.TicketingRules.Rules.{treatment}.{f}.{mode.ToString()}");
                        if (typ != null)
                        {
                            methods = typ.GetMethods(BindingFlags.Static | BindingFlags.Public);
                            validationMethods = methods.Where(x => x.ReturnType == typeof(IFS2.Equipment.Common.TTErrorTypes));
                            updateMethods = methods.Except(validationMethods);
                            List<string> pars = new List<string> { typeof(IFS2.Equipment.Common.MediaDetectionTreatment).Name + "." + treatment,
                                    fam.ToString(),
                                    $"{typeof(IFS2.Equipment.TicketingRules.FareMode).Name}.{mode.ToString()}"
                                };                            

                            if (validationMethods.Count() > 0)                            
                                expressions.Add(GetInvocationExpression("AddValidateRule_ATreatmemntType_AFamily_AFareMode", pars.Concat(validationMethods.Select(x => Normalize(x.DeclaringType.FullName) + "." + x.Name))));

                            if (updateMethods.Count() > 0)
                                expressions.Add(GetInvocationExpression("AddUpdateAction_ATreatmemntType_AFamily_AFareMode", pars.Concat(updateMethods.Select(x => Normalize(x.DeclaringType.FullName) + "." + x.Name))));
                        }
                    }
                }
            }

            ConstructorDeclarationSyntax ctor = SF.ConstructorDeclaration("ValidationRules").WithBody(
                SF.Block(
                    expressions.Select(x=>SF.ExpressionStatement(x)
                        )
                )).AddModifiers(SF.Token(SyntaxKind.StaticKeyword));            
            
            var sf = SF.CompilationUnit();
            sf = sf.AddMembers(ctor);
            
            File.WriteAllText(@"d:\junk\ctor.txt", sf.NormalizeWhitespace().ToFullString());            
        }

        private static InvocationExpressionSyntax GetInvocationExpression(string fnName, IEnumerable<string> pars)
        {
            return SF.InvocationExpression(SF.ParseExpression(fnName),
                    SF.ArgumentList(SF.SeparatedList(
                        pars.Select(x => SF.Argument(SF.ParseExpression(x))))));
        }

        private static string Normalize(string fullName)
        {
            string x = "IFS2.Equipment.TicketingRules.";
            if (fullName.StartsWith(x))
                return fullName.Substring(x.Length);
            else
                return x;
        }
    }
}
