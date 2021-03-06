using System;
using System.Collections.Generic;
using System.Text;

using Elsa.Dashboard.Areas.Elsa.ViewModels;

namespace Elsa.Dashboard.Areas.Elsa
{
    public sealed class WorkflowTemp
    {
        public static IList<WorkflowTemplateListItemModel> Templates = new List<WorkflowTemplateListItemModel>()
        {
            new WorkflowTemplateListItemModel("sqoopbase","Sqoop Basic","~/templates/sqoopbase/img.png","templates/sqoopbase/workflow.json","Sqoop import basic template"),
            new WorkflowTemplateListItemModel("sparkbase","Spark Basic","~/templates/sparkbase/img.png","templates/sparkbase/workflow.json","Spark application basic template"),
            new WorkflowTemplateListItemModel("sparkstream","Spark Stream","~/templates/sparkstream/img.png","templates/sparkstream/workflow.json","Spark stream query + kafka + mongo template"),
            new WorkflowTemplateListItemModel("dataprocess","Data Process","~/templates/dataprocess/img.png","templates/dataprocess/workflow.json","Data process integration sqoop with spark template")
        };

        public static Dictionary<string, string> TemplateWizardViews = new Dictionary<string, string>()
        {
            {"sqoopbase","SqoopBaseWizard" },{"sparkbase","SparkBaseWizard"},
            {"sparkstream","SparkStreamWizard"},{"dataprocess","DataProcessWizard"}
        };
    }
}
