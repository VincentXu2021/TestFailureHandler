using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace TestFailureHandler
{
    class Program
    {
        static async Task Main(string[] args)
        {
            List<string> failedTests =ProcessAllFailedTests("TestResults.xml");
            await File.WriteAllLinesAsync("re-run.cmd", failedTests.ToArray());            
        }

        static List<string> ProcessAllFailedTests(string testResultTrxFile)
        {
            if (!File.Exists(testResultTrxFile)) return null;

            XmlDocument doc = new XmlDocument();            
            doc.Load(testResultTrxFile);

            string summary = doc.GetElementsByTagName("ResultSummary")[0]
                .Attributes["outcome"].Value.ToLower();
            if(!summary.Equals("failed")) return null;

            XmlNodeList testResults = doc.GetElementsByTagName("UnitTestResult");
            XmlNodeList testDefinitions = doc.GetElementsByTagName("UnitTest");
            List<string> failedTests = new List<string>();

            foreach(XmlNode testResult in testResults)
            {
                string outcome = testResult.Attributes["outcome"].Value.ToLower();
                if (outcome.Equals("failed"))
                {                    
                    failedTests.Add(HandleFailedTest(testDefinitions,
                        testResult.Attributes["testId"].Value));                    
                }
            }
            return failedTests;
        }

        static string HandleFailedTest(XmlNodeList testDefinitions, string testId)
        {
            foreach(XmlNode definition in testDefinitions)
            {
                string id = definition.Attributes["id"].Value;
                if (id.Equals(testId))
                {
                    string className = definition.ChildNodes[1].Attributes["className"].Value;
                    string name = definition.ChildNodes[1].Attributes["name"].Value;
                    string codebase = definition.ChildNodes[1].Attributes["codeBase"].Value;                     
                    string dllName = codebase.Substring(codebase.LastIndexOf('\\') + 1);
                    return $"dotnet test {dllName} --filter FullyQualifiedName={className}.{name}";
                }
            }
            return null;
        }
    }
}
