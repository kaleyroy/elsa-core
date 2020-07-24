using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Dashboard.Areas.Elsa.ViewModels
{
    public class WorkflowTemplateListItemModel
    {
        public string TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string ImageUrl { get; set; }
        public string JsonFile { get; set; }
        public string Description { get; set; }

        public WorkflowTemplateListItemModel() { }
        public WorkflowTemplateListItemModel(string templateId,string templateName, string imageUrl, string jsonFile, string description = "")
        {
            TemplateId = templateId;
            TemplateName = templateName;
            ImageUrl = imageUrl;
            JsonFile = jsonFile;
            Description = description;
        }
    }
}
